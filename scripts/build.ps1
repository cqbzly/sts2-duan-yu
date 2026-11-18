param(
    [string]$Sts2Path,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$localDotnet = Join-Path $repositoryRoot '.tools/dotnet/dotnet.exe'
$dotnetExecutable = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
$buildArguments = @('build', (Join-Path $repositoryRoot 'DuanYu.csproj'), '--configuration', $Configuration, '--nologo')
if ($Sts2Path) { $buildArguments += "-p:Sts2Path=$Sts2Path" }
& $dotnetExecutable @buildArguments
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }
