# Lab45 - PhD Research Project: Performance Analysis of Parallel and Quantum Computing Systems

## Overview

This repository contains a comprehensive research project for PhD dissertation work on performance analysis and modeling of parallel computing systems and quantum machine learning applications. The project consists of multiple interconnected components designed to support experimental research, performance benchmarking, and formal modeling using Petri nets.

## Project Components

This repository includes four main research components:

### 1. CME Simulation System (Main Project)
**Quantum Machine Learning Web Application Imitation Model**

An imitation model of a web application for quantum machine learning over EEG data, built for PhD research on performance analysis of quantum-enhanced mental state detection systems.

**Key Features:**
- **Online inference**: EEG feature classification via quantum circuits → CME (Countable Mental Energy) computation
- **Training jobs**: Long-running metaheuristic optimization (GA, PSO, ACO, Simulated Annealing) that repeatedly calls quantum backends
- **Performance metrics**: Latency, throughput, queue behavior under load
- **Architecture benchmarking**: Compare 3 architectures (Monolith, Sync Microservices, Brokered) under controlled load
- **React Dashboard**: Real-time monitoring, CSV data upload, process flow visualization, architecture benchmarks
- **Petri Net Mapping**: Complete specification for formal modeling and validation with parameterized models

### 2. MatrixMult Benchmark Suite
**Parallel Matrix Multiplication Algorithms**

A .NET 8 benchmark suite implementing and comparing four parallel matrix multiplication algorithms:
- **Sequential**: Baseline blocked algorithm (single-threaded)
- **Striped**: Row/column block-striped decomposition
- **Fox**: Fox's algorithm with broadcast phases
- **Cannon**: Cannon's algorithm with initial skew and cyclic shifts

**Research Applications:**
- Performance comparison of parallel algorithms
- Communication overhead analysis
- Mapping to Petri net parameters for formal modeling
- Scalability studies with varying thread counts and matrix sizes

### 3. PetriObjModelPaint
**Petri Net Modeling and Simulation Tool**

A Java-based graphical tool for creating, editing, and simulating Petri nets. Used for formal modeling and validation of the parallel computing systems.

**Features:**
- Visual Petri net editor
- Simulation and animation capabilities
- Statistical analysis and charting
- PNML import/export support

### 4. Supporting Tools and Utilities
- **CsvToExcelConverter**: Data format conversion utilities
- **CmeMetricsProcessor**: Metrics analysis tools
- **Load testing scripts**: Performance testing automation

## Repository Structure

```
lab45/
├── CME Simulation System (Main Project)
│   ├── cme-dashboard/          # React web UI
│   ├── CmeSim.Api/             # ASP.NET Core backend
│   ├── qbackend/               # Python quantum backend
│   ├── cme-sim-client/         # TypeScript load generator
│   └── docs/                    # Comprehensive documentation
│
├── MatrixMult/                 # Matrix multiplication benchmarks
│   ├── MatrixMult.Core/        # Core data structures
│   ├── MatrixMult.Algorithms/  # Algorithm implementations
│   ├── MatrixMult.Bench/       # Benchmarking infrastructure
│   └── MatrixMult.App/         # CLI application
│
├── PetriObjModelPaint/         # Petri net modeling tool
│   └── src/                    # Java source code
│
├── Tools/                       # Supporting utilities
│   ├── CsvToExcelConverter/
│   └── CmeMetricsProcessor/
│
├── benchmarks/                   # Benchmark scenarios and results
│   └── scenarios/               # Prebuilt scenario JSON files
│
└── data/                        # Sample datasets
```

## Component 1: CME Simulation System Architecture

```
┌─────────────────────────┐       ┌─────────────────────────┐
│  React Dashboard        │       │  TypeScript Client      │  (Load Generator)
│  cme-dashboard (Web UI) │       │  cme-sim-client         │  - Generates traffic
│  - Real-time monitoring │       └───────────┬─────────────┘  - Measures latency
│  - Submit requests      │                   │ HTTP
│  - View metrics         │                   │
└───────────┬─────────────┘                   │
            │ HTTP                            │
            └─────────────────┬───────────────┘
                              ▼
                    ┌─────────────────────────┐
                    │  ASP.NET Core Web API   │  (Main Backend)
                    │  CmeSim.Api             │  - /api/inference/cme (online path)
                    │                         │  - /api/training/* (job management)
                    │  + SQL Server           │  - Background worker (training loop)
                    │  + EF Core              │  - Persists: requests, results, jobs
                    └───────────┬─────────────┘
                                │ HTTP
                                ▼
                    ┌─────────────────────────┐
                    │  Python FastAPI Service │  (Quantum Backend)
                    │  qbackend               │  - Qiskit Aer simulator
                    │                         │  - /qpu/infer endpoint
                    │  + Qiskit               │  - Configurable latency (0.3-2s)
                    └─────────────────────────┘
```

### Request Flows

**Online Inference Path** (latency-critical):
```
Client → POST /api/inference/cme
       → C# IQuantumBackendClient.InferAsync()
       → HTTP POST /qpu/infer (Python)
       → Qiskit circuit execution (simulator or real QPU)
       → Returns p_flow
       → Compute CME = f(features, p_flow, taskDifficulty)
       → Store in DB (CmeWindowResult)
       → Return to client
```

**Training Path** (long-running background):
```
Client → POST /api/training/start
       → Create TrainingJob (status=Queued) in DB
       → Background worker detects job
       → For N generations:
           - For K candidates:
               - Call /qpu/infer
               - Compute fitness
           - Sleep (CPU simulation)
       → Update job (status=Completed)
```

## Component 1: CME Simulation System - Architecture Benchmarking

### Architecture Benchmarking System

The system includes a comprehensive benchmarking framework to compare three different architectural patterns under controlled load:

**Architecture A: Monolith**
- All operations (preprocess, QPU call, DB write) execute synchronously in the API
- Simple, low-latency for single requests
- Limited scalability under high load

**Architecture B: Synchronous Microservices**
- API calls PreprocessService via HTTP, then QPU backend, then DB
- Simulates network delays between services
- Better separation of concerns, but synchronous coupling

**Architecture C: Brokered (Async)**
- API enqueues requests to an in-memory broker queue
- Worker nodes dequeue and process (preprocess + QPU + DB)
- Returns acknowledgment immediately, results available via polling
- Best scalability and throughput under high load

### Benchmark Features

- **Scenario Builder**: Configure load tests with different parameters (clients, requests, workers, QPU concurrency)
- **Metrics Collection**: Comprehensive metrics including:
  - Latency: Average, P95, P99
  - Throughput: Requests per second
  - Failure rates
  - Queue lengths (QPU queue, broker queue)
  - Stage-level timing (validate, preprocess, QPU wait/service, DB write, response)
- **Results Export**: Export benchmark results as CSV or JSON for analysis
- **Petri Net Parameters**: Generate Petri net model parameters from benchmark results
- **Prebuilt Scenarios**: JSON files with predefined test scenarios

### Using Architecture Benchmarks

1. **Via Dashboard**: Navigate to "Architectures Bench" tab
   - Select scenarios to run
   - Click "Run Selected" or "Run All"
   - View results in the results table
   - Export results as CSV/JSON

2. **Via API**: 
   ```bash
   POST /api/benchmarks/run
   GET /api/benchmarks/{runId}
   GET /api/benchmarks/{runId}/export?format=json
   GET /api/benchmarks/{runId}/petri-params
   ```

3. **Prebuilt Scenarios**: Located in `benchmarks/scenarios/`
   - `minimal-set.json`: Quick tests with low/medium load
   - `extended-set.json`: Comprehensive tests with varying workers, QPU backends, shots

### Benchmark Metrics Schema

Each benchmark run collects:
- **Overall metrics**: avg/p95/p99 latency, throughput, failure rate
- **Queue metrics**: average and maximum queue lengths
- **Stage metrics**: Mean and standard deviation for each processing stage
- **Raw events**: Individual timing events for detailed analysis

Results can be exported and used to parameterize Petri net models for formal performance analysis.

## Component 1: CME Simulation System - Detailed Components

### 1.1 React Dashboard (`cme-dashboard/`)

Modern web UI for system monitoring and control:
- **Real-time system status**: View metrics, sessions, and performance
- **Online inference interface**: Submit EEG data and see CME results instantly
- **Training job management**: Start jobs and monitor progress with 4 metaheuristic algorithms
- **Process Flow visualization**: Visual diagrams showing system architecture and Petri net mapping
- **CSV data upload**: Batch processing of EEG data with results comparison
- **Architectures Bench**: Run load tests across 3 architectures, compare metrics, export results for Petri net parameterization
- **Performance visualizations**: Charts showing latency distribution and job status
- **Live activity feed**: Auto-refreshing training job table

Access at: **http://localhost:3000**

### 1.2 TypeScript Simulation Client (`cme-sim-client/`)

Node.js CLI application that generates realistic load:
- **Online inference requests**: Configurable rate (req/sec)
- **Training job submissions**: Configurable rate (jobs/hour)
- **Metrics collection**: Response time distribution (avg, p95, p99), throughput, failures
- **Multi-client support**: Parallel session simulation

### 1.3 ASP.NET Core Web API (`CmeSim.Api/`)

.NET 8 backend with:
- **Controllers**:
  - `InferenceController`: Online CME computation
  - `TrainingController`: Job submission and status (supports GA, PSO, ACO, SA)
  - `DashboardController`: Aggregated metrics
  - `ExperimentsController`: Experimental data management
- **Database** (SQL Server via EF Core):
  - `Session`, `InferenceRequestLog`, `CmeWindowResult`, `TrainingJob`
- **Services**:
  - `IQuantumBackendClient`: HTTP client for Python service
  - `ICmeCalculator`: CME computation logic
  - `TrainingWorkerService`: Background job processor with metaheuristic support

### 1.4 Python Quantum Backend (`qbackend/`)

FastAPI service with Qiskit:
- **Endpoint**: `POST /qpu/infer`
- **Quantum simulation**: 4-qubit circuit with angle encoding and variational ansatz
- **Configurable latency**: `time.sleep(random(0.3, 2.0))` to imitate QPU queue
- **Returns**: `p_flow` (probability), `shotsUsed`, `depth`, `qpuLatencyMs`
- **Support for real hardware**: Optional IBM Quantum integration

## Component 2: MatrixMult Benchmark Suite

### Overview

A comprehensive .NET 8 benchmark suite for comparing parallel matrix multiplication algorithms. Designed to support research on:
- Parallel algorithm performance analysis
- Communication overhead measurement
- Scalability studies
- Mapping to Petri net models for formal validation

### Algorithms Implemented

1. **Sequential Multiplier** (`SequentialMultiplier.cs`)
   - Baseline blocked algorithm (single-threaded)
   - Time Complexity: O(N³)
   - Used for correctness validation

2. **Striped Multiplier** (`StripedMultiplier.cs`)
   - Row/column block-striped decomposition
   - Each thread processes row blocks from A and column blocks from B
   - Good load balancing for uniform block sizes

3. **Fox Multiplier** (`FoxMultiplier.cs`)
   - Uses p × p process grid where p = floor(√threads)
   - Broadcast phases: A blocks broadcast along rows
   - Cyclic shifts: B blocks shifted cyclically
   - Communication overhead: O(log p) per phase

4. **Cannon Multiplier** (`CannonMultiplier.cs`)
   - Uses p × p process grid where p = floor(√threads)
   - Initial skew phase: A blocks shifted left, B blocks shifted up
   - p phases with cyclic shifts: A left by 1, B up by 1
   - Communication overhead: Initial skew + cyclic shifts

### Key Features

- **Comprehensive Metrics**: Compute time, communication overhead, total time, throughput (GFlops), speedup, efficiency
- **Statistical Analysis**: Average, min, max, P95, P99 latencies across multiple iterations
- **Correctness Validation**: Automatic verification against sequential baseline
- **Petri Net Mapping**: Results can be mapped to Petri net parameters for formal modeling
- **Configurable Parameters**: Matrix size, block size, thread count, iterations

### Usage Example

```bash
cd MatrixMult/MatrixMult.App
dotnet run -- --n 1000 --block 100 --algo all --threads 8 --iterations 10
```

### Research Applications

- **Performance Comparison**: Compare algorithm efficiency under different configurations
- **Scalability Analysis**: Study performance with varying thread counts and matrix sizes
- **Communication Overhead**: Measure and analyze communication costs
- **Formal Modeling**: Use results to parameterize Petri net models for validation

## Component 3: PetriObjModelPaint

### Overview

A Java-based graphical tool for creating, editing, and simulating Petri nets. Used for formal modeling and validation of parallel computing systems.

### Features

- **Visual Editor**: Create and edit Petri nets with graphical interface
- **Simulation**: Run simulations with animation
- **Statistical Analysis**: Generate charts and statistics
- **PNML Support**: Import/export PNML format
- **Custom Libraries**: Support for custom Petri net libraries

### Usage

```bash
cd PetriObjModelPaint
# Windows
run.bat

# Linux/Mac
./run.sh
```

### Research Applications

- **Formal Modeling**: Create Petri net models of parallel algorithms
- **Validation**: Compare Petri net simulation results with actual system measurements
- **Performance Prediction**: Use Petri nets to predict system behavior under different loads

## Getting Started

### Prerequisites

**For CME Simulation System:**
- Node.js 20+
- .NET 8 SDK
- Python 3.11+
- SQL Server (or Docker)
- Docker & Docker Compose (optional, for containerized setup)

**For MatrixMult Benchmarks:**
- .NET 8 SDK

**For PetriObjModelPaint:**
- Java JDK 11 or higher
- Maven (for building)

### Quick Start Guide

#### CME Simulation System (Docker - Recommended)

```bash
# Start all services (SQL Server, Web API, Python backend, Dashboard)
docker-compose up -d

# Wait for services to initialize (~30 seconds)
# The Web API will automatically run migrations

# Access the dashboard
# Open http://localhost:3000 in your browser

# Run simulation client (from host)
cd cme-sim-client
npm install
npm run simulate -- --duration 60 --onlineRate 2 --trainRate 0.1
```

#### MatrixMult Benchmarks

```bash
cd MatrixMult/MatrixMult.App
dotnet restore
dotnet build
dotnet run -- --n 500 --algo all --threads 4
```

#### PetriObjModelPaint

```bash
cd PetriObjModelPaint
# Windows
run.bat

# Linux/Mac
chmod +x run.sh
./run.sh
```

### Manual Setup

#### 1. SQL Server

```bash
# Option A: Docker
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

# Option B: Use local SQL Server instance
```

#### 2. Python Quantum Backend

```bash
cd qbackend
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt

# Run service
uvicorn app.main:app --host 0.0.0.0 --port 8001
```

Service runs on `http://localhost:8001`

#### 3. ASP.NET Core Web API

```bash
cd CmeSim.Api

# Update connection string in appsettings.json if needed

# Run migrations
dotnet ef database update

# Start API
dotnet run
```

API runs on `http://localhost:5000` (or check console output)

#### 4. TypeScript Simulation Client

```bash
cd cme-sim-client
npm install

# Run simulation
npm run simulate -- --duration 120 --onlineRate 1 --trainRate 0.05 --clients 5
```

**Parameters**:
- `--duration`: Simulation duration in seconds (default: 60)
- `--onlineRate`: Online inference requests per second (default: 1)
- `--trainRate`: Training jobs per minute (default: 0.1)
- `--clients`: Number of parallel client sessions (default: 1)

### Example Simulation Output

```
=== CME Simulation Started ===
Duration: 120s | Online Rate: 1.0 req/s | Training Rate: 0.05 jobs/min | Clients: 5

[00:05] Online requests: 25 | Training jobs: 0 | Avg latency: 1234ms
[00:10] Online requests: 50 | Training jobs: 1 | Avg latency: 1189ms
...

=== Simulation Complete ===

Online Inference Metrics:
  Total requests:    120
  Successful:        118
  Failed:            2
  Avg latency:       1205 ms
  P95 latency:       2340 ms
  P99 latency:       3120 ms
  Throughput:        0.98 req/s

Training Job Metrics:
  Total submitted:   2
  Completed:         1
  Running:           1
  Avg completion:    45.3 s
```

## Research Applications

### CME Simulation System Research

This imitation model supports research on:

1. **Performance Analysis**:
   - Measure latency under load with varying request rates
   - Observe how QPU queue delays propagate to end-user latency
   - Analyze queue behavior and resource contention

2. **Queueing Theory**:
   - Model as queueing network: M/M/1 or M/G/1 queue at QPU
   - Study impact of training jobs on online inference latency
   - Validate Petri net models against real system measurements

3. **Metaheuristic Comparison**:
   - Compare GA, PSO, ACO, and Simulated Annealing algorithms
   - Analyze convergence rates and fitness improvements
   - Study QPU utilization patterns

4. **Formal Modeling**:
   - Create Petri net models from system architecture
   - Validate models using experimental data
   - Predict system behavior under different configurations

### MatrixMult Research

The benchmark suite supports:

1. **Algorithm Performance Comparison**:
   - Compare sequential, striped, Fox, and Cannon algorithms
   - Analyze scalability with varying thread counts
   - Measure communication overhead

2. **Petri Net Parameter Extraction**:
   - Map compute times to transition firing times
   - Extract communication overhead for modeling
   - Use throughput metrics for validation

3. **Scalability Studies**:
   - Performance analysis with varying matrix sizes
   - Block size optimization
   - Thread count efficiency analysis

### Petri Net Modeling Research

PetriObjModelPaint enables:

1. **Formal System Modeling**:
   - Create Petri net models of parallel algorithms
   - Model queueing networks
   - Represent system architecture formally

2. **Simulation and Validation**:
   - Run Petri net simulations
   - Compare results with actual system measurements
   - Validate model accuracy

3. **Performance Prediction**:
   - Predict system behavior under different loads
   - Analyze bottlenecks and resource utilization
   - Optimize system configurations

## Database Schema

```sql
Sessions
  - Id (GUID, PK)
  - UserId (string)
  - StartedAt, EndedAt

InferenceRequestLog
  - Id (GUID, PK)
  - SessionId (FK)
  - WindowId (string)
  - RequestedAt (DateTime)
  - TotalLatencyMs, QpuLatencyMs

CmeWindowResult
  - Id (GUID, PK)
  - SessionId (FK)
  - WindowId (string)
  - CmeValue, PFlow (double)
  - ShotsUsed, Depth (int)

TrainingJob
  - Id (GUID, PK)
  - Status (Queued/Running/Completed/Failed)
  - TotalGenerations, CompletedGenerations
  - BestFitness (double)
  - TotalQpuCalls (int)
```

## 🎓 Understanding the Algorithms & Parameters

### Quick Summary

**Quantum Algorithm**: Variational Quantum Classifier (VQC)
- 4-qubit circuit with trainable rotation angles
- Classifies EEG features → flow state probability
- Similar to a quantum neural network

**Metaheuristic**: Evolutionary Algorithm (Genetic Algorithm)
- Optimizes 8 circuit parameters (rotation angles)
- 5 candidates per generation, tracks best fitness
- Goal: Maximize flow state detection accuracy

**Training Data**: 
- Real system: Labeled EEG recordings (Flow vs No Flow)
- This simulation: Random features (for performance testing only)

**CME Formula**: `CME = k × Energy(features) × g(difficulty, p_flow)`
- Energy = sum of EEG feature magnitudes
- Higher flow probability + harder task = higher CME

## Documentation

### CME Simulation System Documentation

**Essential Reading** (in `docs/` directory):
- **[START_HERE.md](docs/START_HERE.md)** - Complete getting started guide
- **[WHAT_IS_WHAT.md](docs/WHAT_IS_WHAT.md)** - Big picture explanation
- **[DISSERTATION_GUIDE.md](docs/DISSERTATION_GUIDE.md)** - PhD research workflow
- **[PETRI_NET_MODEL.md](docs/PETRI_NET_MODEL.md)** - Complete Petri net specification
- **[VISUAL_GUIDE.md](docs/VISUAL_GUIDE.md)** - UI element explanations
- **[ALGORITHMS_EXPLAINED.md](docs/ALGORITHMS_EXPLAINED.md)** - Technical deep dive
- **[QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md)** - Quick lookup tables

**Component-Specific Documentation**:
- `CmeSim.Api/README.md` - API backend documentation
- `qbackend/README.md` - Quantum backend documentation
- `cme-dashboard/README.md` - Dashboard documentation
- `cme-sim-client/README.md` - Simulation client documentation

### MatrixMult Documentation

- `MatrixMult/README.md` - Complete benchmark suite documentation
- `MatrixMult/RUN_BENCHMARK_TABLE.md` - Benchmark execution guide

### PetriObjModelPaint Documentation

- `PetriObjModelPaint/README.md` - Tool usage and features

### Additional Resources

- `docs/PROJECT_STRUCTURE.md` - Complete codebase overview
- `docs/SETUP.md` - Detailed setup instructions
- `docs/TROUBLESHOOTING.md` - Common issues and solutions
- `docs/INDEX.md` - Complete documentation index

## Troubleshooting

If you encounter issues (e.g., SQL Server health check failures, missing tables, connection errors), see **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** for solutions.

## Configuration

### ASP.NET Core (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CmeSimDb;..."
  },
  "QuantumBackend": {
    "BaseUrl": "http://localhost:8001"
  },
  "TrainingWorker": {
    "PollingIntervalSeconds": 5,
    "GenerationsPerJob": 10,
    "CandidatesPerGeneration": 5
  }
}
```

### Python Backend (`qbackend/.env`)

```env
QPU_LATENCY_MIN_MS=300
QPU_LATENCY_MAX_MS=2000
DEFAULT_SHOTS=1024
IBMQ_TOKEN=  # Optional: for real IBM Quantum access
```

## Example HTTP Requests

See `requests.http` for manual testing with REST Client extension.

## Queueing Model Mapping

This system can be viewed as a queueing network:

- **Arrivals**: Online inference (Poisson λ), Training jobs (Poisson λ_train)
- **Service stations**:
  - Web API server (CPU-bound, fast: ~10ms)
  - QPU backend (bottleneck: ~0.3-2s per request)
- **Queues**:
  - HTTP connection pool
  - QPU request queue (implicit in Python service)
  - Training job queue (DB table with background worker)

Petri net view:
- **Places**: {ClientReady, RequestQueued, InQPU, ResultReady}
- **Transitions**: {Submit, StartQPU, FinishQPU, StoreResult}
- **Tokens**: Individual requests

## Extending the Model

To make this more realistic:

1. **Add variability**: Vary EEG feature dimensions, task difficulty
2. **Resource limits**: Limit concurrent QPU requests (semaphore)
3. **Failure modes**: Simulate QPU timeouts, circuit errors
4. **Adaptive algorithms**: Training jobs that change based on fitness
5. **Real quantum hardware**: Configure IBMQ token to use actual IBM Quantum backends

## Project Summary

### Research Goals

This repository supports PhD dissertation research on:

1. **Performance Analysis of Quantum ML Systems**:
   - Understanding latency characteristics of quantum-enhanced applications
   - Analyzing queue behavior under mixed workloads (online inference + training)
   - Comparing metaheuristic optimization algorithms

2. **Parallel Algorithm Performance**:
   - Benchmarking parallel matrix multiplication algorithms
   - Analyzing communication overhead and scalability
   - Comparing different parallelization strategies

3. **Formal Modeling and Validation**:
   - Creating Petri net models of parallel and quantum systems
   - Validating models against experimental measurements
   - Using formal methods for performance prediction

### Key Contributions

- **Complete Imitation Model**: Full-stack quantum ML system for performance analysis
- **Comprehensive Benchmarks**: Multiple parallel algorithms with detailed metrics
- **Formal Modeling Support**: Petri net specifications and mapping guidelines
- **Extensive Documentation**: 20+ guides covering all aspects of the system

### Technology Stack Summary

| Component | Technologies |
|-----------|-------------|
| **CME Dashboard** | React, TypeScript, Vite |
| **CME API** | .NET 8, ASP.NET Core, EF Core, SQL Server |
| **Quantum Backend** | Python 3.11, FastAPI, Qiskit |
| **Simulation Client** | TypeScript, Node.js, Axios |
| **MatrixMult** | C# .NET 8, Parallel Processing |
| **Petri Net Tool** | Java, Swing, Maven |
| **Infrastructure** | Docker, Docker Compose |

### System Capabilities

✅ **Real-time Performance Monitoring**: Dashboard with live metrics  
✅ **Load Testing**: Configurable simulation client  
✅ **Multiple Algorithms**: 4 metaheuristics + 4 matrix multiplication algorithms  
✅ **Formal Modeling**: Complete Petri net specifications  
✅ **Data Analysis**: CSV processing, metrics export, statistical analysis  
✅ **Scalability Studies**: Configurable parameters for various scenarios  

### Research Workflow

1. **System Understanding**: Use dashboard and documentation to understand system behavior
2. **Data Collection**: Run experiments with simulation client and benchmarks
3. **Formal Modeling**: Create Petri net models using specifications
4. **Validation**: Compare Petri net results with experimental data
5. **Analysis**: Statistical comparison and performance prediction

### File Organization

- **Main Components**: Each major component has its own directory with README
- **Documentation**: Comprehensive guides in `docs/` directory
- **Data**: Sample datasets in `data/` and `example_data/`
- **Tools**: Supporting utilities in `Tools/`
- **Scripts**: Automation scripts for testing and benchmarking

## License

MIT - For academic/research use in PhD project.

## Contact

For questions about this research project, refer to the comprehensive documentation in the `docs/` directory or component-specific README files.

