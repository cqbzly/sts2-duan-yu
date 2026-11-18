param(
    [string]$Sts2Path,
    [string]$PreviewPath,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'package.ps1') -Sts2Path $Sts2Path -Configuration Release
}
$manifestPath = Join-Path $repositoryRoot 'DuanYu.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.id -ne 'CQBZLY_DUAN_YU' -or -not $manifest.has_dll -or $manifest.has_pck) {
    throw 'Expected the DLL-only CQBZLY_DUAN_YU mod manifest.'
}
$baseLib = @($manifest.dependencies | Where-Object { $_.id -eq 'BaseLib' })
if ($baseLib.Count -ne 1) { throw 'The manifest must declare exactly one BaseLib dependency.' }
$packageDirectory = Join-Path $repositoryRoot "dist/DuanYu-$($manifest.version)/DuanYu"
$assemblyName = "$($manifest.id).dll"
$assemblyPath = Join-Path $packageDirectory $assemblyName
$packagedManifestPath = Join-Path $packageDirectory 'DuanYu.json'
if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf) -or -not (Test-Path -LiteralPath $packagedManifestPath -PathType Leaf)) {
    throw 'The current release package is missing. Run scripts/package.ps1 first or omit -SkipBuild.'
}
if ((Get-FileHash -LiteralPath $manifestPath).Hash -ne (Get-FileHash -LiteralPath $packagedManifestPath).Hash) {
    throw 'The packaged manifest is stale. Rebuild the release package first.'
}
$assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($assemblyPath).Version
$manifestVersion = [version]$manifest.version
if ($assemblyVersion.Major -ne $manifestVersion.Major -or $assemblyVersion.Minor -ne $manifestVersion.Minor -or $assemblyVersion.Build -ne $manifestVersion.Build) {
    throw 'The packaged DLL version does not match the manifest. Rebuild the release package first.'
}
if (-not $PreviewPath) { $PreviewPath = Join-Path $repositoryRoot 'assets/workshop-preview.png' }
$preview = Get-Item -LiteralPath $PreviewPath
if ($preview.PSIsContainer -or $preview.Length -ge 1000000) { throw 'The Workshop preview must be a PNG file smaller than 1,000,000 bytes.' }
$previewStream = $preview.OpenRead()
try {
    $header = New-Object byte[] 8
    $headerLength = $previewStream.Read($header, 0, $header.Length)
    if ($headerLength -ne 8 -or [BitConverter]::ToString($header) -ne '89-50-4E-47-0D-0A-1A-0A') {
        throw 'The Workshop preview is not a PNG image.'
    }
}
finally { $previewStream.Dispose() }
$metadata = Get-Content -LiteralPath (Join-Path $repositoryRoot 'workshop/workshop.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($metadata.visibility -ne 'private') { throw 'The initial testing workspace must have private visibility.' }
if (@($metadata.dependencies).Count -ne 1 -or $metadata.dependencies[0] -ne 3737335127) {
    throw 'Workshop dependencies must contain the BaseLib Workshop item 3737335127.'
}
foreach ($field in @('title', 'description', 'changeNote')) {
    $metadata.$field = $metadata.$field.Replace('{{VERSION}}', $manifest.version).Replace('{{GAME_VERSION}}', $manifest.min_game_version).Replace('{{BASELIB_VERSION}}', $baseLib[0].min_version)
}
$workspace = Join-Path $repositoryRoot 'dist/workshop'
$contentDirectory = Join-Path $workspace 'content'
if (Test-Path -LiteralPath $contentDirectory) {
    $unexpectedFiles = @(Get-ChildItem -LiteralPath $contentDirectory -Force | Where-Object { $_.PSIsContainer -or $_.Name -notin @($assemblyName, 'DuanYu.json') })
    if ($unexpectedFiles.Count -gt 0) {
        throw 'Unexpected files exist in dist/workshop/content. Move them out before preparing the upload.'
    }
}
New-Item -ItemType Directory -Path $contentDirectory -Force | Out-Null
Copy-Item -LiteralPath $assemblyPath -Destination (Join-Path $contentDirectory $assemblyName) -Force
Copy-Item -LiteralPath $packagedManifestPath -Destination (Join-Path $contentDirectory 'DuanYu.json') -Force
Copy-Item -LiteralPath $preview.FullName -Destination (Join-Path $workspace 'image.png') -Force
$metadataJson = $metadata | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText((Join-Path $workspace 'workshop.json'), $metadataJson, (New-Object System.Text.UTF8Encoding($false)))
$modIdPath = Join-Path $workspace 'mod_id.txt'
$publishedItemPath = Join-Path $repositoryRoot 'workshop/published-item.json'
if (-not (Test-Path -LiteralPath $modIdPath) -and (Test-Path -LiteralPath $publishedItemPath)) {
    $publishedItem = Get-Content -LiteralPath $publishedItemPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($publishedItem.appId -ne 2868840 -or $publishedItem.publishedFileId -notmatch '^[1-9][0-9]*$') {
        throw 'Published Workshop item metadata is invalid.'
    }
    [System.IO.File]::WriteAllText($modIdPath, $publishedItem.publishedFileId, (New-Object System.Text.UTF8Encoding($false)))
}
if (Test-Path -LiteralPath $modIdPath) {
    $modId = (Get-Content -LiteralPath $modIdPath -Raw -Encoding UTF8).Trim()
    if ($modId -notmatch '^[1-9][0-9]*$') { throw 'Existing mod_id.txt is invalid; it has been preserved for inspection.' }
    Write-Output "Preserved existing Workshop item ID $modId."
}
Write-Output "Prepared private Workshop workspace: $workspace"
Write-Output 'Only CQBZLY_DUAN_YU.dll and DuanYu.json will be included as mod content.'
Write-Output "Upload command: .\.tools\mod-uploader\ModUploader.exe upload -w `"$workspace`""
