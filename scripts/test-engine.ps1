param(
    [switch]$Simulation,
    [switch]$FullRun,
    [ValidateRange(1, 100)][int]$Samples = 12,
    [ValidateRange(1, 1000)][int]$Attempts = 20,
    [ValidateRange(1, 1000)][int]$StartSeed = 1,
    [ValidatePattern('^[A-Za-z0-9_-]*$')][string]$Seed = '',
    [ValidateRange(1, 86400)][int]$TimeoutSeconds = 600,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_-]*$')][string]$OutputName = 'a10'
)

$ErrorActionPreference = 'Stop'
if ($Simulation -and $FullRun) { throw 'Choose either Simulation or FullRun.' }
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$artifactDirectory = Join-Path $repositoryRoot 'artifacts'
$hostDirectory = Join-Path $artifactDirectory 'game-selftest'
$gameExecutable = Join-Path $hostDirectory 'SlayTheSpire2.exe'
$overridePath = Join-Path $hostDirectory 'override.cfg'
$isolatedUserDirectoryName = 'cqbzly-duanyu-engine-tests'
foreach ($requiredPath in @($gameExecutable, $overridePath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Isolated test host is incomplete: $requiredPath"
    }
}
if ((Get-Item -LiteralPath $hostDirectory).Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
    throw 'The isolated test host must be a real directory, not a junction or symbolic link.'
}

$section = ''
$applicationSettings = @{}
foreach ($line in Get-Content -LiteralPath $overridePath) {
    $trimmedLine = $line.Trim()
    if ($trimmedLine -match '^\[(.+)\]$') {
        $section = $Matches[1]
    }
    elseif ($section -eq 'application' -and $trimmedLine -match '^([^;#=]+)=(.*)$') {
        $applicationSettings[$Matches[1].Trim()] = $Matches[2].Trim()
    }
}
if ($applicationSettings['config/use_custom_user_dir'] -cne 'true' -or
    $applicationSettings['config/custom_user_dir_name'] -cne ('"' + $isolatedUserDirectoryName + '"')) {
    throw "override.cfg must enable the custom user directory `"$isolatedUserDirectoryName`"."
}

$modDirectory = Join-Path $hostDirectory 'mods/DuanYu'
foreach ($directory in @((Join-Path $hostDirectory 'mods'), $modDirectory)) {
    if ((Test-Path -LiteralPath $directory) -and
        ((Get-Item -LiteralPath $directory).Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
        throw "The isolated mod directory must not redirect elsewhere: $directory"
    }
}
& (Join-Path $PSScriptRoot 'build.ps1') -Sts2Path $hostDirectory -Configuration Debug
$assemblyPath = Join-Path $repositoryRoot '.godot/mono/temp/bin/Debug/CQBZLY_DUAN_YU.dll'
if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
    throw "Debug assembly was not produced: $assemblyPath"
}
New-Item -ItemType Directory -Path $modDirectory -Force | Out-Null
Copy-Item -LiteralPath $assemblyPath -Destination (Join-Path $modDirectory 'CQBZLY_DUAN_YU.dll') -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'DuanYu.json') -Destination (Join-Path $modDirectory 'DuanYu.json') -Force

$userDirectory = Join-Path ([Environment]::GetFolderPath('ApplicationData')) $isolatedUserDirectoryName
$reportCopies = @(
    @{
        Source = Join-Path $userDirectory 'duanyu-engine-tests.json'
        Destination = Join-Path $artifactDirectory "$OutputName-engine-tests.json"
        Required = $true
    },
    @{
        Source = Join-Path $userDirectory 'duanyu-a10-simulation.json'
        Destination = Join-Path $artifactDirectory "$OutputName-simulation.json"
        Required = [bool]$Simulation
    },
    @{
        Source = Join-Path $userDirectory 'duanyu-full-run.json'
        Destination = Join-Path $artifactDirectory "$OutputName-full-run.json"
        Required = [bool]$FullRun
    }
)
foreach ($report in $reportCopies) {
    foreach ($reportPath in @($report.Source, $report.Destination)) {
        if (Test-Path -LiteralPath $reportPath) { Remove-Item -LiteralPath $reportPath -Force }
    }
}
$stdoutPath = Join-Path $artifactDirectory "$OutputName-stdout.log"
$stderrPath = Join-Path $artifactDirectory "$OutputName-stderr.log"
$gameArguments = @('--headless', '--force-steam=off', '--')
if ($FullRun) {
    $gameArguments += @('--duanyu-full-run', "--duanyu-run-attempts=$Attempts", "--duanyu-run-start=$StartSeed")
    if ($Seed) { $gameArguments += "--duanyu-run-seed=$Seed" }
}
elseif ($Simulation) {
    $gameArguments += @('--duanyu-a10-sim', "--duanyu-sim-samples=$Samples")
}
else {
    $gameArguments += '--duanyu-self-test'
}

$startedAt = [DateTime]::UtcNow
$testProcess = Start-Process -FilePath $gameExecutable -WorkingDirectory $hostDirectory -ArgumentList $gameArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
$timedOut = $false
try {
    $null = $testProcess.Handle
    if (-not $testProcess.WaitForExit($TimeoutSeconds * 1000)) {
        $timedOut = $true
        if (-not $testProcess.HasExited) { $testProcess.Kill() }
        if (-not $testProcess.WaitForExit(10000)) {
            throw "Timed out and could not stop test process $($testProcess.Id). Logs: $stdoutPath, $stderrPath"
        }
    }
    $testProcess.WaitForExit()
    $exitCode = $testProcess.ExitCode
}
finally {
    if (-not $testProcess.HasExited) {
        $testProcess.Kill()
        $null = $testProcess.WaitForExit(10000)
    }
    $testProcess.Dispose()
}

$missingReports = @()
foreach ($report in $reportCopies) {
    if ((Test-Path -LiteralPath $report.Source -PathType Leaf) -and
        (Get-Item -LiteralPath $report.Source).LastWriteTimeUtc -ge $startedAt) {
        Copy-Item -LiteralPath $report.Source -Destination $report.Destination -Force
        Write-Output "Report: $($report.Destination)"
    }
    elseif ($report.Required) {
        $missingReports += $report.Source
    }
}
Write-Output "Logs: $stdoutPath, $stderrPath"
if ($timedOut) { throw "Engine tests exceeded the $TimeoutSeconds-second timeout." }
if ($null -eq $exitCode) { throw 'Engine test process did not expose an exit code.' }
if ($exitCode -ne 0) { throw "Engine tests failed with exit code $exitCode." }
if ($missingReports.Count -gt 0) { throw "Engine tests did not produce fresh reports: $($missingReports -join ', ')" }
$engineReport = Get-Content -LiteralPath $reportCopies[0].Destination -Raw | ConvertFrom-Json
if ($null -eq $engineReport.passed -or $null -eq $engineReport.failed -or $engineReport.failed -ne 0 -or $engineReport.passed -lt 1) {
    throw 'Engine test report does not confirm a successful test run.'
}
Write-Output "Engine tests passed: $($engineReport.passed)."
