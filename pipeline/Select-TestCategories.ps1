<#
.SYNOPSIS
    Chooses the test categories a job runs: all of them on its first attempt; on a rerun, only those that did not pass.

.DESCRIPTION
    Every attempt of a job publishes the results of its tests, the NUnit result XML and the TRX (artifact
    test-results-<test run>-<attempt>). 'Rerun failed jobs' downloads those of the earlier attempts into
    PreviousResultsFolder. A category passed when the latest attempt that ran it has no failed or inconclusive test.
    A category no attempt ran (the job was cancelled or timed out before the tests ended) runs again.

    Sets the job variables TestCategories (the categories to run, comma-separated), TestFilter (the dotnet test filter)
    and RunsTests (false when every category passed before), with the prefix VariablePrefix: a job has several test runs.
#>
param(
    [Parameter(Mandatory)] [string] $Categories,
    [string] $PreviousResultsFolder,
    [string] $VariablePrefix
)

$ErrorActionPreference = 'Stop'

$requested = @($Categories -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })

# The test-results-<test run>-<attempt> folders, oldest first.
$attempts = @()
if ($PreviousResultsFolder -and (Test-Path $PreviousResultsFolder)) {
    $attempts = @(Get-ChildItem $PreviousResultsFolder -Directory |
        Where-Object { $_.Name -match '-(\d+)$' } |
        Sort-Object { [int]($_.Name -replace '^.*-', '') })
}

# Category -> whether it passed in the latest attempt that ran it.
$passed = @{}
foreach ($attempt in $attempts) {
    $outcomes = @{}
    foreach ($file in Get-ChildItem $attempt.FullName -Filter *.xml) {
        [xml] $xml = Get-Content $file.FullName -Raw
        foreach ($case in $xml.SelectNodes('//test-case')) {
            # A category is on the fixture or on the test method, so on an ancestor of the test case.
            $caseCategories = @($case.SelectNodes('ancestor-or-self::*/properties/property[@name="Category"]') | ForEach-Object { $_.value })
            $ok = $case.result -ne 'Failed' -and $case.result -ne 'Inconclusive'
            foreach ($category in $requested | Where-Object { $caseCategories -contains $_ }) {
                $outcomes[$category] = ($outcomes[$category] -ne $false) -and $ok
            }
        }
    }
    foreach ($category in $outcomes.Keys) {
        $passed[$category] = $outcomes[$category]
    }
    Write-Host "Attempt $($attempt.Name): $(@($outcomes.Keys | Where-Object { $outcomes[$_] }).Count) passed, $(@($outcomes.Keys | Where-Object { -not $outcomes[$_] }).Count) not passed"
}

$toRun = @($requested | Where-Object { -not $passed[$_] })
foreach ($category in $requested) {
    Write-Host "$(if ($toRun -contains $category) { 'Run' } else { 'Passed in an earlier attempt' }): $category"
}

Write-Host "##vso[task.setvariable variable=${VariablePrefix}TestCategories]$($toRun -join ',')"
Write-Host "##vso[task.setvariable variable=${VariablePrefix}TestFilter]$(($toRun | ForEach-Object { "TestCategory=$_" }) -join '|')"
Write-Host "##vso[task.setvariable variable=${VariablePrefix}RunsTests]$(([bool]$toRun.Count).ToString().ToLower())"
