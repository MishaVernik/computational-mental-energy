# Test script to run architecture benchmarks with different setups
# Make sure the API is running before executing this script

$apiBaseUrl = "http://localhost:5000/api"

Write-Host "=== Architecture Benchmark Testing ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Monolith - Low Load
Write-Host "Test 1: Monolith Architecture - Low Load" -ForegroundColor Yellow
$monolithLow = @{
    name = "Monolith - Low Load Test"
    architecture = "A_Monolith"
    activeClients = 3
    requestsPerClient = 10
    workersCount = 1
    maxConcurrentQpuCalls = 1
    thinkTimeMs = 100
    qpuBackends = 1
    shots = 256
    circuitDepth = 4
    trainingEnabled = $false
    networkProfile = @{ meanMs = 5.0; stdMs = 2.0 }
    dbProfile = @{ meanMs = 10.0; stdMs = 3.0 }
    brokerProfile = @{ meanMs = 2.0; stdMs = 1.0; mode = "Exponential" }
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/run" -Method Post -Body $monolithLow -ContentType "application/json"
    Write-Host "  Started benchmark run: $response" -ForegroundColor Green
    $runId1 = $response
} catch {
    Write-Host "  Error: $_" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 2

# Test 2: Sync Microservices - Low Load
Write-Host ""
Write-Host "Test 2: Sync Microservices Architecture - Low Load" -ForegroundColor Yellow
$syncLow = @{
    name = "Sync Microservices - Low Load Test"
    architecture = "B_SyncMicroservices"
    activeClients = 3
    requestsPerClient = 10
    workersCount = 1
    maxConcurrentQpuCalls = 1
    thinkTimeMs = 100
    qpuBackends = 1
    shots = 256
    circuitDepth = 4
    trainingEnabled = $false
    networkProfile = @{ meanMs = 5.0; stdMs = 2.0 }
    dbProfile = @{ meanMs = 10.0; stdMs = 3.0 }
    brokerProfile = @{ meanMs = 2.0; stdMs = 1.0; mode = "Exponential" }
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/run" -Method Post -Body $syncLow -ContentType "application/json"
    Write-Host "  Started benchmark run: $response" -ForegroundColor Green
    $runId2 = $response
} catch {
    Write-Host "  Error: $_" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# Test 3: Brokered - Low Load
Write-Host ""
Write-Host "Test 3: Brokered Architecture - Low Load" -ForegroundColor Yellow
$brokeredLow = @{
    name = "Brokered - Low Load Test"
    architecture = "C_Brokered"
    activeClients = 3
    requestsPerClient = 10
    workersCount = 4
    maxConcurrentQpuCalls = 2
    thinkTimeMs = 100
    qpuBackends = 1
    shots = 256
    circuitDepth = 4
    trainingEnabled = $false
    networkProfile = @{ meanMs = 5.0; stdMs = 2.0 }
    dbProfile = @{ meanMs = 10.0; stdMs = 3.0 }
    brokerProfile = @{ meanMs = 2.0; stdMs = 1.0; mode = "Exponential" }
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/run" -Method Post -Body $brokeredLow -ContentType "application/json"
    Write-Host "  Started benchmark run: $response" -ForegroundColor Green
    $runId3 = $response
} catch {
    Write-Host "  Error: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Waiting for benchmarks to complete (30 seconds) ===" -ForegroundColor Cyan
Start-Sleep -Seconds 30

# Check results
Write-Host ""
Write-Host "=== Checking Results ===" -ForegroundColor Cyan

if ($runId1) {
    Write-Host ""
    Write-Host "Monolith Results:" -ForegroundColor Yellow
    try {
        $result1 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/$runId1" -Method Get
        Write-Host "  Status: $($result1.status)" -ForegroundColor $(if ($result1.status -eq "Completed") { "Green" } else { "Yellow" })
        Write-Host "  Avg Latency: $([math]::Round($result1.avgLatencyMs, 2)) ms"
        Write-Host "  P95 Latency: $([math]::Round($result1.p95LatencyMs, 2)) ms"
        Write-Host "  Throughput: $([math]::Round($result1.throughputRps, 2)) req/s"
        Write-Host "  Success: $($result1.successCount), Failed: $($result1.failCount)"
    } catch {
        Write-Host "  Error fetching results: $_" -ForegroundColor Red
    }
}

if ($runId2) {
    Write-Host ""
    Write-Host "Sync Microservices Results:" -ForegroundColor Yellow
    try {
        $result2 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/$runId2" -Method Get
        Write-Host "  Status: $($result2.status)" -ForegroundColor $(if ($result2.status -eq "Completed") { "Green" } else { "Yellow" })
        Write-Host "  Avg Latency: $([math]::Round($result2.avgLatencyMs, 2)) ms"
        Write-Host "  P95 Latency: $([math]::Round($result2.p95LatencyMs, 2)) ms"
        Write-Host "  Throughput: $([math]::Round($result2.throughputRps, 2)) req/s"
        Write-Host "  Success: $($result2.successCount), Failed: $($result2.failCount)"
    } catch {
        Write-Host "  Error fetching results: $_" -ForegroundColor Red
    }
}

if ($runId3) {
    Write-Host ""
    Write-Host "Brokered Results:" -ForegroundColor Yellow
    try {
        $result3 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/$runId3" -Method Get
        Write-Host "  Status: $($result3.status)" -ForegroundColor $(if ($result3.status -eq "Completed") { "Green" } else { "Yellow" })
        Write-Host "  Avg Latency: $([math]::Round($result3.avgLatencyMs, 2)) ms"
        Write-Host "  P95 Latency: $([math]::Round($result3.p95LatencyMs, 2)) ms"
        Write-Host "  Throughput: $([math]::Round($result3.throughputRps, 2)) req/s"
        Write-Host "  Success: $($result3.successCount), Failed: $($result3.failCount)"
    } catch {
        Write-Host "  Error fetching results: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Benchmark History ===" -ForegroundColor Cyan
try {
    $history = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/history?limit=5" -Method Get
    Write-Host "Recent runs:" -ForegroundColor Yellow
    foreach ($run in $history) {
        Write-Host "  - $($run.name) [$($run.architecture)]: $($run.status) - Avg: $([math]::Round($run.avgLatencyMs, 2))ms, Throughput: $([math]::Round($run.throughputRps, 2)) req/s"
    }
} catch {
    Write-Host "  Error fetching history: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Green


