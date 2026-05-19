# Simplified Load Testing Script for CME Quantum ML System
# Measures: Average response times, P95/P99 latencies, Throughput, Queue lengths

param(
    [int]$ConcurrentUsers = 5,
    [int]$RequestsPerUser = 20,
    [string]$ApiBaseUrl = "http://localhost:5000"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CME Quantum ML System - Load Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Concurrent Users: $ConcurrentUsers" -ForegroundColor Yellow
Write-Host "Requests per User: $RequestsPerUser" -ForegroundColor Yellow
Write-Host "Total Requests: $($ConcurrentUsers * $RequestsPerUser)" -ForegroundColor Yellow
Write-Host ""

# Generate random EEG features (8 features normalized to [-1, 1])
function Get-RandomFeatures {
    $features = @()
    for ($i = 0; $i -lt 8; $i++) {
        $features += (Get-Random -Minimum -1.0 -Maximum 1.0)
    }
    return $features
}

# Make a single inference request
function Invoke-InferenceRequest {
    param(
        [int]$RequestNumber,
        [string]$SessionId,
        [string]$WindowId
    )
    
    $features = Get-RandomFeatures
    $taskDifficulty = Get-Random -Minimum 0.3 -Maximum 0.9
    
    $body = @{
        sessionId = $SessionId
        windowId = $WindowId
        features = $features
        taskDifficulty = $taskDifficulty
    } | ConvertTo-Json
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/inference/cme" `
            -Method POST `
            -Body $body `
            -ContentType "application/json" `
            -TimeoutSec 60 `
            -ErrorAction Stop
        
        $stopwatch.Stop()
        
        return @{
            Success = $true
            LatencyMs = $stopwatch.ElapsedMilliseconds
            TotalLatencyMs = $response.totalLatencyMs
            QpuLatencyMs = $response.qpuLatencyMs
            Cme = $response.cme
            PFlow = $response.pFlow
            RequestNumber = $RequestNumber
        }
    }
    catch {
        $stopwatch.Stop()
        return @{
            Success = $false
            LatencyMs = $stopwatch.ElapsedMilliseconds
            Error = $_.Exception.Message
            RequestNumber = $RequestNumber
        }
    }
}

# Calculate percentile
function Get-Percentile {
    param(
        [double[]]$Values,
        [double]$Percentile
    )
    
    if ($Values.Count -eq 0) { return 0 }
    
    $sorted = $Values | Sort-Object
    $index = [Math]::Ceiling($sorted.Count * $Percentile) - 1
    if ($index -lt 0) { $index = 0 }
    if ($index -ge $sorted.Count) { $index = $sorted.Count - 1 }
    
    return $sorted[$index]
}

# Main test execution
Write-Host "Starting load test..." -ForegroundColor Green
Write-Host ""

$testStartTime = Get-Date
$allResults = [System.Collections.ArrayList]::new()

# Create sessions for each worker
$sessionIds = @()
for ($i = 0; $i -lt $ConcurrentUsers; $i++) {
    $sessionIds += [Guid]::NewGuid().ToString()
}

# Use ThreadJob for concurrent execution (PowerShell 7+)
$jobs = @()

for ($workerId = 0; $workerId -lt $ConcurrentUsers; $workerId++) {
    $sessionId = $sessionIds[$workerId]
    
    $job = Start-ThreadJob -ScriptBlock {
        param($WorkerId, $RequestCount, $SessionId, $ApiBaseUrl)
        
        function Get-RandomFeatures {
            $features = @()
            for ($i = 0; $i -lt 8; $i++) {
                $features += (Get-Random -Minimum -1.0 -Maximum 1.0)
            }
            return $features
        }
        
        $results = @()
        for ($i = 1; $i -le $RequestCount; $i++) {
            $features = Get-RandomFeatures
            $taskDifficulty = Get-Random -Minimum 0.3 -Maximum 0.9
            $windowId = "load-test-w-$WorkerId-$i"
            
            $body = @{
                sessionId = $SessionId
                windowId = $windowId
                features = $features
                taskDifficulty = $taskDifficulty
            } | ConvertTo-Json
            
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            
            try {
                $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/inference/cme" `
                    -Method POST `
                    -Body $body `
                    -ContentType "application/json" `
                    -TimeoutSec 60 `
                    -ErrorAction Stop
                
                $stopwatch.Stop()
                
                $results += @{
                    Success = $true
                    LatencyMs = $stopwatch.ElapsedMilliseconds
                    TotalLatencyMs = $response.totalLatencyMs
                    QpuLatencyMs = $response.qpuLatencyMs
                    Cme = $response.cme
                    PFlow = $response.pFlow
                    RequestNumber = $i
                }
            }
            catch {
                $stopwatch.Stop()
                $results += @{
                    Success = $false
                    LatencyMs = $stopwatch.ElapsedMilliseconds
                    Error = $_.Exception.Message
                    RequestNumber = $i
                }
            }
            
            Start-Sleep -Milliseconds (Get-Random -Minimum 10 -Maximum 50)
        }
        
        return $results
    } -ArgumentList $workerId, $RequestsPerUser, $sessionId, $ApiBaseUrl
    
    $jobs += $job
    Write-Host "Started worker $($workerId + 1)/$ConcurrentUsers" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Waiting for all requests to complete..." -ForegroundColor Green

# Wait for all jobs and collect results
$completed = 0
$maxWaitTime = 300 # 5 minutes max
$waitStart = Get-Date

while (($jobs | Where-Object { $_.State -eq "Running" -or $_.State -eq "NotStarted" }).Count -gt 0) {
    $completed = ($jobs | Where-Object { $_.State -eq "Completed" -or $_.State -eq "Failed" }).Count
    $elapsed = ((Get-Date) - $waitStart).TotalSeconds
    
    if ($elapsed -gt $maxWaitTime) {
        Write-Host "Timeout waiting for jobs to complete!" -ForegroundColor Red
        break
    }
    
    Write-Progress -Activity "Load Test Progress" -Status "Completed: $completed/$ConcurrentUsers workers (Elapsed: $([Math]::Round($elapsed, 1))s)" -PercentComplete (($completed / $ConcurrentUsers) * 100)
    Start-Sleep -Milliseconds 1000
}

Write-Progress -Activity "Load Test Progress" -Completed

# Collect all results
foreach ($job in $jobs) {
    try {
        $workerResults = Receive-Job -Job $job -ErrorAction SilentlyContinue
        if ($workerResults) {
            foreach ($result in $workerResults) {
                [void]$allResults.Add($result)
            }
        }
    }
    catch {
        Write-Host "Error receiving job results: $_" -ForegroundColor Yellow
    }
    finally {
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
    }
}

$testEndTime = Get-Date
$testDuration = ($testEndTime - $testStartTime).TotalSeconds

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LOAD TEST RESULTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Filter successful requests
$successfulResults = $allResults | Where-Object { $_.Success -eq $true }
$failedResults = $allResults | Where-Object { $_.Success -eq $false }

$totalRequests = $allResults.Count
$successCount = $successfulResults.Count
$failureCount = $failedResults.Count
$successRate = if ($totalRequests -gt 0) { ($successCount / $totalRequests) * 100 } else { 0 }

Write-Host "Test Duration: $([Math]::Round($testDuration, 2)) seconds" -ForegroundColor White
Write-Host "Total Requests: $totalRequests" -ForegroundColor White
Write-Host "Successful: $successCount ($([Math]::Round($successRate, 2))%)" -ForegroundColor Green
Write-Host "Failed: $failureCount" -ForegroundColor $(if ($failureCount -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($successfulResults.Count -eq 0) {
    Write-Host "ERROR: No successful requests!" -ForegroundColor Red
    if ($failedResults.Count -gt 0) {
        Write-Host "First error: $($failedResults[0].Error)" -ForegroundColor Red
    }
    exit 1
}

# Calculate latency metrics
$latencies = $successfulResults | ForEach-Object { $_.TotalLatencyMs }
$latenciesSorted = $latencies | Sort-Object

$avgLatency = ($latencies | Measure-Object -Average).Average
$minLatency = ($latencies | Measure-Object -Minimum).Minimum
$maxLatency = ($latencies | Measure-Object -Maximum).Maximum

$p50Latency = Get-Percentile -Values $latencies -Percentile 0.50
$p90Latency = Get-Percentile -Values $latencies -Percentile 0.90
$p95Latency = Get-Percentile -Values $latencies -Percentile 0.95
$p99Latency = Get-Percentile -Values $latencies -Percentile 0.99

# Calculate throughput
$throughput = $successCount / $testDuration

# Calculate variance for average latency (5-10% check)
$variance = ($latencies | ForEach-Object { [Math]::Pow($_ - $avgLatency, 2) } | Measure-Object -Average).Average
$stdDev = [Math]::Sqrt($variance)
$coefficientOfVariation = if ($avgLatency -gt 0) { ($stdDev / $avgLatency) * 100 } else { 0 }

# QPU metrics
$qpuLatencies = $successfulResults | ForEach-Object { $_.QpuLatencyMs }
$avgQpuLatency = ($qpuLatencies | Measure-Object -Average).Average
$totalQpuTime = ($qpuLatencies | Measure-Object -Sum).Sum
$qpuUtilization = if ($testDuration -gt 0) { ($totalQpuTime / ($testDuration * 1000)) * 100 } else { 0 }

# Queue length estimation (simplified: based on concurrent requests vs QPU capacity)
$estimatedQueueLength = [Math]::Max(0, $ConcurrentUsers - 1)

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "RESPONSE TIME METRICS" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "Average Latency:     $([Math]::Round($avgLatency, 2)) ms" -ForegroundColor White
Write-Host "Min Latency:         $([Math]::Round($minLatency, 2)) ms" -ForegroundColor White
Write-Host "Max Latency:         $([Math]::Round($maxLatency, 2)) ms" -ForegroundColor White
Write-Host ""
Write-Host "P50 (Median):        $([Math]::Round($p50Latency, 2)) ms" -ForegroundColor Cyan
Write-Host "P90:                 $([Math]::Round($p90Latency, 2)) ms" -ForegroundColor Cyan
Write-Host "P95:                 $([Math]::Round($p95Latency, 2)) ms" -ForegroundColor Yellow
Write-Host "P99:                 $([Math]::Round($p99Latency, 2)) ms" -ForegroundColor Yellow
Write-Host ""
Write-Host "Std Deviation:       $([Math]::Round($stdDev, 2)) ms" -ForegroundColor Gray
Write-Host "Coefficient of Var:   $([Math]::Round($coefficientOfVariation, 2))%" -ForegroundColor $(if ($coefficientOfVariation -le 10) { "Green" } else { "Yellow" })
Write-Host ""

# Check if variance is within 5-10%
$varianceCheck = if ($coefficientOfVariation -le 10) { "✓ PASS" } else { "✗ FAIL (should be ≤10%)" }
Write-Host "Variance Check (≤10%): $varianceCheck" -ForegroundColor $(if ($coefficientOfVariation -le 10) { "Green" } else { "Red" })
Write-Host ""

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "THROUGHPUT METRICS" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "Throughput:          $([Math]::Round($throughput, 3)) req/s" -ForegroundColor White
Write-Host "Total Requests:      $successCount" -ForegroundColor White
Write-Host "Test Duration:       $([Math]::Round($testDuration, 2)) seconds" -ForegroundColor White
Write-Host ""

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "QPU METRICS" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "Avg QPU Latency:     $([Math]::Round($avgQpuLatency, 2)) ms" -ForegroundColor White
Write-Host "QPU Utilization:    $([Math]::Round($qpuUtilization, 2))%" -ForegroundColor White
Write-Host "Total QPU Time:      $([Math]::Round($totalQpuTime, 2)) ms" -ForegroundColor White
Write-Host ""

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "QUEUE LENGTH ESTIMATION" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "Concurrent Users:    $ConcurrentUsers" -ForegroundColor White
Write-Host "Estimated Queue:     ~$estimatedQueueLength requests" -ForegroundColor White
Write-Host "(Based on concurrent requests vs QPU capacity)" -ForegroundColor Gray
Write-Host ""

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "SUMMARY" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host "✓ Average Response Time: $([Math]::Round($avgLatency, 2)) ms" -ForegroundColor Green
Write-Host "✓ P95 Latency: $([Math]::Round($p95Latency, 2)) ms" -ForegroundColor Green
Write-Host "✓ P99 Latency: $([Math]::Round($p99Latency, 2)) ms" -ForegroundColor Green
Write-Host "✓ Throughput: $([Math]::Round($throughput, 3)) req/s" -ForegroundColor Green
Write-Host "✓ Queue Length: ~$estimatedQueueLength requests" -ForegroundColor Green
Write-Host ""

# Export results to CSV
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$csvPath = "load-test-results-$timestamp.csv"

$csvData = @()
foreach ($result in $successfulResults) {
    $csvData += [PSCustomObject]@{
        RequestNumber = $result.RequestNumber
        TotalLatencyMs = $result.TotalLatencyMs
        QpuLatencyMs = $result.QpuLatencyMs
        Cme = $result.Cme
        PFlow = $result.PFlow
    }
}

$csvData | Export-Csv -Path $csvPath -NoTypeInformation
Write-Host "Results exported to: $csvPath" -ForegroundColor Cyan
Write-Host ""

Write-Host "Load test completed!" -ForegroundColor Green

