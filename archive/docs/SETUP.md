# Setup Guide

Complete setup instructions for the CME Simulation System.

## Prerequisites

- **Docker & Docker Compose**: Latest version
- **Node.js**: 20+ (for simulation client)
- **.NET SDK**: 8.0 (if running API without Docker)
- **Python**: 3.11+ (if running quantum backend without Docker)

## Quick Start (Recommended)

### 1. Start Services with Docker

```bash
# Clone/navigate to project directory
cd lab45

# Start all services
docker-compose up -d

# Check service health
docker-compose ps
```

Services will be available at:
- **API**: http://localhost:5000
- **Quantum Backend**: http://localhost:8001
- **SQL Server**: localhost:1433

### 2. Wait for Initialization

The API will automatically run database migrations on startup. Wait ~30 seconds for all services to be healthy:

```bash
# Watch logs
docker-compose logs -f api

# When you see "Database migrations complete", services are ready
```

### 3. Run Simulation Client

```bash
cd cme-sim-client
npm install
npm run build

# Run a basic simulation
npm run simulate -- --duration 60 --onlineRate 1 --trainRate 0.1
```

### 4. Stop Services

```bash
docker-compose down

# Remove volumes (clean database)
docker-compose down -v
```

## Manual Setup (Development)

### 1. SQL Server

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver -d \
  mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Python Quantum Backend

```bash
cd qbackend

# Create virtual environment
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Copy environment file
cp .env.example .env

# Run service
uvicorn app.main:app --host 0.0.0.0 --port 8001
```

Verify: http://localhost:8001/health

### 3. ASP.NET Core API

```bash
cd CmeSim.Api

# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Run migrations
dotnet ef database update

# Start API
dotnet run
```

Verify: http://localhost:5000/api/dashboard/summary

### 4. TypeScript Simulation Client

```bash
cd cme-sim-client

npm install
npm run build

# Test run
npm run simulate -- --duration 30 --onlineRate 0.5
```

## Configuration

### Environment Variables

**Quantum Backend** (`qbackend/.env`):
```env
QPU_LATENCY_MIN_MS=300
QPU_LATENCY_MAX_MS=2000
DEFAULT_SHOTS=1024
```

**API** (`CmeSim.Api/appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=CmeSimDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  },
  "QuantumBackend": {
    "BaseUrl": "http://localhost:8001",
    "TimeoutSeconds": 30
  },
  "TrainingWorker": {
    "PollingIntervalSeconds": 5,
    "GenerationsPerJob": 10,
    "CandidatesPerGeneration": 5,
    "MaxConcurrentJobs": 2
  }
}
```

## Testing

### 1. Health Checks

```bash
# Quantum backend
curl http://localhost:8001/health

# API
curl http://localhost:5000/api/dashboard/summary
```

### 2. Manual Requests

Use the `requests.http` file with VS Code REST Client extension, or:

```bash
# Online inference
curl -X POST http://localhost:5000/api/inference/cme \
  -H "Content-Type: application/json" \
  -d '{
    "sessionId": "11111111-1111-1111-1111-111111111111",
    "windowId": "test_001",
    "features": [0.5, -0.3, 0.8, 0.1, -0.2, 0.6, 0.0, -0.4],
    "taskDifficulty": 0.7
  }'

# Start training job
curl -X POST http://localhost:5000/api/training/start \
  -H "Content-Type: application/json" \
  -d '{"totalGenerations": 5}'
```

### 3. Run Simulation

```bash
cd cme-sim-client

# Low load
npm run simulate -- --duration 60 --onlineRate 1

# High load
npm run simulate -- --duration 120 --onlineRate 5 --clients 3 --trainRate 0.2
```

## Troubleshooting

### SQL Server Connection Issues

```bash
# Check SQL Server is running
docker ps | grep sqlserver

# Test connection
docker exec -it cme-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' -Q "SELECT 1"
```

### API Can't Connect to Quantum Backend

```bash
# Check quantum backend health
curl http://localhost:8001/health

# Check Docker network (if using Docker)
docker network inspect lab45_cme-network
```

### Database Migration Errors

```bash
cd CmeSim.Api

# Drop and recreate database
dotnet ef database drop --force
dotnet ef database update
```

### Quantum Backend Import Errors

```bash
cd qbackend
source venv/bin/activate

# Reinstall dependencies
pip install --upgrade pip
pip install -r requirements.txt
```

## Performance Tuning

### Adjust QPU Latency

Edit `qbackend/.env`:
```env
QPU_LATENCY_MIN_MS=100   # Faster
QPU_LATENCY_MAX_MS=500
```

Restart quantum backend.

### Increase Training Worker Capacity

Edit `CmeSim.Api/appsettings.json`:
```json
{
  "TrainingWorker": {
    "MaxConcurrentJobs": 5,  // More parallel jobs
    "GenerationsPerJob": 20   // Longer jobs
  }
}
```

Restart API.

### Scale Simulation Load

```bash
npm run simulate -- \
  --duration 300 \
  --onlineRate 10 \    # 10 req/s
  --clients 10 \       # 10 parallel sessions
  --trainRate 0.5      # 1 job every 2 minutes
```

## Database Access

### Using Azure Data Studio / SQL Server Management Studio

- **Server**: localhost,1433
- **Username**: sa
- **Password**: YourStrong@Passw0rd

### Using Docker

```bash
docker exec -it cme-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd'

# Query example
1> USE CmeSimDb;
2> SELECT COUNT(*) FROM InferenceRequestLogs;
3> GO
```

## Next Steps

1. **Baseline Test**: Run low-load simulation to establish baseline metrics
2. **Load Testing**: Gradually increase load to find system limits
3. **Training Analysis**: Submit multiple training jobs and observe QPU contention
4. **Configuration Experiments**: Vary QPU latency, training parameters, etc.
5. **Data Analysis**: Export metrics from database for detailed analysis

See README.md for architecture details and performance analysis guidance.


