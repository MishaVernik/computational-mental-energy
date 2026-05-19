# Stop CME Live Pipeline (same ports as run-all-services.ps1)
$ErrorActionPreference = "SilentlyContinue"

function Stop-Port {
    param([int]$Port, [string]$Proto = "TCP")
    $pids = @()
    if ($Proto -eq "TCP") {
        $conn = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
        if ($conn) { $pids = $conn.OwningProcess | Sort-Object -Unique }
    } else {
        $conn = Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue
        if ($conn) { $pids = $conn.OwningProcess | Sort-Object -Unique }
    }
    netstat -ano | Select-String ":$Port\s" | ForEach-Object {
        if ($_ -match '\s+(\d+)\s*$') { $pids += [int]$matches[1] }
    }
    $pids = $pids | Sort-Object -Unique
    foreach ($procId in $pids) {
        if ($procId -gt 0) {
            $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
            if ($proc) {
                Write-Host "Stopping $($proc.ProcessName) (PID $procId) on ${Proto}:$Port"
                Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

Write-Host "`n=== Stopping processes on CME ports ===" -ForegroundColor Yellow
Stop-Port -Port 5000
Stop-Port -Port 8001
Stop-Port -Port 8002
Stop-Port -Port 7002 -Proto UDP
Stop-Port -Port 7002
Stop-Port -Port 3001

Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like '*bridge.py*' } | ForEach-Object {
    Write-Host "Stopping muse-bridge (PID $($_.ProcessId))"
    Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
}

Write-Host "`nAll CME services stopped." -ForegroundColor Green
