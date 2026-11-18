param(
    [Parameter(Mandatory = $true)][string]$Sts2Path,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$resolvedGamePath = (Resolve-Path -LiteralPath $Sts2Path).Path
$gameDataPath = Join-Path $resolvedGamePath 'data_sts2_windows_x86_64/sts2.dll'
if (-not (Test-Path -LiteralPath $gameDataPath)) { throw 'The selected directory is not a Windows Slay the Spire 2 installation.' }
if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'build.ps1') -Sts2Path $resolvedGamePath -Configuration $Configuration
}
$assemblyPath = Join-Path $repositoryRoot ".godot/mono/temp/bin/$Configuration/CQBZLY_DUAN_YU.dll"
if (-not (Test-Path -LiteralPath $assemblyPath)) {
    $assemblyPath = Join-Path $repositoryRoot "bin/$Configuration/net9.0/CQBZLY_DUAN_YU.dll"
}
if (-not (Test-Path -LiteralPath $assemblyPath)) { throw 'Compiled CQBZLY_DUAN_YU.dll was not found.' }
$modDirectory = Join-Path $resolvedGamePath 'mods/DuanYu'
New-Item -ItemType Directory -Path $modDirectory -Force | Out-Null
foreach ($filename in @('CQBZLY_DUAN_YU.dll', 'DuanYu.json')) {
    $existingPath = Join-Path $modDirectory $filename
    if (Test-Path -LiteralPath $existingPath) {
        $backupDirectory = Join-Path $repositoryRoot ('artifacts/install-backups/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
        New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
        Copy-Item -LiteralPath $existingPath -Destination (Join-Path $backupDirectory $filename)
    }
}
Copy-Item -LiteralPath $assemblyPath -Destination (Join-Path $modDirectory 'CQBZLY_DUAN_YU.dll') -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'DuanYu.json') -Destination (Join-Path $modDirectory 'DuanYu.json') -Force
Write-Output "Installed Duan Yu to $modDirectory"
