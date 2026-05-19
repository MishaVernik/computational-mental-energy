# Allow UDP port 7002 for MindMonitor OSC (run as Administrator)
# MindMonitor sends OSC from phone to PC; Windows Firewall may block it by default.

$ruleName = "Muse Bridge OSC (UDP 7002)"
$port = 7002

$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Rule '$ruleName' already exists." -ForegroundColor Yellow
    exit 0
}

New-NetFirewallRule -DisplayName $ruleName `
    -Direction Inbound `
    -Protocol UDP `
    -LocalPort $port `
    -Action Allow `
    -Profile Any

Write-Host "Added firewall rule: $ruleName (UDP $port)" -ForegroundColor Green
Write-Host "MindMonitor can now send OSC data to this PC." -ForegroundColor Green
