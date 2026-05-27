<#
.SYNOPSIS
    Bind cmeflow.entertainmentpl.com (HTTPS) to the CME dashboard + API via IIS reverse proxy.

.DESCRIPTION
    The VPS already runs IIS on 80/443. This script:
      1. Installs IIS URL Rewrite + ARR (if missing) and enables ARR proxy + WebSockets.
      2. Creates site C:\inetpub\cmeflow with reverse-proxy rules (dashboard :3001, API :5000).
      3. Registers IIS site on port 80 for the host header (Lets Encrypt HTTP-01).
      4. Runs win-acme (wacs) to obtain a Lets Encrypt certificate and bind HTTPS.
      5. Copies web.config with HTTP->HTTPS redirect.

    Prerequisites:
      - DNS A record: <Domain> -> this server's public IP (default 161.97.146.52).
      - cme-api and cme-dashboard NSSM services listening on 127.0.0.1:5000 and :3001.
      - Run elevated.

.PARAMETER Domain
    Hostname (default cmeflow.entertainmentpl.com).

.PARAMETER LetsEncryptEmail
    Contact email for Lets Encrypt (required unless -SkipCertificate).

.PARAMETER Remove
    Remove the IIS site (does not uninstall ARR or certificates).

.EXAMPLE
    .\scripts\Setup-CmeDomainHttps.ps1 -LetsEncryptEmail admin@entertainmentpl.com
#>
#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$Domain = 'cmeflow.entertainmentpl.com',
    [string]$VpsIp = '161.97.146.52',
    [string]$LetsEncryptEmail = '',
    [string]$IisSiteName = 'cme-cmeflow',
    [string]$SitePath = 'C:\inetpub\cmeflow',
    [switch]$SkipCertificate,
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Write-Step { param([string]$m) Write-Host "[$([DateTime]::Now.ToString('HH:mm:ss'))] $m" -ForegroundColor Cyan }
function Write-OK   { param([string]$m) Write-Host "  ok  $m" -ForegroundColor Green }
function Write-Warn { param([string]$m) Write-Host "  !!  $m" -ForegroundColor Yellow }

$id = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not ([Security.Principal.WindowsPrincipal]$id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host 'ERROR: run elevated (Run as Administrator).' -ForegroundColor Red
    exit 1
}

$RepoRoot = Split-Path -Parent $PSScriptRoot
$BinDir   = Join-Path $RepoRoot 'bin'
$WacsDir  = Join-Path $BinDir 'win-acme'
$WacsExe  = Join-Path $WacsDir 'wacs.exe'
$WebConfigTemplate = Join-Path $PSScriptRoot 'iis-cmeflow-web.config'

Import-Module WebAdministration -ErrorAction Stop

if ($Remove) {
    Write-Step "Removing IIS site $IisSiteName"
    if (Get-Website -Name $IisSiteName -ErrorAction SilentlyContinue) {
        Remove-Website -Name $IisSiteName
        Write-OK 'website removed'
    }
    if (Test-Path $SitePath) {
        Remove-Item $SitePath -Recurse -Force -ErrorAction SilentlyContinue
        Write-OK "removed $SitePath"
    }
    Write-Host 'Certificate store entries are left in place; remove manually in certlm.msc if needed.' -ForegroundColor Yellow
    exit 0
}

Write-Step "DNS check for $Domain"
try {
    $dns = [System.Net.Dns]::GetHostAddresses($Domain) | Where-Object { $_.AddressFamily -eq 'InterNetwork' }
    $ips = $dns | ForEach-Object { $_.IPAddressToString }
    if ($ips -contains $VpsIp) {
        Write-OK "$Domain -> $VpsIp"
    } else {
        Write-Warn "$Domain resolves to [$($ips -join ', ')] but expected $VpsIp - continue only if DNS is propagating."
    }
} catch {
    Write-Warn "Could not resolve $Domain : $_"
}

Write-Step 'Backend ports (cme-dashboard :3001, cme-api :5000)'
foreach ($port in 3001, 5000) {
    $listen = Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listen) { Write-OK "port $port listening" }
    else { Write-Warn "port $port not listening - start NSSM services before testing the site" }
}

Write-Step 'IIS features'
$features = @(
    'IIS-WebServerRole', 'IIS-WebServer', 'IIS-CommonHttpFeatures', 'IIS-HttpErrors',
    'IIS-ApplicationDevelopment', 'IIS-NetFxExtensibility45', 'IIS-HealthAndDiagnostics',
    'IIS-HttpLogging', 'IIS-Security', 'IIS-RequestFiltering', 'IIS-Performance',
    'IIS-WebServerManagementTools', 'IIS-ManagementConsole', 'IIS-WebSockets'
)
foreach ($f in $features) {
    $state = (Get-WindowsOptionalFeature -Online -FeatureName $f -ErrorAction SilentlyContinue).State
    if ($state -ne 'Enabled') {
        Write-Host "  enabling $f ..."
        Enable-WindowsOptionalFeature -Online -FeatureName $f -All -NoRestart | Out-Null
    }
}
Write-OK 'IIS features'

function Install-MsiIfMissing {
    param([string]$ProductNamePattern, [string]$MsiUrl, [string]$MsiFileName)
    $installed = Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall' -ErrorAction SilentlyContinue |
        ForEach-Object { Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue } |
        Where-Object {
            $dnProp = $_.PSObject.Properties['DisplayName']
            $dnProp -and $dnProp.Value -and ($dnProp.Value -like $ProductNamePattern)
        } |
        Select-Object -First 1
    if ($installed) {
        Write-OK "$($installed.DisplayName) already installed"
        return
    }
    New-Item -ItemType Directory -Force -Path $BinDir | Out-Null
    $msi = Join-Path $BinDir $MsiFileName
    if (-not (Test-Path $msi)) {
        Write-Host "  downloading $MsiUrl ..."
        Invoke-WebRequest -Uri $MsiUrl -OutFile $msi -UseBasicParsing
    }
    Write-Host "  installing $MsiFileName ..."
    Start-Process msiexec.exe -ArgumentList "/i `"$msi`" /qn /norestart" -Wait -NoNewWindow
    Write-OK "installed $MsiFileName"
}

Write-Step 'URL Rewrite + ARR'
Install-MsiIfMissing -ProductNamePattern 'IIS URL Rewrite*' `
    -MsiUrl 'https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi' `
    -MsiFileName 'rewrite_amd64_en-US.msi'
# Official IIS fwlink (direct download.microsoft.com GUIDs rot; fwlink resolves to current MSI)
Install-MsiIfMissing -ProductNamePattern 'IIS Application Request Routing*' `
    -MsiUrl 'https://go.microsoft.com/fwlink/?LinkID=615136' `
    -MsiFileName 'requestRouter_amd64.msi'

Write-Step 'Enable ARR reverse proxy'
Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter 'system.webServer/proxy' -Name 'enabled' -Value 'True'
Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter 'system.webServer/proxy' -Name 'preserveHostHeader' -Value 'True'
Write-OK 'ARR proxy enabled'

Write-Step "IIS site path $SitePath"
New-Item -ItemType Directory -Force -Path $SitePath | Out-Null
if (-not (Test-Path $WebConfigTemplate)) { throw "Missing template: $WebConfigTemplate" }
Copy-Item $WebConfigTemplate (Join-Path $SitePath 'web.config') -Force
Set-Content -Path (Join-Path $SitePath 'index.html') -Value '<!DOCTYPE html><html><body>CME reverse proxy</body></html>' -Encoding UTF8
Write-OK 'web.config deployed'

Write-Step "IIS site $IisSiteName"
if (Get-Website -Name $IisSiteName -ErrorAction SilentlyContinue) {
    Remove-Website -Name $IisSiteName
}
New-Website -Name $IisSiteName -PhysicalPath $SitePath -Port 80 -HostHeader $Domain -Force | Out-Null
Write-OK "http://$Domain/ -> proxy (port 80 binding)"

$site = Get-Website -Name $IisSiteName
$siteId = $site.Id

if (-not $SkipCertificate) {
    if ([string]::IsNullOrWhiteSpace($LetsEncryptEmail)) {
        throw 'Pass -LetsEncryptEmail or use -SkipCertificate to configure TLS manually.'
    }

    Write-Step 'win-acme (Lets Encrypt)'
    $wacsSettings = Join-Path $WacsDir 'settings.json'
    if ((Test-Path $WacsDir) -and (-not (Test-Path $WacsExe) -or -not (Test-Path $wacsSettings))) {
        Remove-Item $WacsDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (-not (Test-Path $WacsExe)) {
        $zip = Join-Path $env:TEMP "win-acme-$([guid]::NewGuid()).zip"
        $extract = Join-Path $env:TEMP "win-acme-extract-$([guid]::NewGuid())"
        try {
            $release = Invoke-RestMethod -Uri 'https://api.github.com/repos/win-acme/win-acme/releases/latest' -UseBasicParsing
            $asset = $release.assets | Where-Object { $_.name -match '^win-acme\..+\.x64\.pluggable\.zip$' } | Select-Object -First 1
            if (-not $asset) {
                $asset = $release.assets | Where-Object { $_.name -match '^win-acme\..+\.x64\.trimmed\.zip$' } | Select-Object -First 1
            }
            if (-not $asset) { throw 'No win-acme x64 zip asset found on GitHub releases' }
            Write-Host "  downloading $($asset.name) ..."
            Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -UseBasicParsing
            Expand-Archive -Path $zip -DestinationPath $extract -Force
            $root = Get-ChildItem $extract -Recurse -Filter 'wacs.exe' | Select-Object -First 1
            if (-not $root) { throw 'wacs.exe not found in archive' }
            $payloadDir = $root.Directory.FullName
            New-Item -ItemType Directory -Force -Path $WacsDir | Out-Null
            Copy-Item -Path (Join-Path $payloadDir '*') -Destination $WacsDir -Recurse -Force
            if (-not (Test-Path $wacsSettings)) { throw 'settings.json missing after extract' }
            Write-OK "win-acme -> $WacsDir"
        } catch {
            throw "win-acme download failed: $_"
        } finally {
            Remove-Item $zip -Force -ErrorAction SilentlyContinue
            Remove-Item $extract -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Write-Host "  requesting certificate for $Domain (site id $siteId) ..."
    $wacsArgs = @(
        '--source', 'iis',
        '--siteid', $siteId,
        '--host', $Domain,
        '--accepttos',
        '--emailaddress', $LetsEncryptEmail,
        '--installation', 'iis',
        '--installationsiteid', $siteId,
        '--notaskscheduler'
    )
    Push-Location $WacsDir
    try {
        & .\wacs.exe @wacsArgs
    } finally {
        Pop-Location
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Warn "win-acme exited $LASTEXITCODE - check output; re-run or bind HTTPS manually in IIS Manager."
    } else {
        Write-OK 'certificate installed'
    }
} else {
    Write-Warn 'Skipped certificate (-SkipCertificate). Add HTTPS binding manually in IIS Manager.'
}

Write-Host ''
Write-Host '=== CME domain setup complete ===' -ForegroundColor Green
Write-Host "  Site:     $IisSiteName  (id $siteId)"
Write-Host "  URL:      https://$Domain/"
Write-Host "  API hub:  https://$Domain/eeg-stream"
Write-Host ''
Write-Host 'Next steps:' -ForegroundColor Yellow
Write-Host '  1. Rebuild dashboard:  cd cme-live-dashboard; npm run build'
Write-Host '  2. Restart dashboard:  Restart-Service cme-dashboard'
Write-Host '  3. Redeploy API:       .\scripts\Redeploy-VpsServices.ps1'
Write-Host "  4. Open https://$Domain/ and start a session"
Write-Host ''
Write-Host 'DNS must point here before Lets Encrypt can succeed. Allow TCP 80 and 443 inbound.' -ForegroundColor DarkGray
