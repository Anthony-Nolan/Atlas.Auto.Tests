<#
.SYNOPSIS
    Samples CPU and memory of the agent while a test run runs, to see how many tests can run at once
    (LevelOfParallelism in AssemblyInfo.cs).

.DESCRIPTION
    -Action Start launches a hidden sampler that writes a line to Folder/resources.csv every IntervalSeconds;
    -Action Stop stops it and prints the peaks and averages. Windows agents only (CIM counters).
#>
param(
    [Parameter(Mandatory)] [ValidateSet('Start', 'Stop', 'Sample')] [string] $Action,
    [Parameter(Mandatory)] [string] $Folder,
    [int] $IntervalSeconds = 10
)

$ErrorActionPreference = 'Stop'
$csv = Join-Path $Folder 'resources.csv'
$stopFile = Join-Path $Folder 'stop'

switch ($Action) {
    'Start' {
        New-Item -ItemType Directory -Force $Folder | Out-Null
        Remove-Item $stopFile -ErrorAction SilentlyContinue
        'time,cpuPercent,usedMemoryMb,totalMemoryMb,testHostMemoryMb' | Set-Content $csv
        $shell = (Get-Process -Id $PID).Path
        Start-Process $shell -WindowStyle Hidden -ArgumentList @(
            '-NoProfile', '-File', $PSCommandPath, '-Action', 'Sample', '-Folder', $Folder, '-IntervalSeconds', $IntervalSeconds)
        Write-Host "Sampling CPU and memory every $IntervalSeconds s into $csv"
    }
    'Sample' {
        while (-not (Test-Path $stopFile)) {
            $os = Get-CimInstance Win32_OperatingSystem
            $cpu = (Get-CimInstance Win32_Processor | Measure-Object LoadPercentage -Average).Average
            # The tests run in testhost; dotnet test only starts it.
            $testHostMb = [math]::Round(((Get-Process testhost -ErrorAction SilentlyContinue | Measure-Object WorkingSet64 -Sum).Sum) / 1MB)
            $totalMb = [math]::Round($os.TotalVisibleMemorySize / 1KB)
            $usedMb = $totalMb - [math]::Round($os.FreePhysicalMemory / 1KB)
            "$((Get-Date).ToString('HH:mm:ss')),$cpu,$usedMb,$totalMb,$testHostMb" | Add-Content $csv
            Start-Sleep -Seconds $IntervalSeconds
        }
    }
    'Stop' {
        New-Item -ItemType File -Force $stopFile | Out-Null
        if (-not (Test-Path $csv)) {
            Write-Host 'No samples'
            return
        }
        $samples = @(Import-Csv $csv)
        if (-not $samples.Count) {
            Write-Host 'No samples'
            return
        }
        foreach ($column in 'cpuPercent', 'usedMemoryMb', 'testHostMemoryMb') {
            $stats = $samples | ForEach-Object { [double] $_.$column } | Measure-Object -Average -Maximum
            Write-Host ("{0,-16} max {1,7}  avg {2,7}" -f $column, $stats.Maximum, [math]::Round($stats.Average))
        }
        Write-Host "Total memory $($samples[0].totalMemoryMb) MB, $($samples.Count) samples"
    }
}
