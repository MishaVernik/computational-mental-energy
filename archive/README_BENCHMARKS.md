# Architecture Benchmarking Guide

## Quick Start

### Prerequisites
1. Ensure the API is running (`dotnet run` in `CmeSim.Api` directory)
2. Ensure the quantum backend is running (`python -m uvicorn app.main:app` in `qbackend` directory)
3. Database should be accessible (SQL Server)

### Running Benchmarks

#### Option 1: Via Dashboard (Recommended)
1. Start the dashboard: `cd cme-dashboard && npm run dev`
2. Navigate to http://localhost:3000
3. Click on "Architectures Bench" tab
4. Select scenarios and click "Run Selected" or "Run All"
5. View results in the results table
6. Export results as CSV/JSON

#### Option 2: Via API (PowerShell Script)
```powershell
.\test-benchmarks.ps1
```

This script will:
- Run 3 quick benchmarks (one per architecture)
- Wait for completion
- Display results
- Show benchmark history

#### Option 3: Via API (Manual)
```powershell
# Start a benchmark
$config = @{
    name = "Test Benchmark"
    architecture = "A_Monolith"
    activeClients = 5
    requestsPerClient = 20
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

$runId = Invoke-RestMethod -Uri "http://localhost:5000/api/benchmarks/run" -Method Post -Body $config -ContentType "application/json"

# Check status
Invoke-RestMethod -Uri "http://localhost:5000/api/benchmarks/$runId" -Method Get

# Export results
Invoke-WebRequest -Uri "http://localhost:5000/api/benchmarks/$runId/export?format=json" -OutFile "results.json"
```

## Architecture Comparison

### Architecture A: Monolith
- **Description**: All operations execute synchronously in the API
- **Best for**: Low latency, simple deployments
- **Limitations**: Limited scalability under high load

### Architecture B: Synchronous Microservices
- **Description**: API calls PreprocessService via HTTP, then QPU, then DB
- **Best for**: Better separation of concerns
- **Limitations**: Synchronous coupling, network overhead

### Architecture C: Brokered (Async)
- **Description**: API enqueues to broker, worker nodes process asynchronously
- **Best for**: High throughput, scalability
- **Limitations**: More complex, requires result polling

## Benchmark Scenarios

### Minimal Set (`benchmarks/scenarios/minimal-set.json`)
Quick tests with low/medium load:
- 6 scenarios covering all architectures
- Low load: 5 clients × 20 requests
- Medium load: 10 clients × 30 requests

### Extended Set (`benchmarks/scenarios/extended-set.json`)
Comprehensive tests:
- 8 scenarios with varying parameters
- High load: 20 clients × 50 requests
- Different worker counts: 1, 4, 16
- Multiple QPU backends: 1, 2
- Different shots: 256, 1024

## Metrics Collected

Each benchmark run collects:
- **Overall Metrics**: avg/p95/p99 latency, throughput, failure rate
- **Queue Metrics**: average and max QPU/broker queue lengths
- **Stage Metrics**: Timing for validate, preprocess, QPU wait/service, DB write, response
- **Raw Events**: Individual timing events for detailed analysis

## Exporting Results

### JSON Export
```powershell
Invoke-WebRequest -Uri "http://localhost:5000/api/benchmarks/{runId}/export?format=json" -OutFile "results.json"
```

### CSV Export
```powershell
Invoke-WebRequest -Uri "http://localhost:5000/api/benchmarks/{runId}/export?format=csv" -OutFile "results.csv"
```

## Petri Net Parameters

Get Petri net model parameters from a benchmark run:
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/benchmarks/{runId}/petri-params" -Method Get
```

Returns:
- Token counts (workers, QPU count, concurrency gates)
- Transition delays (mean/std for each stage)
- Queue statistics (avg/max length, utilization)

## Troubleshooting

### Benchmark Stuck in "Running" Status
- Check API logs for errors
- Ensure quantum backend is accessible
- Verify database connection

### Low Throughput
- Check QPU backend latency settings
- Increase `maxConcurrentQpuCalls` for higher concurrency
- Increase `workersCount` for brokered architecture

### High Failure Rate
- Check quantum backend health
- Verify network connectivity
- Check database connection pool limits

## Performance Tips

1. **Start Small**: Begin with low load scenarios to verify setup
2. **Monitor Resources**: Watch CPU, memory, and database during runs
3. **Use Seeds**: Set `seed` parameter for reproducible results
4. **Compare Architectures**: Run same load on all 3 architectures for fair comparison
5. **Export Results**: Save results for later analysis and Petri net parameterization


