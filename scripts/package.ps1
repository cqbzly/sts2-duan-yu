param(
    [string]$Sts2Path,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'build.ps1') -Sts2Path $Sts2Path -Configuration $Configuration
}
$assemblyPath = Join-Path $repositoryRoot ".godot/mono/temp/bin/$Configuration/CQBZLY_DUAN_YU.dll"
if (-not (Test-Path -LiteralPath $assemblyPath)) {
    $assemblyPath = Join-Path $repositoryRoot "bin/$Configuration/net9.0/CQBZLY_DUAN_YU.dll"
}
if (-not (Test-Path -LiteralPath $assemblyPath)) { throw 'Compiled CQBZLY_DUAN_YU.dll was not found.' }
$manifestPath = Join-Path $repositoryRoot 'DuanYu.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$packageRoot = Join-Path $repositoryRoot "dist/DuanYu-$($manifest.version)"
$modDirectory = Join-Path $packageRoot 'DuanYu'
New-Item -ItemType Directory -Path $modDirectory -Force | Out-Null
Copy-Item -LiteralPath $assemblyPath -Destination (Join-Path $modDirectory 'CQBZLY_DUAN_YU.dll') -Force
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $modDirectory 'DuanYu.json') -Force
$readmePath = Join-Path $repositoryRoot 'docs/installation.md'
Copy-Item -LiteralPath $readmePath -Destination (Join-Path $packageRoot 'README.md') -Force
$cheatSheetPath = Join-Path $packageRoot 'CHEAT-SHEET.md'
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'docs/cheat-sheet.md') -Destination $cheatSheetPath -Force
$archivePath = Join-Path $repositoryRoot "dist/DuanYu-$($manifest.version).zip"
Compress-Archive -LiteralPath $modDirectory, (Join-Path $packageRoot 'README.md'), $cheatSheetPath -DestinationPath $archivePath -Force
Write-Output $archivePath
