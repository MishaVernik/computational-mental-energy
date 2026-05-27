[CmdletBinding()]
param(
    [string]$StorageAccount = 'cmedtmv',
    [string]$Container      = 'twin-assets',
    [string]$BlobName       = 'head_with_muse.glb'
)

# Rebuild-And-Upload-Glb.ps1
# Regenerates cme-live-dashboard/public/head_with_muse.glb from build-head-glb.mjs
# and uploads it to the configured Azure storage container so 3D Scenes Studio
# picks up the new geometry on next browser refresh.

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$dash = Join-Path $repo 'cme-live-dashboard'
$glb  = Join-Path $dash  'public\head_with_muse.glb'

# Ensure Azure CLI is on PATH for this session
$azBin = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin'
if ((Test-Path $azBin) -and ($env:PATH -notlike "*${azBin}*")) {
    $env:PATH = "$azBin;$env:PATH"
}

Write-Host "=== building GLB ==="  -ForegroundColor Cyan
Push-Location $dash
try {
    & node scripts/build-head-glb.mjs
    if ($LASTEXITCODE -ne 0) { throw "build-head-glb.mjs failed (exit $LASTEXITCODE)" }
} finally { Pop-Location }

if (-not (Test-Path $glb)) { throw "GLB not found at $glb" }
$size = (Get-Item $glb).Length
Write-Host "  GLB: $glb ($size bytes)"

Write-Host ""
Write-Host "=== uploading to $StorageAccount/$Container/$BlobName ===" -ForegroundColor Cyan
& az storage blob upload `
    --account-name $StorageAccount `
    --container-name $Container `
    --name $BlobName `
    --file $glb `
    --auth-mode login `
    --overwrite `
    --only-show-errors
if ($LASTEXITCODE -ne 0) { throw "blob upload failed (exit $LASTEXITCODE)" }

$blobUrl = "https://$StorageAccount.blob.core.windows.net/$Container/$BlobName"
Write-Host ""
Write-Host "  uploaded -> $blobUrl" -ForegroundColor Green
Write-Host "  Refresh 3D Scenes Studio (Ctrl+F5) to pull the new GLB."
