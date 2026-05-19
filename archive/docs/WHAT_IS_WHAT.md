# What Is What? - Big Picture Explanation

## Overview

This is an **imitation model** built for a PhD dissertation comparing:
1. **Quantum ML Web Application** (this implementation)
2. **Petri Net Model** (separate formal model)

The goal is to validate that the Petri net correctly models the behavior of the real system.

---

## The Research Question

**Does a Petri net model accurately represent the performance and behavior of a quantum machine learning web application for real-time EEG-based mental state detection?**

To answer this, you need:
- ✅ A working implementation (this codebase)
- ✅ A Petri net formal model (your separate work)
- ✅ Performance data from both to compare

---

## What This System Does (Conceptually)

### Scenario: Monitoring Mental "Flow" State

Imagine a researcher wearing an EEG headset while doing cognitive tasks:

1. **EEG sensors** record brain activity (μV signals from electrodes)
2. **System preprocesses** signals → extracts features (power bands, asymmetry)
3. **Quantum classifier** analyzes features → predicts flow probability
4. **CME metric** quantifies mental energy expenditure
5. **Dashboard** shows real-time feedback and historical trends

### Why Quantum?

- EEG patterns are complex, high-dimensional, noisy
- Quantum entanglement might capture brain correlations better
- Research hypothesis: Quantum ML ≥ Classical ML for this task

---

## What Each Component Represents

### 1. EEG Data (Input)

**In Real World**:
- Raw signals from 64-channel EEG cap
- Sampled at 500 Hz
- Artifacts removed (blinks, movement)
- Segmented into 1-second windows
- Features extracted via FFT, wavelets, statistics

**In This Simulation**:
- Pre-extracted 8D feature vectors
- Each dimension = one EEG characteristic
- Normalized to [-1, 1] range

### 2. Quantum Circuit (Classifier)

**What It Is**:
- A quantum neural network (VQC)
- 4 qubits storing quantum state
- Trainable parameters (rotation angles)
- Outputs: probability of "flow" mental state

**How It Works**:
```
EEG Features → Encode into quantum state → 
Apply trainable rotations → Measure → 
Probability distribution → Extract p_flow
```

**Why 4 Qubits?**:
- 4 qubits = 2⁴ = 16-dimensional Hilbert space
- Can represent complex patterns
- Practical for current quantum hardware

### 3. Training (Optimization)

**What's Being Trained**:
- The 8 rotation angles in the quantum circuit
- These determine how the circuit classifies inputs

**Goal**:
- Find angles that maximize accuracy on labeled data
- "Accuracy" = how often it predicts flow correctly

**Method**:
- Metaheuristic optimization (no gradients needed)
- Try different angle combinations
- Keep the best ones
- Evolve toward better solutions

### 4. CME Metric (Output)

**What It Measures**:
- Mental energy expenditure per time window
- Combines: brain activity, flow state, task difficulty

**Formula**:
```
CME = 10 × (sum of EEG magnitudes) × (1 + flow_prob) × (0.5 + difficulty)
```

**Use Case**:
- Track cognitive load over time
- Detect when user is in optimal flow zone
- Warn if mental fatigue is high
- Optimize task scheduling

---

## Data Flow: From Brain to Dashboard

```
1. BRAIN ACTIVITY
   ↓ (electrical signals)
   
2. EEG SENSORS (64 channels @ 500 Hz)
   ↓ (preprocessing: filters, artifact removal)
   
3. FEATURE EXTRACTION
   • FFT → Power spectral density
   • Alpha (8-13 Hz), Beta (13-30 Hz), etc.
   • Asymmetry metrics (left vs right)
   • HRV from ECG
   ↓ (8D feature vector per 1-second window)
   
4. QUANTUM CLASSIFIER (via API)
   • Encode features into quantum state
   • Run circuit with trained parameters
   • Measure 1024 shots
   • Extract p_flow
   ↓ (probability: 0.623 = 62.3% flow)
   
5. CME COMPUTATION
   • Combine: features + p_flow + difficulty
   • Apply formula
   ↓ (CME value: 57.42)
   
6. DATABASE STORAGE
   • Store: CME, p_flow, latency metrics
   • Historical trends
   ↓
   
7. DASHBOARD VISUALIZATION
   • Real-time display
   • Charts and tables
   • Performance analytics
```

---

## Imitation vs Real System

### What's Real in This Implementation

✅ **Architecture**: Multi-tier web app (React → API → Quantum Backend → DB)  
✅ **Request flows**: HTTP requests, database persistence, async processing  
✅ **Quantum circuits**: Actual Qiskit code running on simulator  
✅ **Performance characteristics**: Realistic latencies, queue behavior  
✅ **Training loop structure**: Background worker, generations, candidates  

### What's Simulated (Imitation)

❌ **Training data**: Random instead of real EEG recordings  
❌ **Model improvement**: Parameters stay fixed (don't actually optimize)  
❌ **Validation**: No ground truth labels to compare against  
❌ **Accuracy metrics**: Synthetic fitness values  

### Why This Approach?

**For dissertation purposes**:
- You don't need a fully working quantum ML model
- You need to analyze **system behavior**: latency, throughput, queues
- Petri net models **process flows**, not ML accuracy
- This imitation captures the essential **performance characteristics**

---

## Metaheuristic Algorithms (What's Being Compared)

### Evolutionary Algorithm (Current)
- **Inspired by**: Natural selection
- **Operations**: Selection, crossover, mutation
- **Best for**: Discrete optimization, no gradients
- **Example**: Genetic Algorithm

### Particle Swarm Optimization (PSO)
- **Inspired by**: Bird flocking, fish schooling
- **Method**: Particles explore search space
- **Best for**: Continuous optimization, fast convergence
- **Example**: Swarm finds best parameters

### Ant Colony Optimization (ACO)
- **Inspired by**: Ant foraging behavior
- **Method**: Pheromone trails guide search
- **Best for**: Combinatorial problems
- **Example**: Ants find optimal circuit parameters

### Simulated Annealing (SA)
- **Inspired by**: Metal annealing process
- **Method**: Probabilistic hill climbing with cooling
- **Best for**: Escaping local optima
- **Example**: Temperature-based parameter search

---

## EEG Data Format

### CSV Structure

```csv
timestamp,session_id,window_id,alpha,beta,theta,delta,frontal_asym,parietal_asym,hrv,engagement,label
1732038456.123,session_001,w_001,0.52,-0.31,0.78,0.11,-0.23,0.61,0.05,-0.42,Flow
1732038457.123,session_001,w_002,0.48,-0.28,0.81,0.09,-0.20,0.58,0.03,-0.39,Flow
1732038458.123,session_001,w_003,0.15,0.42,-0.62,0.31,0.87,-0.19,0.51,0.12,No_Flow
...
```

### Column Definitions

| Column | Type | Range | Meaning |
|--------|------|-------|---------|
| `timestamp` | float | Unix time | Recording time (seconds since epoch) |
| `session_id` | string | - | Unique session identifier |
| `window_id` | string | - | Unique window identifier |
| `alpha` | float | [-1, 1] | Alpha band power (8-13 Hz, relaxation) |
| `beta` | float | [-1, 1] | Beta band power (13-30 Hz, active thinking) |
| `theta` | float | [-1, 1] | Theta band power (4-8 Hz, deep focus) |
| `delta` | float | [-1, 1] | Delta band power (0.5-4 Hz, sleep) |
| `frontal_asym` | float | [-1, 1] | Frontal asymmetry (approach/withdrawal) |
| `parietal_asym` | float | [-1, 1] | Parietal asymmetry |
| `hrv` | float | [-1, 1] | Heart rate variability |
| `engagement` | float | [-1, 1] | Overall engagement level |
| `label` | string | - | Ground truth: "Flow" or "No_Flow" |

### How Features Are Normalized

From raw EEG to [-1, 1]:

```python
# Example: Alpha power
raw_alpha = compute_fft_power(eeg_signal, freq_range=(8, 13))  # μV²/Hz
log_alpha = log10(raw_alpha)  # Log transform
normalized = (log_alpha - mean) / std  # Z-score
clipped = clip(normalized, -3, 3) / 3  # Clip to [-1, 1]
```

---

## Dashboard UI Elements Explained

### Online Inference Panel

**What you're doing**: Simulating one 1-second brain activity window

**Inputs**:
- **Session ID**: Which recording session (like "Patient 001, Session 3")
- **Window ID**: Which second of data (like "second 42")
- **Features**: The 8 extracted EEG characteristics
- **Task Difficulty**: How hard the task was (0=easy sudoku, 1=expert programming)

**Outputs**:
- **CME**: Mental energy score (30-80 typical)
- **p_flow**: Quantum classifier's confidence (0.65 = 65% flow)
- **Shots**: How many times circuit was measured
- **QPU Latency**: Time spent in quantum hardware

### Training Jobs Panel

**What you're doing**: Optimizing the quantum circuit to classify better

**Inputs**:
- **Generations**: How many optimization iterations
- **Algorithm**: Which metaheuristic to use

**Process**:
1. Try different circuit parameter combinations
2. Test each on validation data
3. Keep the best ones
4. Repeat until convergence

**Outputs**:
- **Best Fitness**: Highest accuracy found (0.85 = 85% accurate)
- **QPU Calls**: Total quantum backend uses

---

## For Your Dissertation

### Petri Net Mapping

Your Petri net should model:

**Places** (states):
- `ClientReady`, `RequestSubmitted`, `WaitingForQPU`, `InQPU`, `ProcessingResult`, `ResponseReady`
- `TrainingQueued`, `TrainingRunning`, `TrainingCompleted`

**Transitions** (events):
- `SubmitRequest`, `StartQPU`, `FinishQPU`, `ComputeCME`, `StoreDB`, `SendResponse`
- `StartTraining`, `EvaluateCandidate`, `AdvanceGeneration`, `CompleteTraining`

**Tokens** (instances):
- Individual inference requests
- Training job tasks

**Performance Metrics**:
- Token residence times = latency
- Throughput = tokens per second
- Queue lengths at each place

### Validation Approach

1. **Run experiments** on this implementation:
   - Measure: latency distribution, throughput, queue lengths
   - Vary: load, QPU latency, training frequency

2. **Simulate Petri net** with same parameters:
   - Use stochastic Petri net tools (CPN Tools, PIPE, etc.)
   - Same arrival rates, service times

3. **Compare results**:
   - Do latencies match?
   - Does throughput align?
   - Are queue behaviors similar?

4. **Thesis contribution**:
   - Petri nets can model quantum ML systems
   - Performance predictions are accurate
   - Useful for capacity planning without building full system

---

## Summary

**This Implementation** = Working software  
**Your Petri Net** = Formal model  
**Dissertation** = Prove they're equivalent for performance analysis

This codebase gives you **ground truth data** to validate your Petri net model!
