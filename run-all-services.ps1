# CME Live Pipeline - Kill ports and launch all services
# Ports: 5000 (API), 8001 (QBackend), 8002 (FlowClassifier), 7002 (OSC), 3001 (Dashboard)

$ErrorActionPreference = "SilentlyContinue"
$root = "c:\Data\phd\fmpi\lab45"

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
    # Also use netstat (more reliable when Get-NetTCPConnection misses listeners)
    netstat -ano | Select-String ":$Port\s" | ForEach-Object {
        if ($_ -match '\s+(\d+)\s*$') { $pids += [int]$matches[1] }
    }
    $pids = $pids | Sort-Object -Unique
    foreach ($pid in $pids) {
        if ($pid -gt 0) {
            $proc = Get-Process -Id $pid -ErrorAction SilentlyContinue
            if ($proc) {
                Write-Host "Stopping $($proc.ProcessName) (PID $pid) on $Proto`:$Port"
                Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

Write-Host "`n=== Stopping processes on CME ports ===" -ForegroundColor Yellow
Stop-Port -Port 5000   # API
Stop-Port -Port 8001   # QBackend
Stop-Port -Port 8002   # FlowClassifier
Stop-Port -Port 7002 -Proto UDP   # OSC (muse-bridge)
Stop-Port -Port 7002   # OSC TCP
Stop-Port -Port 3001   # Live dashboard
Start-Sleep -Seconds 3

Write-Host "`n=== Starting CME Live Pipeline ===" -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$root\CmeSim.Api'; dotnet run"
Start-Sleep -Seconds 5

Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$root\qbackend'; python -m uvicorn app.main:app --host 0.0.0.0 --port 8001"
Start-Sleep -Seconds 2
Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$root\flow-classifier'; python -m uvicorn app.main:app --host 0.0.0.0 --port 8002"
Start-Sleep -Seconds 2

# Wait for API to be ready before starting bridge (API can take 10-15s to start)
Write-Host "Waiting for API on port 5000..." -ForegroundColor Gray
$apiReady = $false
for ($i = 1; $i -le 30; $i++) {
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $tcp.Connect("127.0.0.1", 5000)
        $tcp.Close()
        $apiReady = $true
        Write-Host "API ready (attempt $i)" -ForegroundColor Green
        break
    } catch {
        Start-Sleep -Seconds 2
    }
}
if (-not $apiReady) { Write-Host "WARNING: API may not be ready, starting bridge anyway" -ForegroundColor Yellow }

# Re-kill 7002 and any lingering bridge.py before muse-bridge
Stop-Port -Port 7002 -Proto UDP
Stop-Port -Port 7002
Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like '*bridge.py*' } | ForEach-Object {
    Write-Host "Stopping muse-bridge (PID $($_.ProcessId))"
    Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 2

Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$root\muse-bridge'; python bridge.py --osc --hub-url http://localhost:5000/eeg-stream"
Start-Sleep -Seconds 1

# Use cmd for npm (bypasses PowerShell execution policy)
Start-Process cmd -ArgumentList "/k","cd /d $root\cme-live-dashboard && npm run dev"

Write-Host "`nAll services launched. Dashboard: http://localhost:3001" -ForegroundColor Cyan
