# Algorithms and Training Explained

## Overview

This document explains **exactly what is being trained, optimized, and computed** in the CME Quantum ML System.

---

## 1. Quantum Machine Learning Algorithm: Variational Quantum Classifier (VQC)

### What is VQC?

A **Variational Quantum Classifier** is a hybrid quantum-classical algorithm for supervised learning, similar to a quantum neural network.

### Circuit Architecture (4 Qubits)

```
Layer 1: Feature Encoding (Angle Encoding)
─────────────────────────────────────────
Qubit 0: ──Ry(θ₀)──●─────────────Ry──Rz──M
Qubit 1: ──Ry(θ₁)──X──●──────────Ry──Rz──M
Qubit 2: ──Ry(θ₂)─────X──●───────Ry──Rz──M
Qubit 3: ──Ry(θ₃)────────X──●────Ry──Rz──M
            ↑               │
       EEG Features    Entangling
       θᵢ = (featureᵢ + 1) × π
```

**Layer 1: Angle Encoding**
- Encodes EEG features into quantum state
- Each feature → rotation angle on a qubit
- Formula: `θᵢ = (featureᵢ + 1) × π`
- Assumes features are normalized to [-1, 1]

**Layer 2: Entangling**
- Creates quantum correlations between qubits
- Uses CNOT (CX) gates in a chain
- Allows circuit to learn complex patterns

**Layer 3: Variational Ansatz**
- Trainable parameters: `{Ry(θ), Rz(φ)}`
- These are optimized during training
- **Current State**: Fixed values (0.5, 1.2, etc.) - simulates "trained" model

**Measurement**
- Measures all qubits in computational basis
- Returns bit string (e.g., "0101")
- Repeat 1024 times (shots) for statistics

### Output: Flow Probability

```python
p_flow = P(qubit_0 = |1⟩)
```

**Interpretation**:
- p_flow = 0.8 → 80% probability of "flow" mental state
- p_flow = 0.2 → 20% probability of "flow" mental state
- This is extracted from measurement statistics

---

## 2. Training Data (In Real System)

### What Would Be Trained?

In a production system, you would have:

**Dataset**: Labeled EEG recordings
```
Training Examples:
┌────────────────┬─────────────────────┬───────┐
│ EEG Features   │ Task Context        │ Label │
├────────────────┼─────────────────────┼───────┤
│ [0.5, -0.3,...]│ Difficulty: 0.7     │ Flow  │
│ [0.1, 0.8, ...]│ Difficulty: 0.3     │ Flow  │
│ [-0.2, 0.4,...]│ Difficulty: 0.9     │ No    │
│ [0.9, -0.1,...]│ Difficulty: 0.5     │ Flow  │
└────────────────┴─────────────────────┴───────┘
```

**EEG Features** (8-dimensional):
1. Alpha power (8-13 Hz)
2. Beta power (13-30 Hz)
3. Theta power (4-8 Hz)
4. Delta power (0.5-4 Hz)
5. Frontal asymmetry
6. Parietal asymmetry
7. Heart rate variability
8. Task engagement score

**Labels**:
- Binary: Flow (1) vs No Flow (0)
- Based on self-reports, behavioral metrics, or expert annotations

### In This Simulation

**No real training data** - instead:
- Random features generated: `[random(-1, 1) for _ in range(8)]`
- Used to evaluate candidate models during optimization
- Simulates the process without needing actual EEG recordings

---

## 3. Metaheuristic Algorithm: Simplified Evolutionary Strategy

### Algorithm Type

**Evolutionary Algorithm** (Genetic Algorithm style)

### What is Being Optimized?

**Objective**: Find the best variational parameters `{θ, φ}` for the quantum circuit that maximize classification accuracy on validation data.

### Parameters Being Optimized

```
Variational Parameters (per qubit):
- θᵢ: Rotation angle for Ry gate (Layer 3)
- φᵢ: Rotation angle for Rz gate (Layer 3)

Total: 4 qubits × 2 parameters = 8 parameters
```

**Current State**: Fixed at `[0.5, 1.2, 0.7, 1.05, 0.9, 0.9, 1.1, 0.75]`  
**During Training**: These would be optimized

### Evolutionary Strategy Pseudocode

```python
def train_quantum_model(generations, candidates_per_generation):
    # Initialize population
    population = [random_parameters() for _ in range(candidates_per_generation)]
    best_fitness = 0
    best_parameters = None
    
    for generation in range(generations):
        # Evaluate all candidates
        fitness_scores = []
        for candidate in population:
            # Set quantum circuit parameters
            circuit = build_circuit_with_params(candidate)
            
            # Evaluate on validation set (via quantum backend)
            accuracy = evaluate_accuracy(circuit, validation_data)
            fitness_scores.append(accuracy)
            
            if accuracy > best_fitness:
                best_fitness = accuracy
                best_parameters = candidate
        
        # Select best candidates (elitism)
        survivors = select_top_k(population, fitness_scores, k=candidates_per_generation//2)
        
        # Generate new candidates (mutation + crossover)
        offspring = []
        for i in range(candidates_per_generation//2):
            parent1, parent2 = random.sample(survivors, 2)
            child = crossover(parent1, parent2)
            child = mutate(child, mutation_rate=0.1)
            offspring.append(child)
        
        # New population
        population = survivors + offspring
    
    return best_parameters, best_fitness
```

### In This Simulation

**Simplified Version**:
1. Generate random features (simulates validation samples)
2. Call quantum backend with current "model"
3. Compute synthetic fitness = `p_flow * (1 + small_random_noise)`
4. Track best fitness across all candidates
5. No actual parameter updates (parameters stay fixed)

**Reason**: This is an **imitation model** - we simulate the **process** and **performance characteristics** without full training logic.

---

## 4. CME (Countable Mental Energy) Formula

### Formula

```
CME(t) = k × Energy(features) × g(task_difficulty, p_flow)
```

### Components

**Energy(features)**:
```python
energy = sum(abs(feature) for feature in features)
```
- Sum of absolute values of EEG features
- Higher EEG activity → higher energy

**Scaling Function g(difficulty, p_flow)**:
```python
flow_factor = 1.0 + p_flow           # Range: [1.0, 2.0]
difficulty_factor = 0.5 + difficulty  # Range: [0.5, 1.5]
g = flow_factor × difficulty_factor
```
- Higher flow probability → higher CME
- Higher task difficulty → higher CME
- Represents mental resource utilization

**Scaling Constant k**:
```python
k = 10.0
```
- Calibration constant (would be determined empirically)

### Example Calculation

Given:
- Features: `[0.5, -0.3, 0.8, 0.1, -0.2, 0.6, 0.0, -0.4]`
- p_flow: `0.65`
- Task difficulty: `0.7`

Compute:
```
energy = |0.5| + |−0.3| + |0.8| + |0.1| + |−0.2| + |0.6| + |0| + |−0.4|
       = 2.9

flow_factor = 1.0 + 0.65 = 1.65
difficulty_factor = 0.5 + 0.7 = 1.2
g = 1.65 × 1.2 = 1.98

CME = 10.0 × 2.9 × 1.98 = 57.42
```

### Interpretation

- **CME = 30-50**: Moderate mental energy, comfortable task
- **CME = 50-80**: High mental energy, challenging but manageable
- **CME = 80+**: Very high mental energy, peak performance or overload

---

## 5. What Gets Trained in Real System

### Full Training Pipeline

```
Step 1: Data Collection
├─ Record EEG during tasks (gaming, coding, learning)
├─ Label mental states (flow, boredom, stress, etc.)
└─ Extract features (power bands, asymmetry, etc.)

Step 2: Quantum Circuit Design
├─ Choose number of qubits (4-8)
├─ Design ansatz (variational layers)
└─ Initialize parameters randomly

Step 3: Hybrid Training
├─ For each training batch:
│   ├─ Encode features → quantum circuit
│   ├─ Execute on quantum backend
│   ├─ Measure outputs
│   ├─ Compute loss (cross-entropy)
│   └─ Update parameters (gradient descent or metaheuristic)
└─ Repeat until convergence

Step 4: Validation
├─ Test on held-out EEG data
├─ Measure accuracy, precision, recall
└─ Compare to classical ML baseline

Step 5: Deployment
├─ Use trained parameters in production
├─ Real-time inference on live EEG
└─ Compute CME continuously
```

### Why Quantum?

**Potential Advantages**:
1. **Exponential state space**: n qubits → 2ⁿ dimensional Hilbert space
2. **Entanglement**: Captures complex correlations in EEG data
3. **Quantum interference**: Natural feature interactions
4. **Noise resilience**: Variational algorithms handle noisy quantum hardware

**Research Question**: Does quantum ML outperform classical ML for EEG classification?

---

## 6. Parameters in the Dashboard

### Online Inference Panel

**Session ID**:
- Identifies an EEG recording session
- Used for tracking and aggregation
- Example: `11111111-1111-1111-1111-111111111111`

**Window ID**:
- Unique identifier for a time window (e.g., 1-second segment)
- EEG is segmented into overlapping windows
- Example: `window-1732038456789`

**Task Difficulty** (0-1 slider):
- Subjective or measured task complexity
- 0.0 = Very easy task
- 0.5 = Moderate task
- 1.0 = Very difficult task
- Affects CME scaling

**EEG Features** (8-dimensional array):
- Preprocessed EEG signal features
- Each value in [-1, 1] range (normalized)
- Example: `[0.5, -0.3, 0.8, 0.1, -0.2, 0.6, 0.0, -0.4]`
- In real system: extracted from raw EEG using FFT, filters, etc.

### Training Jobs Panel

**Total Generations**:
- Number of optimization iterations
- Each generation evaluates multiple candidate models
- Higher = better optimization (but slower)
- Example: 10 generations × 5 candidates = 50 quantum backend calls

---

## 7. Performance Metrics Explained

### Response Time Distribution

**Average Latency**:
- Mean time for one CME computation
- Includes: API processing + QPU execution + database write
- Typically: 1200-2000ms (dominated by QPU)

**P95 Latency** (95th percentile):
- 95% of requests complete within this time
- Indicator of typical performance
- Important for user experience

**P99 Latency** (99th percentile):
- 99% of requests complete within this time
- Captures tail latency (worst-case scenarios)
- Critical for SLA guarantees

### Training Job Metrics

**Best Fitness**:
- Highest accuracy achieved across all candidates
- Range: [0, 1] (higher is better)
- Example: 0.85 = 85% classification accuracy

**Total QPU Calls**:
- Number of times quantum backend was invoked
- Each call = 1024 shots on quantum circuit
- Resource consumption metric

---

## 8. Why This Is an "Imitation Model"

### What's Simulated

✅ **Request flows** (client → API → QPU → database)  
✅ **QPU latency** (300-2000ms delay)  
✅ **Training loop structure** (generations, candidates, evaluation)  
✅ **Performance characteristics** (throughput, latency, queues)  
✅ **Quantum circuit execution** (Qiskit Aer simulator)

### What's NOT Real

❌ **No actual EEG data** - random features generated  
❌ **No real training** - parameters stay fixed  
❌ **No gradient computation** - fitness is synthetic  
❌ **No model improvement** - "trained" model doesn't actually improve  
❌ **No accuracy validation** - no ground truth labels

### Purpose

This imitation model is designed for:
- **Performance analysis**: Measure latency, throughput, queue behavior
- **Queueing theory**: Study M/M/1, M/G/1 models
- **Resource planning**: Estimate QPU usage, database load
- **PhD research**: Understand system behavior without expensive real QML

---

## 9. Extending to Real Training

### What You'd Need to Add

1. **Real Dataset**:
   ```python
   eeg_data = load_eeg_dataset("flow_vs_noflow.csv")
   X_train, X_test, y_train, y_test = train_test_split(eeg_data)
   ```

2. **Parameter Optimization**:
   ```python
   from qiskit.algorithms.optimizers import COBYLA
   
   def objective(params):
       circuit = build_circuit(params)
       predictions = [execute_circuit(circuit, x) for x in X_train]
       return -accuracy_score(y_train, predictions)  # Minimize negative accuracy
   
   result = COBYLA().minimize(objective, initial_params)
   best_params = result.x
   ```

3. **Validation Loop**:
   ```python
   accuracy = evaluate_model(best_params, X_test, y_test)
   print(f"Test Accuracy: {accuracy:.2%}")
   ```

4. **Real IBM Quantum Hardware** (optional):
   ```python
   from qiskit_ibm_runtime import QiskitRuntimeService
   
   service = QiskitRuntimeService(token="YOUR_TOKEN")
   backend = service.backend("ibm_brisbane")  # 127-qubit system
   ```

---

## Summary

| Component | Algorithm | What's Optimized | Current State |
|-----------|-----------|------------------|---------------|
| **Quantum Circuit** | VQC (Variational Quantum Classifier) | Rotation angles {θ, φ} | Fixed "trained" values |
| **Training** | Evolutionary Strategy (Genetic Algorithm) | Circuit parameters to maximize accuracy | Simulated (no real updates) |
| **Inference** | Quantum measurement + post-processing | N/A (uses trained params) | Working (simulated QPU) |
| **CME Calculation** | Analytical formula | N/A | Working |

**Key Insight**: This is a **working simulation** of the full pipeline, designed for performance analysis, not actual quantum ML research.

For real quantum ML, you'd replace the simulation with actual training loops, real EEG data, and optionally real quantum hardware.


