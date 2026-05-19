# CME Simulation API

ASP.NET Core Web API backend for quantum machine learning simulation system.

## Architecture

This API serves as the main backend for the imitation model, handling:
- Online inference (CME computation via quantum backend)
- Training job management (background metaheuristic optimization)
- Dashboard metrics and aggregated statistics
- Database persistence (SQL Server via EF Core)

## Database Setup

### Local SQL Server (Docker)

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

### Run Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Or migrations run automatically on application startup.

## Configuration

Edit `appsettings.json`:

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

## Running

```bash
dotnet restore
dotnet build
dotnet run
```

API will be available at `http://localhost:5000` (or check console output).

Swagger UI: `http://localhost:5000/swagger`

## Endpoints

### Inference

**POST /api/inference/cme**
```json
{
  "sessionId": "11111111-1111-1111-1111-111111111111",
  "windowId": "window_001",
  "features": [0.5, -0.3, 0.8, 0.1, -0.2, 0.6, 0.0, -0.4],
  "taskDifficulty": 0.7
}
```

Response:
```json
{
  "cme": 45.23,
  "pFlow": 0.623,
  "shotsUsed": 1024,
  "depth": 8,
  "qpuLatencyMs": 1456,
  "totalLatencyMs": 1523
}
```

### Training

**POST /api/training/start**
```json
{
  "totalGenerations": 10
}
```

**GET /api/training/{jobId}**

Returns training job status.

### Dashboard

**GET /api/dashboard/summary**

Returns aggregated metrics:
- Total inference requests
- Average CME value
- Response time percentiles (avg, p95, p99)
- Training jobs by status
- Total sessions

## Database Schema

See Models directory for full entity definitions:
- `Session`: EEG recording session
- `InferenceRequestLog`: Performance metrics for each request
- `CmeWindowResult`: Computed CME values
- `TrainingJob`: Long-running optimization jobs

## Background Services

**TrainingWorkerService**
- Polls for queued training jobs
- Runs metaheuristic optimization loop
- Calls quantum backend for each candidate model
- Updates job status and metrics

## Testing

```bash
# Using curl
curl -X POST http://localhost:5000/api/inference/cme \
  -H "Content-Type: application/json" \
  -d '{"sessionId":"11111111-1111-1111-1111-111111111111","windowId":"w1","features":[0.5,-0.3,0.8,0.1],"taskDifficulty":0.5}'

# Using .NET HTTP REPL
dotnet tool install -g Microsoft.dotnet-httprepl
httprepl http://localhost:5000
```

## Development

```bash
# Watch mode (auto-reload)
dotnet watch run

# Add migration
dotnet ef migrations add MigrationName

# Reset database
dotnet ef database drop
dotnet ef database update
```


