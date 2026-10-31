<#
.SYNOPSIS
    Tags the test runs a job has published with its stage, for the Tags filter of the Tests tab.

.DESCRIPTION
    The runs are those Publish-TestRunsByCategory.ps1 has published in this job, as it lists them in
    published-test-runs.txt of the agent's temp folder. The agent creates a run some time after the publish command, so
    the runs are read again until each of them is there; a run of an earlier attempt of the job has the same title, so
    only runs created since the publish count. The publish command cannot tag a run, so the tags are added through the
    REST API with the token of the job (SYSTEM_ACCESSTOKEN). A run left without tags fails the step.
#>
param(
    [Parameter(Mandatory)] [string] $CollectionUri,
    [Parameter(Mandatory)] [string] $Project,
    [Parameter(Mandatory)] [int] $BuildId,
    # The stage display name; a tag has only letters and digits: 'Donor Import' -> 'DonorImport'.
    [Parameter(Mandatory)] [string] $Stage,
    [int] $WaitMinutes = 5
)

$ErrorActionPreference = 'Stop'

$publishedFile = Join-Path $env:AGENT_TEMPDIRECTORY 'published-test-runs.txt'
if (-not (Test-Path $publishedFile)) {
    Write-Host 'No test runs published'
    return
}
# Title -> the time it was published.
$publishedAt = @{}
foreach ($line in Get-Content $publishedFile) {
    $time, $title = $line -split "`t", 2
    $publishedAt[$title] = [datetime]::Parse($time, $null, 'RoundtripKind')
}

$tag = -join ($Stage -split '[^A-Za-z0-9]+' | Where-Object { $_ } | ForEach-Object { $_.Substring(0, 1).ToUpper() + $_.Substring(1) })
$api = "$($CollectionUri.TrimEnd('/'))/$([uri]::EscapeDataString($Project))/_apis/test/runs"
$headers = @{ Authorization = "Bearer $env:SYSTEM_ACCESSTOKEN" }

function Get-BuildRuns {
    $runs = @()
    do {
        $page = @((Invoke-RestMethod "$($api)?buildUri=vstfs:///Build/Build/$BuildId&includeRunDetails=true&`$top=100&`$skip=$($runs.Count)&api-version=7.1" -Headers $headers).value)
        $runs += $page
    } while ($page.Count -eq 100)
    $runs
}

# Only a run itself has its creation time, not the list of runs. Run id -> created.
$createdOf = @{}
# The clocks of the agent and the server differ a little.
$skew = [timespan]::FromMinutes(1)
$deadline = [datetime]::UtcNow.AddMinutes($WaitMinutes)
while ($true) {
    $runs = @(Get-BuildRuns | Where-Object { $publishedAt.ContainsKey($_.name) } | Where-Object {
        if (-not $createdOf.ContainsKey($_.id)) {
            $createdOf[$_.id] = ([datetime] (Invoke-RestMethod "$api/$($_.id)?api-version=7.1" -Headers $headers).createdDate).ToUniversalTime()
        }
        $createdOf[$_.id] -ge $publishedAt[$_.name] - $skew
    })
    $missing = @($publishedAt.Keys | Where-Object { $runs.name -notcontains $_ } | Sort-Object)
    if (-not $missing.Count -or [datetime]::UtcNow -ge $deadline) {
        break
    }
    Write-Host "Waiting for $($missing.Count) test run(s): $($missing -join ', ')"
    Start-Sleep -Seconds 15
}

$body = @{ tags = @(@{ name = $tag }) } | ConvertTo-Json -Depth 3
foreach ($run in $runs) {
    Invoke-RestMethod "$api/$($run.id)?api-version=7.1" -Method Patch -Headers $headers -ContentType 'application/json' -Body $body | Out-Null
    Write-Host "$($run.name): $tag"
}

if ($missing.Count) {
    throw "Not found in $WaitMinutes min, left without tags: $($missing -join ', ')"
}
