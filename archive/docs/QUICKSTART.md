# Quick Start Guide

Get the CME Simulation System running in 5 minutes.

## TL;DR

```bash
# 1. Start backend services
docker-compose up -d

# 2. Wait 30 seconds for initialization
sleep 30

# 3. Run simulation client
cd cme-sim-client
npm install && npm run build
npm run simulate -- --duration 60 --onlineRate 2

# Done! Check the metrics output.
```

## Step-by-Step

### 1. Start Docker Services

From the project root:

```bash
docker-compose up -d
```

This starts:
- SQL Server (port 1433)
- Python Quantum Backend (port 8001)
- ASP.NET Core API (port 5000)
- React Dashboard (port 3000)

### 2. Verify Services Are Healthy

```bash
# Check status
docker-compose ps

# All services should show "healthy" after ~30 seconds
# Watch logs if needed:
docker-compose logs -f
```

### 3. Open the Web Dashboard

Open your browser and navigate to:
```
http://localhost:3000
```

You'll see:
- ✅ **System Overview**: Real-time metrics and status
- ✅ **Online Inference Panel**: Submit EEG data for CME computation
- ✅ **Training Jobs Panel**: Start and monitor training jobs
- ✅ **Performance Charts**: Visualize latency and throughput
- ✅ **Recent Activity**: Live training job progress

### 4. Test API Manually (Optional)

```bash
# Health check
curl http://localhost:5000/api/dashboard/summary

# Single inference request
curl -X POST http://localhost:5000/api/inference/cme \
  -H "Content-Type: application/json" \
  -d '{
    "sessionId": "11111111-1111-1111-1111-111111111111",
    "windowId": "test_001",
    "features": [0.5, -0.3, 0.8, 0.1, -0.2, 0.6, 0.0, -0.4],
    "taskDifficulty": 0.7
  }'
```

### 5. Run Load Simulation (Optional)

```bash
cd cme-sim-client
npm install
npm run build

# Basic simulation: 60 seconds, 2 req/s
npm run simulate -- --duration 60 --onlineRate 2
```

Expected output:
```
=== CME Simulation Started ===
Duration: 60s | Online Rate: 2.0 req/s | Training Rate: 0.1 jobs/min | Clients: 1

[   5s] Online: 10 (10 ok) | Training: 0 | Avg latency: 1234ms
[  10s] Online: 20 (20 ok) | Training: 0 | Avg latency: 1189ms
...

=== Simulation Complete ===

Online Inference Metrics:
  Total requests:    120
  Successful:        120
  Failed:            0
  Avg latency:       1205 ms
  P95 latency:       2340 ms
  P99 latency:       3120 ms
  Throughput:        2.00 req/s

Training Job Metrics:
  Total submitted:   0
  ...
```

### 5. Experiment

**Higher load:**
```bash
npm run simulate -- --duration 120 --onlineRate 5 --clients 3
```

**With training jobs:**
```bash
npm run simulate -- --duration 180 --onlineRate 2 --trainRate 0.2
```

**Custom API URL:**
```bash
npm run simulate -- --url http://your-api:5000 --duration 60
```

## Common Issues

### "Connection refused" on API

Services may still be starting. Wait 30 seconds and try again.

```bash
# Check API logs
docker-compose logs api
```

### "Database not found"

Migrations should run automatically. If not:

```bash
docker-compose restart api
docker-compose logs -f api
# Wait for "Database migrations complete"
```

### High latency (>5 seconds)

This is expected! The quantum backend simulates realistic QPU delays (300-2000ms). This is intentional for the imitation model.

To reduce latency, edit `qbackend/.env`:
```env
QPU_LATENCY_MIN_MS=100
QPU_LATENCY_MAX_MS=500
```

Then:
```bash
docker-compose restart qbackend
```

## What's Next?

- **View Swagger UI**: http://localhost:5000/swagger
- **Test with REST Client**: Open `requests.http` in VS Code
- **Analyze Results**: Query database or use dashboard endpoint
- **Read Documentation**: See README.md and SETUP.md

## Cleanup

```bash
# Stop services
docker-compose down

# Remove volumes (database data)
docker-compose down -v
```

## Architecture Reminder

```
TypeScript Client (Node)
    ↓ HTTP
ASP.NET Core API (C#)
    ↓ HTTP              ↓ SQL
Python Qiskit Service   SQL Server
```

All three components are needed for a complete simulation run.

## Support

For detailed setup, configuration, and troubleshooting, see:
- `SETUP.md` - Complete setup guide
- `README.md` - Architecture and analysis
- Component READMEs in each directory

