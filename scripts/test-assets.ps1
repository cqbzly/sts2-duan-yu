param([string]$GodotPath, [string]$ExportArtPath)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
if (-not $GodotPath) {
    $GodotPath = Join-Path $repositoryRoot '.tools/godot/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe'
}
if (-not (Test-Path -LiteralPath $GodotPath)) { throw 'Pass -GodotPath pointing to a Godot 4.5.1 .NET executable.' }
$testDirectory = Join-Path $repositoryRoot 'tests/AssetSmoke'
$localDotnet = Join-Path $repositoryRoot '.tools/dotnet/dotnet.exe'
$dotnetExecutable = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
& $dotnetExecutable build (Join-Path $testDirectory 'AssetSmoke.csproj') --nologo
if ($LASTEXITCODE -ne 0) { throw "Asset test build failed with exit code $LASTEXITCODE." }
$previousDotnetRoot = $env:DOTNET_ROOT
try {
    if (Test-Path -LiteralPath $localDotnet) { $env:DOTNET_ROOT = Split-Path $localDotnet -Parent }
    $godotArguments = @('--headless', '--path', $testDirectory)
    if ($ExportArtPath) { $godotArguments += @('--', '--export-art', [System.IO.Path]::GetFullPath($ExportArtPath)) }
    $outputLines = & $GodotPath @godotArguments 2>&1
    $testExitCode = $LASTEXITCODE
    $outputLines | ForEach-Object { Write-Output $_ }
    if ($testExitCode -ne 0 -or ($outputLines | Select-String -Pattern 'ERROR:|ASSET_SMOKE_FAIL')) {
        throw "Asset test failed with exit code $testExitCode."
    }
    if (-not ($outputLines | Select-String -Pattern 'ASSET_SMOKE_OK')) { throw 'Asset test did not report completion.' }
}
finally { $env:DOTNET_ROOT = $previousDotnetRoot }
