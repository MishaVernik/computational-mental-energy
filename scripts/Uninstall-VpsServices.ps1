<#
.SYNOPSIS
    Stops and removes the cme-* Windows services and the three CME firewall rules.

.DESCRIPTION
    Mirror of Install-VpsServices.ps1. Does NOT delete build artefacts (publish/, .venv/, dist/, logs/, bin/nssm.exe).
    Pass -PurgeArtifacts to also delete those directories.

    Run elevated.
#>
#Requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$PurgeArtifacts
)

$ErrorActionPreference = 'Continue'
Set-StrictMode -Version Latest

$id = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not ([Security.Principal.WindowsPrincipal]$id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host 'ERROR: must run elevated.' -ForegroundColor Red
    exit 1
}

$RepoRoot = Split-Path -Parent $PSScriptRoot
$BinDir   = Join-Path $RepoRoot 'bin'
$NssmExe  = Join-Path $BinDir   'nssm.exe'

$services = @('cme-bridge', 'cme-dashboard', 'cme-flowclassifier', 'cme-qbackend', 'cme-api')

Write-Host '[1/3] stopping services'
foreach ($s in $services) {
    if (Get-Service $s -ErrorAction SilentlyContinue) {
        Write-Host "  stop $s"
        Stop-Service $s -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 200
    }
}

Write-Host '[2/3] removing services'
foreach ($s in $services) {
    if (Get-Service $s -ErrorAction SilentlyContinue) {
        if (Test-Path $NssmExe) {
            & $NssmExe remove $s confirm | Out-Null
        } else {
            & sc.exe delete $s | Out-Null
        }
        Write-Host "  removed $s"
    }
}

Write-Host '[3/3] removing firewall rules'
foreach ($rule in @('CME OSC (MindMonitor)', 'CME API', 'CME Dashboard')) {
    $found = Get-NetFirewallRule -DisplayName $rule -ErrorAction SilentlyContinue
    if ($found) {
        Remove-NetFirewallRule -DisplayName $rule -ErrorAction SilentlyContinue
        Write-Host "  removed $rule"
    }
}

if ($PurgeArtifacts) {
    Write-Host 'purging build artefacts'
    foreach ($p in @('publish','logs','.venv','bin')) {
        $full = Join-Path $RepoRoot $p
        if (Test-Path $full) {
            Remove-Item $full -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  removed $full"
        }
    }
    $dashDist = Join-Path $RepoRoot 'cme-live-dashboard\dist'
    if (Test-Path $dashDist) {
        Remove-Item $dashDist -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  removed $dashDist"
    }
}

Write-Host 'done.'
Get-Service cme-* -ErrorAction SilentlyContinue | Format-Table Name, Status | Out-String | Write-Host
