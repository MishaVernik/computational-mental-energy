[CmdletBinding()]
param(
    [string]$Subscription = '9acd98d2-d87b-46b9-ab35-daaa52513f2c',
    [string]$AdtName      = 'cme',
    [switch]$NoStart
)

# Reset-Adt.ps1
# Safely re-runs the DTDL reset:
#   1. Self-elevates (UAC) if not already admin.
#   2. Stops cme-api + dependents so the API can't recreate twins mid-reset.
#   3. Invokes Complete-Azure-Setup.ps1 -Reset which wipes twins + models then
#      re-uploads the 6 DTDL files from docs\dtdl\.
#   4. Starts services back in dependency order (unless -NoStart).

$ErrorActionPreference = 'Stop'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    Write-Host "Not elevated. Relaunching as Administrator..." -ForegroundColor Yellow
    $argList = @('-NoProfile','-ExecutionPolicy','Bypass','-File',"`"$PSCommandPath`"")
    if ($Subscription -ne '9acd98d2-d87b-46b9-ab35-daaa52513f2c') { $argList += @('-Subscription',$Subscription) }
    if ($AdtName -ne 'cme') { $argList += @('-AdtName',$AdtName) }
    if ($NoStart) { $argList += '-NoStart' }
    Start-Process powershell -Verb RunAs -ArgumentList $argList
    return
}

$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

# Ensure Azure CLI is on PATH for this session
$azBin = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin'
if ((Test-Path $azBin) -and ($env:PATH -notlike "*${azBin}*")) {
    $env:PATH = "$azBin;$env:PATH"
}

$stopOrder  = 'cme-bridge','cme-flowclassifier','cme-qbackend','cme-api'
$startOrder = 'cme-api','cme-qbackend','cme-flowclassifier','cme-bridge'

Write-Host ""
Write-Host "=== stopping services so reset can complete ===" -ForegroundColor Cyan
foreach ($s in $stopOrder) {
    $svc = Get-Service -Name $s -ErrorAction SilentlyContinue
    if (-not $svc) { Write-Host "  [skip] $s not installed"; continue }
    if ($svc.Status -eq 'Stopped') { Write-Host "  [ok]   $s already stopped"; continue }
    try { Stop-Service $s -Force -ErrorAction Stop; Write-Host "  [stop] $s" }
    catch { Write-Host "  [warn] $s : $($_.Exception.Message)" -ForegroundColor Yellow }
}

Start-Sleep -Seconds 3

Write-Host ""
Write-Host "=== running Complete-Azure-Setup.ps1 -Reset ===" -ForegroundColor Cyan
$logFile = Join-Path $env:TEMP "setup-reset-$(Get-Date -Format yyyyMMdd-HHmmss).log"
Write-Host "  log: $logFile"
& "$repo\scripts\Complete-Azure-Setup.ps1" -Reset 2>&1 | Tee-Object -FilePath $logFile
$resetExit = $LASTEXITCODE

if (-not $NoStart) {
    Write-Host ""
    Write-Host "=== starting services back ===" -ForegroundColor Cyan
    foreach ($s in $startOrder) {
        $svc = Get-Service -Name $s -ErrorAction SilentlyContinue
        if (-not $svc) { continue }
        try { Start-Service $s -ErrorAction Stop; Write-Host "  [start] $s" }
        catch { Write-Host "  [warn]  $s : $($_.Exception.Message)" -ForegroundColor Yellow }
        Start-Sleep -Milliseconds 800
    }
}

Write-Host ""
if ($resetExit -eq 0) { Write-Host "Reset complete." -ForegroundColor Green }
else { Write-Host "Reset finished with non-zero exit ($resetExit). Check $logFile." -ForegroundColor Yellow }
