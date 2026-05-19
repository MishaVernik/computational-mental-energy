# CME Simulation Client

TypeScript/Node.js CLI application for load testing the CME quantum ML system.

## Features

- **Online inference traffic**: Generates HTTP requests at configurable rate (req/sec)
- **Training job submissions**: Starts long-running optimization jobs (jobs/min)
- **Parallel clients**: Simulates multiple concurrent users/sessions
- **Performance metrics**: Measures latency distribution (avg, p95, p99), throughput, failures

## Installation

```bash
npm install
npm run build
```

## Usage

```bash
npm run simulate -- [options]
```

### Options

- `-d, --duration <seconds>`: Simulation duration (default: 60)
- `-o, --onlineRate <rate>`: Online inference requests per second (default: 1)
- `-t, --trainRate <rate>`: Training jobs per minute (default: 0.1)
- `-c, --clients <count>`: Number of parallel client sessions (default: 1)
- `-u, --url <url>`: API base URL (default: http://localhost:5000)

### Examples

**Basic simulation (60 seconds, 1 req/s)**:
```bash
npm run simulate
```

**High load (2 minutes, 5 req/s, 5 parallel clients)**:
```bash
npm run simulate -- --duration 120 --onlineRate 5 --clients 5
```

**With training jobs (0.2 jobs/minute = 1 job every 5 minutes)**:
```bash
npm run simulate -- --duration 300 --onlineRate 2 --trainRate 0.2
```

**Custom API URL**:
```bash
npm run simulate -- --url http://api.example.com:8080
```

## Output

The simulator prints progress updates every 5 seconds:

```
=== CME Simulation Started ===
Duration: 60s | Online Rate: 2.0 req/s | Training Rate: 0.1 jobs/min | Clients: 3

[   5s] Online: 30 (29 ok) | Training: 0 | Avg latency: 1234ms
[  10s] Online: 60 (58 ok) | Training: 1 | Avg latency: 1189ms
...
```

Final summary:

```
=== Simulation Complete ===

Online Inference Metrics:
  Total requests:    120
  Successful:        118
  Failed:            2
  Avg latency:       1205 ms
  P95 latency:       2340 ms
  P99 latency:       3120 ms
  Throughput:        1.97 req/s

Training Job Metrics:
  Total submitted:   1
  Completed:         1
  Running:           0
  Failed:            0
  Avg completion:    45.3 s

Total duration: 60 seconds
```

## Architecture

```
index.ts (CLI)
    ↓
simulator.ts (LoadSimulator)
    ↓
api-client.ts (CmeApiClient)
    ↓
HTTP → ASP.NET Core API
```

### Load Generation

- **Online requests**: Each client runs an async loop with fixed interval between requests (approximate Poisson process)
- **Training jobs**: Separate async loop submits jobs at configured rate
- **Metrics collection**: All requests are timed and logged for analysis

## Development

```bash
# Build
npm run build

# Run directly (after build)
node dist/index.js --duration 30 --onlineRate 2
```

## Use Cases

1. **Baseline performance**: Run with low load to establish baseline latency
2. **Load testing**: Increase `--onlineRate` and `--clients` to find system limits
3. **Queue analysis**: Submit multiple training jobs and observe impact on online latency
4. **Configuration comparison**: Test different QPU latency settings in Python service

## Metrics Interpretation

- **Avg latency**: Mean response time (affected by QPU latency ~300-2000ms)
- **P95/P99 latency**: Tail latencies (important for user experience)
- **Throughput**: Actual req/s achieved (may be less than requested if system is overloaded)
- **Failed requests**: HTTP errors or timeouts (should be 0 in healthy system)


