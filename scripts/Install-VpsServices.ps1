<#
.SYNOPSIS
    Idempotent bootstrap of the CME stack on a Windows Server VPS as five NSSM-managed services.

.DESCRIPTION
    Run elevated. Resolves $RepoRoot from $PSScriptRoot, downloads NSSM portable, opens firewall ports,
    builds a Python venv + .NET publish + dashboard dist, then registers/updates auto-starting Windows
    services for: cme-api, cme-qbackend, cme-flowclassifier, cme-bridge, cme-dashboard.

    Re-run safely after edits. To skip rebuild steps pass -SkipBuild.

.NOTES
    Reads optional sibling file .env.vps (KEY=VALUE per line). When AzureDigitalTwins__* keys are present
    they are passed to cme-api as env vars; otherwise the API falls back to NoOpDigitalTwinSyncService.
#>
#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$VpsIp = '161.97.146.52',
    [int]$OscPort = 55772,
    [int]$ApiPort = 5000,
    [int]$DashboardPort = 3001,
    [int]$QBackendPort = 8001,
    [int]$FlowClassifierPort = 8002,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Write-Step { param([string]$m) Write-Host "[$([DateTime]::Now.ToString('HH:mm:ss'))] $m" -ForegroundColor Cyan }
function Write-OK   { param([string]$m) Write-Host "  ok  $m" -ForegroundColor Green }
function Write-Warn { param([string]$m) Write-Host "  ??  $m" -ForegroundColor Yellow }

# --- elevation ---
$id = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not ([Security.Principal.WindowsPrincipal]$id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: must run elevated. Right-click PowerShell -> Run as Administrator." -ForegroundColor Red
    exit 1
}

# --- paths ---
$RepoRoot   = Split-Path -Parent $PSScriptRoot
$BinDir     = Join-Path $RepoRoot 'bin'
$PublishDir = Join-Path $RepoRoot 'publish\CmeSim.Api'
$LogDir     = Join-Path $RepoRoot 'logs'
$VenvDir    = Join-Path $RepoRoot '.venv'
$VenvPy     = Join-Path $VenvDir  'Scripts\python.exe'
$NssmExe    = Join-Path $BinDir   'nssm.exe'
$EnvFile    = Join-Path $PSScriptRoot '.env.vps'
$DashDir    = Join-Path $RepoRoot 'cme-live-dashboard'
$QBackDir   = Join-Path $RepoRoot 'qbackend'
$FlowDir    = Join-Path $RepoRoot 'flow-classifier'
$BridgeDir  = Join-Path $RepoRoot 'muse-bridge'

New-Item -ItemType Directory -Force -Path $BinDir, $PublishDir, $LogDir | Out-Null

Write-Step "RepoRoot = $RepoRoot"

# --- NSSM ---
Write-Step 'NSSM portable'
if (Test-Path $NssmExe) {
    Write-OK "already present: $NssmExe (SHA256 $((Get-FileHash $NssmExe -Algorithm SHA256).Hash))"
} else {
    $zip     = Join-Path $env:TEMP "nssm-2.24-$([guid]::NewGuid()).zip"
    $extract = Join-Path $env:TEMP "nssm-2.24-$([guid]::NewGuid())"
    try {
        Write-Host '  downloading https://nssm.cc/release/nssm-2.24.zip ...'
        Invoke-WebRequest -Uri 'https://nssm.cc/release/nssm-2.24.zip' -OutFile $zip -UseBasicParsing
        Expand-Archive -Path $zip -DestinationPath $extract -Force
        $src = Join-Path $extract 'nssm-2.24\win64\nssm.exe'
        if (-not (Test-Path $src)) { throw "nssm.exe not in archive: $src" }
        Copy-Item $src $NssmExe -Force
        Write-OK "installed $NssmExe (SHA256 $((Get-FileHash $NssmExe -Algorithm SHA256).Hash))"
    } finally {
        Remove-Item $zip     -Force            -ErrorAction SilentlyContinue
        Remove-Item $extract -Recurse -Force   -ErrorAction SilentlyContinue
    }
}

# --- firewall ---
Write-Step 'Windows Firewall rules'
function Set-FwRule {
    param([string]$Name, [string]$Proto, [int]$Port)
    Get-NetFirewallRule -DisplayName $Name -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction SilentlyContinue
    New-NetFirewallRule -DisplayName $Name -Direction Inbound -Protocol $Proto -LocalPort $Port -Action Allow -Profile Any | Out-Null
    Write-OK "$Name -- inbound $Proto/$Port allowed (Any profile)"
}
Set-FwRule -Name 'CME OSC (MindMonitor)' -Proto UDP -Port $OscPort
Set-FwRule -Name 'CME API'               -Proto TCP -Port $ApiPort
Set-FwRule -Name 'CME Dashboard'         -Proto TCP -Port $DashboardPort

# --- Python venv ---
Write-Step 'Python venv'
$basePy = (Get-Command python -ErrorAction SilentlyContinue)
if (-not $basePy) { throw 'python not found on PATH' }
$basePy = $basePy.Source
if (-not (Test-Path $VenvPy)) {
    Write-Host "  creating venv $VenvDir using $basePy"
    & $basePy -m venv $VenvDir
    if ($LASTEXITCODE -ne 0) { throw 'venv creation failed' }
}
Write-Host '  upgrading pip'
& $VenvPy -m pip install --upgrade pip --disable-pip-version-check --quiet
if ($LASTEXITCODE -ne 0) { throw 'pip upgrade failed' }
foreach ($req in @(
    (Join-Path $QBackDir  'requirements.txt'),
    (Join-Path $FlowDir   'requirements.txt'),
    (Join-Path $BridgeDir 'requirements.txt')
)) {
    Write-Host "  pip install -r $req"
    & $VenvPy -m pip install -r $req --disable-pip-version-check --quiet
    if ($LASTEXITCODE -ne 0) { throw "pip install -r $req failed" }
}
Write-OK "venv ready at $VenvPy"

# --- dotnet publish ---
if (-not $SkipBuild) {
    Write-Step 'dotnet publish CmeSim.Api'
    Push-Location $RepoRoot
    try {
        & dotnet publish 'CmeSim.Api\CmeSim.Api.csproj' -c Release -o $PublishDir --nologo
        if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }
    } finally { Pop-Location }
    Write-OK "published -> $PublishDir"
} else {
    Write-Warn 'SkipBuild: dotnet publish skipped'
}

# --- dashboard build ---
if (-not $SkipBuild) {
    Write-Step 'cme-live-dashboard npm ci + build'
    Push-Location $DashDir
    try {
        & npm ci
        if ($LASTEXITCODE -ne 0) { throw 'npm ci failed' }
        if (-not (Test-Path (Join-Path $DashDir 'node_modules\serve'))) {
            Write-Host '  installing serve (transient, --no-save)'
            & npm install --no-save 'serve@^14'
            if ($LASTEXITCODE -ne 0) { throw 'npm install serve failed' }
        }
        & npm run build
        if ($LASTEXITCODE -ne 0) { throw 'npm run build failed' }
    } finally { Pop-Location }
    Write-OK "dist -> $(Join-Path $DashDir 'dist')"
} else {
    Write-Warn 'SkipBuild: npm build skipped'
}

# Resolve serve entrypoint (v14 -> build/main.js; older -> bin/serve.js)
$ServeMain = Join-Path $DashDir 'node_modules\serve\build\main.js'
if (-not (Test-Path $ServeMain)) {
    $alt = Join-Path $DashDir 'node_modules\serve\bin\serve.js'
    if (Test-Path $alt) { $ServeMain = $alt } else { throw "serve entry missing: $ServeMain (no fallback)" }
}

# --- .env.vps ---
Write-Step '.env.vps'
$EnvKv = [ordered]@{}
if (Test-Path $EnvFile) {
    foreach ($line in Get-Content $EnvFile) {
        $t = $line.Trim()
        if (-not $t -or $t.StartsWith('#')) { continue }
        $i = $t.IndexOf('=')
        if ($i -le 0) { continue }
        $k = $t.Substring(0, $i).Trim()
        $v = $t.Substring($i + 1).Trim().Trim('"').Trim("'")
        $EnvKv[$k] = $v
    }
    Write-OK "$EnvFile loaded -- $($EnvKv.Count) entries (keys: $($EnvKv.Keys -join ', '))"
} else {
    Write-Warn "$EnvFile not present -- ADT sync disabled (NoOpDigitalTwinSyncService)"
}

# --- NSSM service helpers ---
function Set-NssmService {
    param(
        [string]$Name,
        [string]$Application,
        [string]$Arguments,
        [string]$AppDirectory,
        [hashtable]$EnvVars = @{},
        [string[]]$Depends = @()
    )
    $exists = $null -ne (Get-Service $Name -ErrorAction SilentlyContinue)
    if (-not $exists) {
        Write-Host "  [$Name] installing"
        & $NssmExe install $Name $Application | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "nssm install $Name failed" }
    } else {
        Write-Host "  [$Name] exists -- updating config in place (effective on restart)"
    }

    & $NssmExe set $Name Application           $Application      | Out-Null
    & $NssmExe set $Name AppParameters         $Arguments        | Out-Null
    & $NssmExe set $Name AppDirectory          $AppDirectory     | Out-Null
    & $NssmExe set $Name Start                 SERVICE_AUTO_START | Out-Null
    & $NssmExe set $Name ObjectName            'LocalSystem'      | Out-Null
    & $NssmExe set $Name AppStdout             (Join-Path $LogDir "$Name.out.log") | Out-Null
    & $NssmExe set $Name AppStderr             (Join-Path $LogDir "$Name.err.log") | Out-Null
    & $NssmExe set $Name AppRotateFiles        1                  | Out-Null
    & $NssmExe set $Name AppRotateOnline       1                  | Out-Null
    & $NssmExe set $Name AppRotateBytes        10485760           | Out-Null
    & $NssmExe set $Name AppStopMethodConsole  5000               | Out-Null
    & $NssmExe set $Name AppExit               'Default' 'Restart' | Out-Null
    & $NssmExe set $Name AppRestartDelay       5000               | Out-Null

    # AppEnvironmentExtra: pass each KEY=VALUE as a separate arg; nssm reset clears when empty.
    if ($EnvVars.Count -gt 0) {
        $envArgs = @('set', $Name, 'AppEnvironmentExtra')
        foreach ($k in $EnvVars.Keys) { $envArgs += ('{0}={1}' -f $k, $EnvVars[$k]) }
        & $NssmExe @envArgs | Out-Null
    } else {
        & $NssmExe reset $Name AppEnvironmentExtra | Out-Null
    }

    if ($Depends.Count -gt 0) {
        & $NssmExe set $Name DependOnService @Depends | Out-Null
    } else {
        & $NssmExe reset $Name DependOnService | Out-Null
    }

    Write-OK "$Name configured ($Application $Arguments)"
}

# --- register services ---
Write-Step 'NSSM services'

$apiEnv = [ordered]@{
    'ASPNETCORE_URLS'        = "http://0.0.0.0:$ApiPort"
    'ASPNETCORE_ENVIRONMENT' = 'Production'
    'DOTNET_NOLOGO'          = '1'
}
foreach ($k in $EnvKv.Keys) { $apiEnv[$k] = $EnvKv[$k] }

Set-NssmService -Name 'cme-api' `
    -Application 'C:\Program Files\dotnet\dotnet.exe' `
    -Arguments  ('"{0}\CmeSim.Api.dll"' -f $PublishDir) `
    -AppDirectory $PublishDir `
    -EnvVars $apiEnv

Set-NssmService -Name 'cme-qbackend' `
    -Application $VenvPy `
    -Arguments  ('-m uvicorn app.main:app --host 0.0.0.0 --port {0}' -f $QBackendPort) `
    -AppDirectory $QBackDir `
    -EnvVars @{ 'PYTHONUNBUFFERED' = '1' }

Set-NssmService -Name 'cme-flowclassifier' `
    -Application $VenvPy `
    -Arguments  ('-m uvicorn app.main:app --host 0.0.0.0 --port {0}' -f $FlowClassifierPort) `
    -AppDirectory $FlowDir `
    -EnvVars @{ 'PYTHONUNBUFFERED' = '1' }

Set-NssmService -Name 'cme-bridge' `
    -Application $VenvPy `
    -Arguments  ('bridge.py --osc --osc-port {0} --hub-url http://127.0.0.1:{1}/eeg-stream' -f $OscPort, $ApiPort) `
    -AppDirectory $BridgeDir `
    -EnvVars @{ 'PYTHONUNBUFFERED' = '1' } `
    -Depends @('cme-api')

$nodeExe = 'C:\Program Files\nodejs\node.exe'
if (-not (Test-Path $nodeExe)) {
    $alt = (Get-Command node -ErrorAction SilentlyContinue)
    if ($alt) { $nodeExe = $alt.Source } else { throw 'node.exe not found' }
}
Set-NssmService -Name 'cme-dashboard' `
    -Application $nodeExe `
    -Arguments  ('"{0}" -s "{1}\dist" -l {2}' -f $ServeMain, $DashDir, $DashboardPort) `
    -AppDirectory $DashDir

# --- start + wait ---
function Start-AndWait {
    param([string]$Name, [int]$TcpPort = 0, [int]$UdpPort = 0, [int]$TimeoutSec = 45)
    Start-Service $Name -ErrorAction Stop
    if ($TcpPort -le 0 -and $UdpPort -le 0) { Write-OK "$Name started"; return }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
        if ($TcpPort -gt 0 -and (Get-NetTCPConnection -State Listen -LocalPort $TcpPort -ErrorAction SilentlyContinue)) {
            Write-OK "$Name listening on TCP $TcpPort ($([int]$sw.Elapsed.TotalSeconds)s)"; return
        }
        if ($UdpPort -gt 0 -and (Get-NetUDPEndpoint -LocalPort $UdpPort -ErrorAction SilentlyContinue)) {
            Write-OK "$Name listening on UDP $UdpPort ($([int]$sw.Elapsed.TotalSeconds)s)"; return
        }
        Start-Sleep -Milliseconds 500
    }
    Write-Warn "$Name did NOT reach listening state in ${TimeoutSec}s -- check $LogDir\$Name.err.log"
}

Write-Step 'stopping running services (reverse dependency order) to apply new config'
foreach ($s in @('cme-bridge','cme-dashboard','cme-flowclassifier','cme-qbackend','cme-api')) {
    $svc = Get-Service $s -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -ne 'Stopped') {
        try { Stop-Service $s -Force -ErrorAction Stop; Write-Host "  stopped $s" }
        catch { Write-Warn "stop $s -- $($_.Exception.Message)" }
    }
}

Write-Step 'starting services (dependency order)'
Start-AndWait -Name 'cme-api'            -TcpPort $ApiPort
Start-AndWait -Name 'cme-qbackend'       -TcpPort $QBackendPort
Start-AndWait -Name 'cme-flowclassifier' -TcpPort $FlowClassifierPort
Start-AndWait -Name 'cme-dashboard'      -TcpPort $DashboardPort
Start-AndWait -Name 'cme-bridge'         -UdpPort $OscPort

# --- summary ---
Write-Host ''
Write-Host '=== cme-* services ===' -ForegroundColor Cyan
Get-Service cme-* | Sort-Object Name | Format-Table Name, Status, StartType -AutoSize | Out-String | Write-Host

Write-Host '=== CME firewall rules ===' -ForegroundColor Cyan
Get-NetFirewallRule -DisplayName 'CME *' -ErrorAction SilentlyContinue |
    Format-Table DisplayName, Enabled, Direction, Action -AutoSize | Out-String | Write-Host

Write-Host '=== listening ports ===' -ForegroundColor Cyan
Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
    Where-Object { $_.LocalPort -in $ApiPort,$DashboardPort,$QBackendPort,$FlowClassifierPort } |
    Sort-Object LocalPort | Select-Object LocalAddress,LocalPort | Format-Table -AutoSize | Out-String | Write-Host
Get-NetUDPEndpoint -ErrorAction SilentlyContinue |
    Where-Object { $_.LocalPort -eq $OscPort } |
    Select-Object LocalAddress,LocalPort | Format-Table -AutoSize | Out-String | Write-Host

Write-Host "Done."
Write-Host "  Dashboard: http://${VpsIp}:${DashboardPort}"
Write-Host "  MindMonitor: Host=$VpsIp Port=$OscPort, all bands + FFT enabled, then Start streaming"
Write-Host "  Logs:        Get-Content $LogDir\cme-bridge.out.log -Wait"
Write-Host "  Stop/start:  Restart-Service cme-* -Force"
Write-Host "  Uninstall:   .\scripts\Uninstall-VpsServices.ps1"
