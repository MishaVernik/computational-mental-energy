# Quick Reference - Algorithms & Parameters

Quick lookup for what everything means in the CME system.

## Online Inference Parameters

| Parameter | Meaning | Example | Range/Format |
|-----------|---------|---------|--------------|
| **Session ID** | EEG recording session identifier | `11111111-1111-1111-1111-111111111111` | GUID |
| **Window ID** | 1-second EEG segment ID | `window-1732038456789` | String |
| **Task Difficulty** | Task complexity level | `0.7` (challenging) | 0.0 (easy) to 1.0 (very hard) |
| **EEG Features** | 8D normalized feature vector | `[0.5, -0.3, 0.8, ...]` | 8 floats in [-1, 1] |

### EEG Features Breakdown (8 dimensions)

1. **Alpha Power** (8-13 Hz) - Relaxation, flow
2. **Beta Power** (13-30 Hz) - Active thinking, concentration
3. **Theta Power** (4-8 Hz) - Deep focus, creativity
4. **Delta Power** (0.5-4 Hz) - Deep sleep, unconscious processing
5. **Frontal Asymmetry** - Left vs right frontal lobe activity
6. **Parietal Asymmetry** - Left vs right parietal activity
7. **HRV** (Heart Rate Variability) - Stress/relaxation indicator
8. **Engagement Score** - Overall cognitive engagement level

## Training Job Parameters

| Parameter | Meaning | Example | Range |
|-----------|---------|---------|-------|
| **Total Generations** | Evolutionary algorithm iterations | `10` | 5-50 |

**What happens per generation:**
- 5 candidate models are generated
- Each is evaluated on the quantum backend
- Best fitness is tracked
- Parameters evolve toward better solutions

**Total QPU calls** = Generations × Candidates = 10 × 5 = **50 calls**

## Quantum Circuit (VQC)

### Architecture
```
4 Qubits, 3 Layers:

1. Feature Encoding:  EEG → R_y(θ) rotations
2. Entangling:        CNOT gates (creates correlations)
3. Variational:       R_y(θ), R_z(φ) trainable parameters
4. Measurement:       1024 shots → probability distribution
```

### Parameters Being Optimized

**8 rotation angles** (2 per qubit):
```
{θ₀, φ₀, θ₁, φ₁, θ₂, φ₂, θ₃, φ₃}
```

**Current state**: Fixed at `[0.5, 1.2, 0.7, 1.05, 0.9, 0.9, 1.1, 0.75]` (simulates "trained")

**In real training**: These would be updated via metaheuristic/gradient descent to maximize accuracy

## Output Metrics

### Online Inference Results

| Metric | Meaning | Typical Range |
|--------|---------|---------------|
| **CME** | Countable Mental Energy | 30-80 |
| **p_flow** | Probability of flow state | 0.0-1.0 (0-100%) |
| **Shots Used** | Quantum measurements taken | 1024 |
| **Circuit Depth** | Number of quantum gate layers | 8-12 |
| **QPU Latency** | Quantum backend execution time | 300-2000 ms |
| **Total Latency** | End-to-end response time | 1200-2500 ms |

### CME Interpretation

- **< 30**: Low mental energy, easy task or disengagement
- **30-50**: Moderate mental energy, comfortable performance
- **50-80**: High mental energy, optimal challenge (flow zone)
- **> 80**: Very high mental energy, peak performance or cognitive overload

### Training Job Results

| Metric | Meaning | Range |
|--------|---------|-------|
| **Best Fitness** | Highest classification accuracy achieved | 0.0-1.0 |
| **Total QPU Calls** | Number of quantum backend invocations | Generations × 5 |
| **Completed Generations** | Progress indicator | 0 to Total |

## CME Formula

```
CME = k × Energy(features) × g(difficulty, p_flow)

Where:
  Energy = Σ |feature_i|  (sum of absolute values)
  g = (1 + p_flow) × (0.5 + difficulty)
  k = 10.0 (scaling constant)
```

### Example Calculation

```
Features: [0.5, -0.3, 0.8, 0.1, -0.2, 0.6, 0.0, -0.4]
p_flow: 0.65
Difficulty: 0.7

Energy = 0.5 + 0.3 + 0.8 + 0.1 + 0.2 + 0.6 + 0.0 + 0.4 = 2.9
g = (1 + 0.65) × (0.5 + 0.7) = 1.65 × 1.2 = 1.98

CME = 10.0 × 2.9 × 1.98 = 57.42
```

## Algorithms Used

### Quantum ML: Variational Quantum Classifier (VQC)

- **Type**: Hybrid quantum-classical supervised learning
- **Similar to**: Quantum neural network
- **Purpose**: Binary classification (Flow vs No Flow)
- **Input**: 8D EEG feature vector
- **Output**: Probability p_flow ∈ [0, 1]

### Metaheuristic: Evolutionary Algorithm

- **Type**: Genetic Algorithm / Evolutionary Strategy
- **Optimizes**: 8 quantum circuit parameters
- **Objective**: Maximize classification accuracy
- **Method**: Selection, crossover, mutation
- **Fitness**: Accuracy on validation set

### In This Simulation

✅ **What's Real:**
- Quantum circuit execution (Qiskit Aer)
- Request/response flows
- Performance characteristics (latency, throughput)
- Database persistence
- Training loop structure

❌ **What's Simulated:**
- Training data (random instead of real EEG)
- Parameter updates (fixed instead of optimized)
- Fitness computation (synthetic instead of validated)
- Model improvement (doesn't actually get better)

**Purpose**: Performance analysis, queueing theory, resource planning

## Dashboard Status Colors

| Color | Status | Meaning |
|-------|--------|---------|
| 🟢 Green | Healthy, Completed | Success, normal operation |
| 🔵 Blue | Running, In Progress | Active processing |
| 🟡 Yellow | Queued, Waiting | Pending, not started yet |
| 🔴 Red | Failed, Error | Something went wrong |

## Useful Commands

```bash
# View all services
docker-compose ps

# Check logs
docker logs cme-api
docker logs cme-qbackend
docker logs cme-dashboard

# Query database
docker exec cme-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -d CmeSimDb -Q "SELECT COUNT(*) FROM InferenceRequestLogs"

# Run simulation
cd cme-sim-client
npm run simulate -- --duration 60 --onlineRate 2

# Restart a service
docker-compose restart dashboard
```

## Quick Links

- **Dashboard**: http://localhost:3000
- **API Swagger**: http://localhost:5000/swagger
- **Quantum Backend Health**: http://localhost:8001/health
- **Full Algorithm Docs**: [ALGORITHMS_EXPLAINED.md](ALGORITHMS_EXPLAINED.md)
- **Troubleshooting**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- **Dashboard Guide**: [DASHBOARD_GUIDE.md](DASHBOARD_GUIDE.md)


