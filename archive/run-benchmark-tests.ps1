# Comprehensive benchmark testing script
# This script checks services, runs benchmarks, and displays results

$apiBaseUrl = "http://localhost:5000/api"
$qbackendUrl = "http://localhost:8001"

Write-Host "=== Architecture Benchmark Testing ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if services are running
Write-Host "Step 1: Checking services..." -ForegroundColor Yellow

$apiRunning = $false
$qbackendRunning = $false

try {
    $response = Invoke-WebRequest -Uri "$apiBaseUrl/dashboard/summary" -Method Get -TimeoutSec 2 -ErrorAction Stop
    $apiRunning = $true
    Write-Host "  ✓ API is running on port 5000" -ForegroundColor Green
} catch {
    Write-Host "  ✗ API is not running on port 5000" -ForegroundColor Red
    Write-Host "    Start it with: cd CmeSim.Api && dotnet run" -ForegroundColor Yellow
}

try {
    $response = Invoke-WebRequest -Uri "$qbackendUrl/health" -Method Get -TimeoutSec 2 -ErrorAction Stop
    $qbackendRunning = $true
    Write-Host "  ✓ Quantum backend is running on port 8001" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Quantum backend is not running on port 8001" -ForegroundColor Red
    Write-Host "    Start it with: cd qbackend && python -m uvicorn app.main:app --port 8001" -ForegroundColor Yellow
}

if (-not $apiRunning -or -not $qbackendRunning) {
    Write-Host ""
    Write-Host "Please start the required services and run this script again." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Running benchmark tests..." -ForegroundColor Yellow
Write-Host ""

# Test 1: Monolith - Low Load
Write-Host "  [1/3] Starting Monolith benchmark..." -ForegroundColor Cyan
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
    $runId1 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/run" -Method Post -Body $monolithLow -ContentType "application/json"
    Write-Host "    ✓ Started: $runId1" -ForegroundColor Green
} catch {
    Write-Host "    ✗ Error: $_" -ForegroundColor Red
    $runId1 = $null
}

Start-Sleep -Seconds 2

# Test 2: Sync Microservices - Low Load
Write-Host "  [2/3] Starting Sync Microservices benchmark..." -ForegroundColor Cyan
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
    $runId2 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/run" -Method Post -Body $syncLow -ContentType "application/json"
    Write-Host "    ✓ Started: $runId2" -ForegroundColor Green
} catch {
    Write-Host "    ✗ Error: $_" -ForegroundColor Red
    $runId2 = $null
}

Start-Sleep -Seconds 2

# Test 3: Brokered - Low Load
Write-Host "  [3/3] Starting Brokered benchmark..." -ForegroundColor Cyan
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
    $runId3 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/run" -Method Post -Body $brokeredLow -ContentType "application/json"
    Write-Host "    ✓ Started: $runId3" -ForegroundColor Green
} catch {
    Write-Host "    ✗ Error: $_" -ForegroundColor Red
    $runId3 = $null
}

Write-Host ""
Write-Host "Step 3: Waiting for benchmarks to complete..." -ForegroundColor Yellow
Write-Host "  (This may take 30-60 seconds depending on load)" -ForegroundColor Gray

$maxWait = 120 # 2 minutes max
$waitInterval = 5
$elapsed = 0

while ($elapsed -lt $maxWait) {
    Start-Sleep -Seconds $waitInterval
    $elapsed += $waitInterval
    
    $allComplete = $true
    $statuses = @()
    
    if ($runId1) {
        try {
            $result1 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/$runId1" -Method Get -ErrorAction Stop
            $statuses += "Monolith: $($result1.status)"
            if ($result1.status -ne "Completed" -and $result1.status -ne "Failed") {
                $allComplete = $false
            }
        } catch { $allComplete = $false }
    }
    
    if ($runId2) {
        try {
            $result2 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/$runId2" -Method Get -ErrorAction Stop
            $statuses += "Sync: $($result2.status)"
            if ($result2.status -ne "Completed" -and $result2.status -ne "Failed") {
                $allComplete = $false
            }
        } catch { $allComplete = $false }
    }
    
    if ($runId3) {
        try {
            $result3 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/$runId3" -Method Get -ErrorAction Stop
            $statuses += "Brokered: $($result3.status)"
            if ($result3.status -ne "Completed" -and $result3.status -ne "Failed") {
                $allComplete = $false
            }
        } catch { $allComplete = $false }
    }
    
    if ($statuses.Count -gt 0) {
        Write-Host "    [$elapsed s] $($statuses -join ', ')" -ForegroundColor Gray
    }
    
    if ($allComplete) {
        break
    }
}

Write-Host ""
Write-Host "Step 4: Results Comparison" -ForegroundColor Yellow
Write-Host ""

$results = @()

if ($runId1) {
    try {
        $result1 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/$runId1" -Method Get
        $results += [PSCustomObject]@{
            Architecture = "Monolith (A)"
            Status = $result1.status
            AvgLatency = [math]::Round($result1.avgLatencyMs, 2)
            P95Latency = [math]::Round($result1.p95LatencyMs, 2)
            P99Latency = [math]::Round($result1.p99LatencyMs, 2)
            Throughput = [math]::Round($result1.throughputRps, 2)
            Success = $result1.successCount
            Failed = $result1.failCount
            RunId = $runId1
        }
    } catch {
        Write-Host "  ✗ Error fetching Monolith results: $_" -ForegroundColor Red
    }
}

if ($runId2) {
    try {
        $result2 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/$runId2" -Method Get
        $results += [PSCustomObject]@{
            Architecture = "Sync Microservices (B)"
            Status = $result2.status
            AvgLatency = [math]::Round($result2.avgLatencyMs, 2)
            P95Latency = [math]::Round($result2.p95LatencyMs, 2)
            P99Latency = [math]::Round($result2.p99LatencyMs, 2)
            Throughput = [math]::Round($result2.throughputRps, 2)
            Success = $result2.successCount
            Failed = $result2.failCount
            RunId = $runId2
        }
    } catch {
        Write-Host "  ✗ Error fetching Sync Microservices results: $_" -ForegroundColor Red
    }
}

if ($runId3) {
    try {
        $result3 = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/$runId3" -Method Get
        $results += [PSCustomObject]@{
            Architecture = "Brokered (C)"
            Status = $result3.status
            AvgLatency = [math]::Round($result3.avgLatencyMs, 2)
            P95Latency = [math]::Round($result3.p95LatencyMs, 2)
            P99Latency = [math]::Round($result3.p99LatencyMs, 2)
            Throughput = [math]::Round($result3.throughputRps, 2)
            Success = $result3.successCount
            Failed = $result3.failCount
            RunId = $runId3
        }
    } catch {
        Write-Host "  ✗ Error fetching Brokered results: $_" -ForegroundColor Red
    }
}

if ($results.Count -gt 0) {
    $results | Format-Table -AutoSize
    
    Write-Host ""
    Write-Host "Step 5: Exporting Results" -ForegroundColor Yellow
    
    $exportDir = "benchmark-exports"
    if (-not (Test-Path $exportDir)) {
        New-Item -ItemType Directory -Path $exportDir | Out-Null
    }
    
    foreach ($result in $results) {
        if ($result.Status -eq "Completed") {
            try {
                # Export JSON
                $jsonFile = "$exportDir/benchmark-$($result.RunId).json"
                Invoke-WebRequest -Uri "$apiBaseUrl/benchmarks/$($result.RunId)/export?format=json" -OutFile $jsonFile
                Write-Host "  ✓ Exported JSON: $jsonFile" -ForegroundColor Green
                
                # Export CSV
                $csvFile = "$exportDir/benchmark-$($result.RunId).csv"
                Invoke-WebRequest -Uri "$apiBaseUrl/benchmarks/$($result.RunId)/export?format=csv" -OutFile $csvFile
                Write-Host "  ✓ Exported CSV: $csvFile" -ForegroundColor Green
            } catch {
                Write-Host "  ✗ Error exporting $($result.Architecture): $_" -ForegroundColor Red
            }
        }
    }
    
    Write-Host ""
    Write-Host "Step 6: Petri Net Parameters" -ForegroundColor Yellow
    
    foreach ($result in $results) {
        if ($result.Status -eq "Completed") {
            try {
                $petriParams = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/$($result.RunId)/petri-params" -Method Get
                Write-Host ""
                Write-Host "  $($result.Architecture) Petri Net Parameters:" -ForegroundColor Cyan
                Write-Host "    Workers: $($petriParams.workers)" -ForegroundColor White
                Write-Host "    QPU Count: $($petriParams.qpuCount)" -ForegroundColor White
                Write-Host "    QPU Concurrency Gate: $($petriParams.qpuConcurrencyGate)" -ForegroundColor White
                Write-Host "    Transition Delays:" -ForegroundColor White
                Write-Host "      Validate: $([math]::Round($petriParams.validateDelay.meanMs, 2))ms ± $([math]::Round($petriParams.validateDelay.stdMs, 2))ms" -ForegroundColor Gray
                Write-Host "      Preprocess: $([math]::Round($petriParams.preprocessDelay.meanMs, 2))ms ± $([math]::Round($petriParams.preprocessDelay.stdMs, 2))ms" -ForegroundColor Gray
                Write-Host "      QPU: $([math]::Round($petriParams.qpuDelay.meanMs, 2))ms ± $([math]::Round($petriParams.qpuDelay.stdMs, 2))ms" -ForegroundColor Gray
                Write-Host "      DB Write: $([math]::Round($petriParams.dbWriteDelay.meanMs, 2))ms ± $([math]::Round($petriParams.dbWriteDelay.stdMs, 2))ms" -ForegroundColor Gray
                Write-Host "    Queue Stats:" -ForegroundColor White
                Write-Host "      QPU Queue: Avg=$([math]::Round($petriParams.qpuQueueStats.avgLength, 2)), Max=$($petriParams.qpuQueueStats.maxLength), Util=$([math]::Round($petriParams.qpuQueueStats.utilization * 100, 1))%" -ForegroundColor Gray
                Write-Host "      Broker Queue: Avg=$([math]::Round($petriParams.brokerQueueStats.avgLength, 2)), Max=$($petriParams.brokerQueueStats.maxLength), Util=$([math]::Round($petriParams.brokerQueueStats.utilization * 100, 1))%" -ForegroundColor Gray
            } catch {
                Write-Host "  ✗ Error fetching Petri params for $($result.Architecture): $_" -ForegroundColor Red
            }
        }
    }
    
    Write-Host ""
    Write-Host "Step 7: Benchmark History" -ForegroundColor Yellow
    try {
        $history = Invoke-RestMethod -Uri "$apiBaseUrl/benchmarks/history?limit=5" -Method Get
        Write-Host ""
        Write-Host "  Recent benchmark runs:" -ForegroundColor Cyan
        foreach ($run in $history) {
            $statusColor = if ($run.status -eq "Completed") { "Green" } elseif ($run.status -eq "Failed") { "Red" } else { "Yellow" }
            Write-Host "    • $($run.name) [$($run.architecture)]" -ForegroundColor White
            Write-Host "      Status: $($run.status) | Avg: $([math]::Round($run.avgLatencyMs, 2))ms | Throughput: $([math]::Round($run.throughputRps, 2)) req/s" -ForegroundColor $statusColor
        }
    } catch {
        Write-Host "  ✗ Error fetching history: $_" -ForegroundColor Red
    }
} else {
    Write-Host "  ✗ No results available" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Benchmark Testing Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Results exported to: $exportDir/" -ForegroundColor Cyan
Write-Host "View dashboard at: http://localhost:3000 (Architectures Bench tab)" -ForegroundColor Cyan


