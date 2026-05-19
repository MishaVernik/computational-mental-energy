# Project Structure

Complete overview of the CME Simulation System codebase.

## Directory Layout

```
lab45/
│
├── README.md                    # Main documentation with architecture
├── QUICKSTART.md                # Get started in 5 minutes
├── SETUP.md                     # Detailed setup instructions
├── docker-compose.yml           # Orchestrates all services
├── requests.http                # Example HTTP requests (REST Client)
├── .gitignore                   # Git ignore rules
├── .dockerignore                # Docker ignore rules
│
├── qbackend/                    # Python Quantum Backend (FastAPI + Qiskit)
│   ├── README.md                # Component documentation
│   ├── Dockerfile               # Container image definition
│   ├── requirements.txt         # Python dependencies
│   ├── .env.example             # Configuration template
│   └── app/
│       ├── __init__.py          # Package init
│       ├── main.py              # FastAPI application
│       ├── config.py            # Configuration management
│       ├── models.py            # Pydantic request/response models
│       └── qml.py               # Quantum circuit logic (Qiskit)
│
├── CmeSim.Api/                  # ASP.NET Core Web API (.NET 8)
│   ├── README.md                # Component documentation
│   ├── Dockerfile               # Container image definition
│   ├── CmeSim.Api.csproj        # .NET project file
│   ├── appsettings.json         # Configuration (DB, QPU URL, etc.)
│   ├── Program.cs               # Application entry point
│   │
│   ├── Models/                  # Entity Framework Core models
│   │   ├── Session.cs           # EEG session entity
│   │   ├── InferenceRequestLog.cs   # Request metrics
│   │   ├── CmeWindowResult.cs   # CME computation results
│   │   └── TrainingJob.cs       # Training job entity
│   │
│   ├── Data/
│   │   └── CmeSimDbContext.cs   # EF Core database context
│   │
│   ├── DTOs/                    # Data Transfer Objects
│   │   ├── InferenceRequestDto.cs   # Online inference API contracts
│   │   ├── TrainingJobDto.cs    # Training job API contracts
│   │   └── DashboardDto.cs      # Dashboard metrics
│   │
│   ├── Services/                # Business logic & external clients
│   │   ├── IQuantumBackendClient.cs      # Quantum backend interface
│   │   ├── QuantumBackendHttpClient.cs   # HTTP client implementation
│   │   ├── ICmeCalculator.cs    # CME computation interface
│   │   └── TrainingWorkerService.cs      # Background worker for training jobs
│   │
│   └── Controllers/             # HTTP API endpoints
│       ├── InferenceController.cs   # POST /api/inference/cme
│       ├── TrainingController.cs    # /api/training/* endpoints
│       └── DashboardController.cs   # GET /api/dashboard/summary
│
└── cme-sim-client/              # TypeScript Simulation Client (Node.js)
    ├── README.md                # Component documentation
    ├── package.json             # npm dependencies & scripts
    ├── tsconfig.json            # TypeScript configuration
    └── src/
        ├── index.ts             # CLI entry point (commander)
        ├── types.ts             # TypeScript type definitions
        ├── api-client.ts        # HTTP client (axios) for API
        └── simulator.ts         # Load generation logic
```

## Component Responsibilities

### 1. Python Quantum Backend (`qbackend/`)

**Purpose**: Simulates a quantum computing backend (IBM Quantum / IBMQ)

**Key Files**:
- `main.py`: FastAPI app with `/qpu/infer` endpoint
- `qml.py`: Quantum circuit construction and execution (Qiskit Aer)
- `config.py`: QPU latency configuration (simulates realistic delays)

**Endpoints**:
- `POST /qpu/infer`: Execute quantum circuit, return p_flow
- `GET /health`: Health check
- `GET /stats`: Monitoring (placeholder)

**Technology**: Python 3.11, FastAPI, Qiskit, Qiskit Aer

### 2. ASP.NET Core Web API (`CmeSim.Api/`)

**Purpose**: Main backend for quantum ML system

**Key Components**:

#### Controllers (API Endpoints)
- `InferenceController`: Online CME computation
  - `POST /api/inference/cme`: Main inference endpoint
- `TrainingController`: Training job management
  - `POST /api/training/start`: Submit new job
  - `GET /api/training/{id}`: Get job status
  - `GET /api/training`: List jobs
- `DashboardController`: System metrics
  - `GET /api/dashboard/summary`: Aggregated stats

#### Services
- `QuantumBackendHttpClient`: Calls Python quantum backend
- `CmeCalculator`: Computes CME from features + p_flow
- `TrainingWorkerService`: Background worker that processes training queue

#### Data Layer
- `CmeSimDbContext`: EF Core context
- Models: `Session`, `InferenceRequestLog`, `CmeWindowResult`, `TrainingJob`

**Technology**: .NET 8, ASP.NET Core, Entity Framework Core, SQL Server

### 3. TypeScript Simulation Client (`cme-sim-client/`)

**Purpose**: Load generator and performance measurement

**Key Components**:
- `index.ts`: CLI with commander (argument parsing)
- `simulator.ts`: Load generation logic
  - Online inference loop (Poisson-like arrivals)
  - Training job submission loop
  - Progress reporting
  - Metrics aggregation
- `api-client.ts`: HTTP client (axios) for API calls

**CLI Parameters**:
- `--duration`: Simulation time (seconds)
- `--onlineRate`: Online requests per second
- `--trainRate`: Training jobs per minute
- `--clients`: Number of parallel sessions
- `--url`: API base URL

**Technology**: TypeScript, Node.js 20+, axios, commander

### 4. Infrastructure

#### Docker Compose (`docker-compose.yml`)
Orchestrates three services:
- `sqlserver`: SQL Server 2022 (port 1433)
- `qbackend`: Python quantum service (port 8001)
- `api`: ASP.NET Core API (port 5000)

#### Database (SQL Server)
Schema:
- `Sessions`: EEG recording sessions
- `InferenceRequestLogs`: Performance metrics per request
- `CmeWindowResults`: CME computation results
- `TrainingJobs`: Long-running optimization jobs

## Request Flow Diagrams

### Online Inference Flow

```
Client
  │
  │ POST /api/inference/cme
  │ {sessionId, windowId, features, taskDifficulty}
  ▼
InferenceController
  │
  ├─► QuantumBackendHttpClient
  │     │
  │     │ POST /qpu/infer {features}
  │     ▼
  │   Python Quantum Backend
  │     │ - Build circuit with angle encoding
  │     │ - Execute on Qiskit Aer
  │     │ - Simulate QPU delay (300-2000ms)
  │     │ - Return p_flow
  │     ▼
  │   {pFlow, shotsUsed, depth, qpuLatencyMs}
  │
  ├─► CmeCalculator
  │     │ - Compute energy from features
  │     │ - Apply scaling function g(difficulty, p_flow)
  │     │ - Return CME value
  │     ▼
  │   {cme}
  │
  ├─► SQL Server
  │     │ - Insert InferenceRequestLog
  │     │ - Insert CmeWindowResult
  │     ▼
  │   Persisted
  │
  ▼
Response to Client
{cme, pFlow, shotsUsed, depth, qpuLatencyMs, totalLatencyMs}
```

### Training Job Flow

```
Client
  │
  │ POST /api/training/start
  ▼
TrainingController
  │ - Create TrainingJob (status=Queued)
  │ - Insert into SQL Server
  ▼
Response {jobId}

[Background Process]

TrainingWorkerService (IHostedService)
  │ Polls every 5 seconds
  │
  ├─► Find jobs with status=Queued
  │
  ├─► Mark job as Running
  │
  ├─► For each generation (e.g., 10 generations):
  │     │
  │     ├─► For each candidate (e.g., 5 candidates):
  │     │     │
  │     │     ├─► Generate random features
  │     │     │
  │     │     ├─► Call QuantumBackendClient.InferAsync()
  │     │     │     │ → Python Quantum Backend
  │     │     │     │ → Return p_flow
  │     │     │
  │     │     ├─► Compute synthetic fitness
  │     │     │
  │     │     └─► Track best fitness
  │     │
  │     ├─► Sleep (simulate CPU work)
  │     │
  │     └─► Update job.CompletedGenerations in DB
  │
  └─► Mark job as Completed
      │ - Set job.BestFitness
      │ - Set job.TotalQpuCalls
      │ - Update SQL Server
      ▼
    Done
```

## Configuration Files

### `qbackend/.env`
```env
QPU_LATENCY_MIN_MS=300      # Min simulated QPU delay
QPU_LATENCY_MAX_MS=2000     # Max simulated QPU delay
DEFAULT_SHOTS=1024          # Quantum circuit shots
```

### `CmeSim.Api/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=CmeSimDb;..."
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

## Key Design Patterns

### Dependency Injection (ASP.NET Core)
- `IQuantumBackendClient` → `QuantumBackendHttpClient`
- `ICmeCalculator` → `CmeCalculator`
- `CmeSimDbContext` injected into controllers

### Background Services
- `TrainingWorkerService` implements `BackgroundService`
- Runs continuously, polls database for queued jobs

### Async/Await Everywhere
- All HTTP calls are async
- All database operations are async
- TypeScript client uses async/await

### Repository Pattern (implicit via EF Core)
- DbContext provides abstraction over SQL Server
- Controllers use DbContext directly (simple project, no separate repository layer)

## Testing Strategy

### Manual Testing
- Use `requests.http` with REST Client extension
- Direct curl commands

### Load Testing
- Use `cme-sim-client` with varying parameters
- Measure latency, throughput, queue behavior

### Example Scenarios

**Baseline**:
```bash
npm run simulate -- --duration 60 --onlineRate 1
```

**Load Test**:
```bash
npm run simulate -- --duration 300 --onlineRate 10 --clients 5
```

**Queue Contention**:
```bash
# Submit multiple training jobs, observe impact on online latency
npm run simulate -- --duration 180 --onlineRate 2 --trainRate 0.5
```

## Extension Points

### Add Real Quantum Hardware
- Set `IBMQ_TOKEN` in `qbackend/.env`
- Modify `qml.py` to use IBM Runtime instead of Aer simulator

### Add More Metrics
- Extend `DashboardController` with additional queries
- Add time-series endpoints for latency over time

### Implement Real CME Formula
- Update `CmeCalculator.ComputeCme()` with validated formula from research

### Add Authentication
- Implement JWT auth in ASP.NET Core
- Require auth tokens in TypeScript client

### Scale Out
- Add Redis for distributed caching
- Use message queue (RabbitMQ) for training jobs instead of DB polling
- Deploy multiple API instances behind load balancer

## Performance Considerations

### Bottlenecks (by design)
1. **Quantum Backend**: 300-2000ms latency per request (simulates real QPU)
2. **Training Jobs**: Sustained QPU load (blocks online requests)
3. **Database**: All metrics persisted (adds ~5-10ms per request)

### Optimization Opportunities
1. **Caching**: Cache session lookups
2. **Batch Inserts**: Group DB writes
3. **Connection Pooling**: Configure SQL Server connection pool
4. **Parallel QPU Requests**: Add semaphore to limit concurrent calls

## Documentation Map

- **README.md**: Architecture, motivation, queueing model
- **QUICKSTART.md**: Get running in 5 minutes
- **SETUP.md**: Detailed setup for all deployment modes
- **PROJECT_STRUCTURE.md**: This file (codebase overview)
- **requests.http**: Example API calls
- Component READMEs in each directory

## Summary

This is a complete, runnable imitation model of a quantum machine learning web application. All three components work together to simulate realistic performance characteristics of a system that:
- Processes online inference requests (low latency requirement)
- Runs long-running training jobs (high QPU utilization)
- Measures and stores performance metrics
- Provides dashboard for analysis

The system is designed for **performance analysis and queueing theory experiments**, not production use.


