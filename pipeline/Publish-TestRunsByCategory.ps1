<#
.SYNOPSIS
    Publishes the TRX of a job's test run as one test run per category, titled with the category.

.DESCRIPTION
    The Tests tab of a build groups results only by test run, so a job that runs several categories publishes a run
    for each, as the jobs per category did. The TRX has no categories; they come from the NUnit result XML of the same
    run. A category is on a fixture or on a test method (RepeatSearchHappyPathTests_Adult and _Cord are methods of one
    class), so a test is matched by its class and name.
#>
param(
    [Parameter(Mandatory)] [string] $TrxFile,
    [Parameter(Mandatory)] [string] $NUnitXmlFile,
    [Parameter(Mandatory)] [string] $Categories,
    # The run title of tests of none of the categories; without it they are not published.
    [string] $OtherTitle
)

$ErrorActionPreference = 'Stop'

$requested = @($Categories -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })

if (-not (Test-Path $TrxFile)) {
    # The test run was stopped before it wrote its results; its log has what ran.
    Write-Warning "No test results: $TrxFile"
    return
}

# '<class>.<test name>' -> its category among the requested ones. Without the NUnit XML all tests go to OtherTitle.
$categoryOfTest = @{}
if (Test-Path $NUnitXmlFile) {
    [xml] $nunit = Get-Content $NUnitXmlFile -Raw
    foreach ($case in $nunit.SelectNodes('//test-case')) {
        $caseCategories = @($case.SelectNodes('ancestor-or-self::*/properties/property[@name="Category"]') | ForEach-Object { $_.value })
        $category = $requested | Where-Object { $caseCategories -contains $_ } | Select-Object -First 1
        if ($category) {
            $categoryOfTest["$($case.classname).$($case.name)"] = $category
        }
    }
}

$ns = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'
[xml] $trx = Get-Content $TrxFile -Raw
$names = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
$names.AddNamespace('t', $ns)

# Test id -> run title.
$titleOfTest = @{}
foreach ($test in $trx.SelectNodes('//t:TestDefinitions/t:UnitTest', $names)) {
    $key = "$($test.SelectSingleNode('t:TestMethod', $names).className).$($test.name)"
    $title = if ($categoryOfTest.ContainsKey($key)) { $categoryOfTest[$key] } else { $OtherTitle }
    if ($title) {
        $titleOfTest[$test.id] = $title
    }
}

$folder = Split-Path $TrxFile -Parent
foreach ($title in @($titleOfTest.Values | Sort-Object -Unique)) {
    $part = $trx.Clone()
    $partNames = New-Object System.Xml.XmlNamespaceManager($part.NameTable)
    $partNames.AddNamespace('t', $ns)
    $keep = { param($testId) $titleOfTest[$testId] -eq $title }

    foreach ($node in @($part.SelectNodes('//t:Results/t:UnitTestResult', $partNames)) + @($part.SelectNodes('//t:TestEntries/t:TestEntry', $partNames))) {
        if (-not (& $keep $node.testId)) { [void] $node.ParentNode.RemoveChild($node) }
    }
    foreach ($node in @($part.SelectNodes('//t:TestDefinitions/t:UnitTest', $partNames))) {
        if (-not (& $keep $node.id)) { [void] $node.ParentNode.RemoveChild($node) }
    }

    $outcomes = @($part.SelectNodes('//t:Results/t:UnitTestResult', $partNames) | ForEach-Object { $_.outcome })
    $counters = $part.SelectSingleNode('//t:ResultSummary/t:Counters', $partNames)
    $counters.total = [string] $outcomes.Count
    $counters.executed = [string] @($outcomes | Where-Object { $_ -ne 'NotExecuted' }).Count
    $counters.passed = [string] @($outcomes | Where-Object { $_ -eq 'Passed' }).Count
    $counters.failed = [string] @($outcomes | Where-Object { $_ -eq 'Failed' }).Count
    $counters.notExecuted = [string] @($outcomes | Where-Object { $_ -eq 'NotExecuted' }).Count
    $counters.inconclusive = [string] @($outcomes | Where-Object { $_ -eq 'Inconclusive' }).Count
    $part.SelectSingleNode('//t:ResultSummary', $partNames).outcome = if ($outcomes -contains 'Failed') { 'Failed' } else { 'Completed' }

    $file = Join-Path $folder "$title.trx"
    $part.Save($file)
    Write-Host "$title`: $($outcomes.Count) test(s), $(@($outcomes | Where-Object { $_ -eq 'Failed' }).Count) failed"
    Write-Host "##vso[results.publish type=VSTest;mergeResults=false;publishRunAttachments=true;runTitle=$title;resultFiles=$file;]"
    if ($env:AGENT_TEMPDIRECTORY) {
        # Add-TestRunTags.ps1 waits for these runs: the agent creates them after the command, not at once.
        Add-Content (Join-Path $env:AGENT_TEMPDIRECTORY 'published-test-runs.txt') "$([datetime]::UtcNow.ToString('o'))`t$title"
    }
}
