[CmdletBinding()]
param(
    [switch]$SkipApi,
    [switch]$SkipDashboard,
    [switch]$NoStart,
    [string]$Configuration = 'Release'
)

# Redeploy-VpsServices.ps1
# Rebuild + restart the cme-* services in correct dependency order.
# - Stops services in reverse dependency order so dotnet publish can overwrite locked DLLs.
# - Runs dotnet publish for CmeSim.Api and npm build for cme-live-dashboard.
# - Starts services back in dependency order.
# Self-elevates if not already running as Administrator.

$ErrorActionPreference = 'Stop'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p  = New-Object Security.Principal.WindowsPrincipal($id)
    return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    Write-Host "Not elevated. Relaunching as Administrator..." -ForegroundColor Yellow
    $argList = @('-NoProfile','-ExecutionPolicy','Bypass','-File',"`"$PSCommandPath`"")
    if ($SkipApi)       { $argList += '-SkipApi' }
    if ($SkipDashboard) { $argList += '-SkipDashboard' }
    if ($NoStart)       { $argList += '-NoStart' }
    if ($Configuration -ne 'Release') { $argList += @('-Configuration', $Configuration) }
    Start-Process powershell -Verb RunAs -ArgumentList $argList
    return
}

$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo
Write-Host "repo: $repo" -ForegroundColor Cyan

# Service dependency order:
#   cme-api  <-  cme-qbackend, cme-flowclassifier, cme-bridge
#   cme-live-dashboard is independent (UI only)
$stopOrder  = 'cme-bridge','cme-flowclassifier','cme-qbackend','cme-dashboard','cme-api'
$startOrder = 'cme-api','cme-qbackend','cme-flowclassifier','cme-bridge','cme-dashboard'

function Get-CmeService([string]$name) {
    Get-Service -Name $name -ErrorAction SilentlyContinue
}

function Stop-CmeService([string]$name) {
    $svc = Get-CmeService $name
    if (-not $svc) { Write-Host "  [skip] $name not installed"; return }
    if ($svc.Status -eq 'Stopped') { Write-Host "  [ok]   $name already stopped"; return }
    try {
        Stop-Service $name -Force -ErrorAction Stop
        Write-Host "  [stop] $name"
    } catch {
        Write-Host "  [warn] could not stop $name : $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Start-CmeService([string]$name) {
    $svc = Get-CmeService $name
    if (-not $svc) { Write-Host "  [skip] $name not installed"; return }
    if ($svc.Status -eq 'Running') { Write-Host "  [ok]    $name already running"; return }
    try {
        Start-Service $name -ErrorAction Stop
        Write-Host "  [start] $name"
    } catch {
        Write-Host "  [warn]  could not start $name : $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== stopping services (reverse dependency order) ===" -ForegroundColor Cyan
foreach ($s in $stopOrder) { Stop-CmeService $s }

# Wait briefly so file locks release
Start-Sleep -Seconds 2

if (-not $SkipApi) {
    Write-Host ""
    Write-Host "=== publishing CmeSim.Api ($Configuration) ===" -ForegroundColor Cyan
    & dotnet publish "$repo\CmeSim.Api\CmeSim.Api.csproj" -c $Configuration -o "$repo\publish\CmeSim.Api" --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "dotnet publish failed (exit $LASTEXITCODE). Aborting." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

if (-not $SkipDashboard) {
    $dashDir = Join-Path $repo 'cme-live-dashboard'
    if (Test-Path (Join-Path $dashDir 'package.json')) {
        Write-Host ""
        Write-Host "=== building cme-live-dashboard ===" -ForegroundColor Cyan
        Push-Location $dashDir
        try {
            & npm run build
            if ($LASTEXITCODE -ne 0) {
                Write-Host "npm build failed (exit $LASTEXITCODE). Continuing without dashboard rebuild." -ForegroundColor Yellow
            }
        } finally { Pop-Location }
    }
}

if (-not $NoStart) {
    Write-Host ""
    Write-Host "=== starting services (dependency order) ===" -ForegroundColor Cyan
    foreach ($s in $startOrder) { Start-CmeService $s; Start-Sleep -Milliseconds 800 }
}

Write-Host ""
Write-Host "=== final status ===" -ForegroundColor Cyan
foreach ($s in $startOrder) {
    $svc = Get-CmeService $s
    if ($svc) {
        $color = if ($svc.Status -eq 'Running') { 'Green' } else { 'Yellow' }
        Write-Host ("  {0,-22} {1}" -f $svc.Name, $svc.Status) -ForegroundColor $color
    } else {
        Write-Host ("  {0,-22} not installed" -f $s) -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
