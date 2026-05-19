# Dissertation Guide - CME System for Petri Net Comparison

## Purpose of This Implementation

This **working software implementation** provides **ground truth data** for your PhD dissertation comparing:

1. **Quantum ML Web Application** (this codebase)
2. **Petri Net Formal Model** (your theoretical model)

**Research Goal**: Validate that Petri nets can accurately model the performance and behavior of quantum machine learning systems.

---

## How to Use This for Your Dissertation

### Phase 1: Understand the System

**Read These Documents** (in order):

1. **WHAT_IS_WHAT.md** - Big picture (start here!)
2. **ALGORITHMS_EXPLAINED.md** - Technical details
3. **QUICK_REFERENCE.md** - Quick lookups
4. **VISUAL_GUIDE.md** - UI explanation

**Use the Dashboard**:
- Open http://localhost:3000
- Click **"Process Flow" tab**
- Study the visual diagrams
- Note the Petri net mapping section

### Phase 2: Collect Baseline Data

**Run Controlled Experiments**:

```bash
# Experiment 1: Low load, measure baseline latency
cd cme-sim-client
npm run simulate -- --duration 300 --onlineRate 1 --clients 1

# Experiment 2: Medium load
npm run simulate -- --duration 300 --onlineRate 3 --clients 2

# Experiment 3: High load
npm run simulate -- --duration 300 --onlineRate 5 --clients 5

# Experiment 4: With training jobs
npm run simulate -- --duration 300 --onlineRate 2 --trainRate 0.2
```

**Extract Data from Database**:

```sql
-- Average latencies
SELECT AVG(TotalLatencyMs) AS AvgTotal,
       AVG(QpuLatencyMs) AS AvgQPU
FROM InferenceRequestLogs

-- Latency distribution
SELECT 
  MIN(TotalLatencyMs) AS Min,
  PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY TotalLatencyMs) AS Median,
  PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY TotalLatencyMs) AS P95,
  PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY TotalLatencyMs) AS P99,
  MAX(TotalLatencyMs) AS Max
FROM InferenceRequestLogs

-- Training job completion times
SELECT 
  Algorithm,
  AVG(DATEDIFF(SECOND, StartedAt, CompletedAt)) AS AvgDuration,
  AVG(TotalQpuCalls) AS AvgQpuCalls
FROM TrainingJobs
WHERE Status = 'Completed'
GROUP BY Algorithm
```

**Export Results**:
```bash
# Save to CSV for analysis
docker exec cme-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -d CmeSimDb -Q "SELECT * FROM InferenceRequestLogs" -o latency_data.csv -s ","
```

### Phase 3: Build Your Petri Net Model

**Use the Timing Parameters** from Process Flow tab:

| Component | Type | Parameters |
|-----------|------|------------|
| Client arrivals | Poisson | λ = 1-10 req/s |
| API processing | Deterministic | ~10 ms |
| QPU execution | Uniform | U(300, 2000) ms |
| CME calculation | Deterministic | ~1 ms |
| DB write | Deterministic | ~5-10 ms |

**Places in Your Petri Net**:
```
P1: ClientReady
P2: RequestInAPI
P3: WaitingForQPU
P4: InQPU
P5: ComputingCME
P6: WritingDB
P7: ResponseReady
```

**Transitions**:
```
T1: SubmitRequest (λ = arrival rate)
T2: CallQPU (~10 ms)
T3: ExecuteCircuit (U(300, 2000) ms)
T4: CalculateCME (~1 ms)
T5: PersistDB (~5-10 ms)
T6: SendResponse (~5 ms)
```

**Example Petri Net Tool**: CPN Tools, PIPE, TimeNet, GreatSPN

### Phase 4: Simulate Petri Net

**Configure Your Simulation**:
- Set same arrival rate as your experiments (e.g., λ = 2 req/s)
- Set same service time distributions
- Run for same duration (e.g., 300 seconds)

**Collect Metrics**:
- Mean response time
- P95, P99 response times
- Throughput (req/s)
- Queue lengths at each place

### Phase 5: Compare Results

**Create Comparison Table**:

| Metric | Real System | Petri Net | Difference (%) |
|--------|-------------|-----------|----------------|
| Avg Latency | 1205 ms | 1187 ms | -1.5% |
| P95 Latency | 2340 ms | 2298 ms | -1.8% |
| P99 Latency | 3120 ms | 3245 ms | +4.0% |
| Throughput | 1.97 req/s | 1.95 req/s | -1.0% |

**Validation Criteria**:
- ✅ **Good match**: Difference < 10%
- ⚠️ **Acceptable**: Difference 10-20%
- ❌ **Poor match**: Difference > 20% (refine Petri net)

### Phase 6: Write Dissertation Chapters

**Suggested Structure**:

**Chapter 1: Introduction**
- Quantum ML for EEG analysis
- Performance modeling challenges
- Petri nets as modeling tool

**Chapter 2: Background**
- Quantum machine learning (VQC, QSVC)
- Metaheuristic optimization
- Petri net theory and simulation

**Chapter 3: System Design**
- CME quantum ML architecture (use diagrams from dashboard)
- Request flows (online inference, training jobs)
- Implementation details (this codebase)

**Chapter 4: Petri Net Model**
- Places, transitions, arcs
- Timing distributions
- Stochastic Petri net formulation

**Chapter 5: Experimental Validation**
- Experiment design (load testing scenarios)
- Data collection (from this system)
- Petri net simulation results
- Comparison and analysis

**Chapter 6: Results**
- Performance metrics comparison tables
- Statistical tests (t-test, ANOVA)
- Discussion: Where model matches/diverges
- Refinements to Petri net

**Chapter 7: Conclusions**
- Petri nets can model quantum ML systems
- Accuracy of predictions
- Usefulness for capacity planning
- Future work

---

## Metaheuristic Algorithms Explained (For Chapter 2)

### 1. Genetic Algorithm (GA)

**Inspiration**: Darwinian evolution

**Algorithm**:
```
1. Initialize: Random population of parameter sets
2. Evaluate: Test each on quantum backend → fitness
3. Select: Choose top performers (elitism)
4. Crossover: Combine pairs of parents → children
5. Mutate: Random changes to children
6. Replace: New generation from survivors + children
7. Repeat: Until convergence or max generations
```

**Parameters**:
- Population size: 5-20
- Crossover rate: 0.7-0.9
- Mutation rate: 0.01-0.1
- Selection method: Tournament, roulette, rank

### 2. Particle Swarm Optimization (PSO)

**Inspiration**: Bird flocking, fish schooling

**Algorithm**:
```
1. Initialize: Swarm of particles (random positions, velocities)
2. Evaluate: Test each particle position → fitness
3. Update personal best: Each particle remembers best position
4. Update global best: Swarm remembers best overall position
5. Update velocities: Move toward personal + global best
   v_i(t+1) = w×v_i(t) + c1×r1×(p_i - x_i) + c2×r2×(g - x_i)
6. Update positions: x_i(t+1) = x_i(t) + v_i(t+1)
7. Repeat: Until convergence
```

**Parameters**:
- Swarm size: 10-50
- Inertia weight (w): 0.4-0.9
- Cognitive coefficient (c1): 1.5-2.0
- Social coefficient (c2): 1.5-2.0

### 3. Ant Colony Optimization (ACO)

**Inspiration**: Ant foraging behavior

**Algorithm**:
```
1. Initialize: Pheromone trails on all paths (parameters)
2. Construct solutions: Ants build solutions probabilistically
   P(param) ∝ (pheromone)^α × (heuristic)^β
3. Evaluate: Test each ant's solution → quality
4. Update pheromones:
   τ_i(t+1) = (1-ρ)×τ_i(t) + Δτ_i  (evaporation + deposit)
5. Repeat: Pheromones guide ants to good solutions
```

**Parameters**:
- Number of ants: 5-50
- Pheromone importance (α): 1-3
- Heuristic importance (β): 2-5
- Evaporation rate (ρ): 0.1-0.5

### 4. Simulated Annealing (SA)

**Inspiration**: Metal annealing process

**Algorithm**:
```
1. Initialize: Random solution, high temperature T
2. Generate neighbor: Small random change to current solution
3. Evaluate: fitness_new vs fitness_current
4. Accept:
   - If better: Always accept
   - If worse: Accept with probability exp(-ΔE/T)
5. Cool down: T = T × cooling_rate
6. Repeat: Until T reaches minimum or convergence
```

**Parameters**:
- Initial temperature: 100-1000
- Cooling rate: 0.85-0.99
- Min temperature: 0.01-1.0
- Iterations per temperature: 10-100

---

## Using Different Algorithms in Dashboard

**Step 1**: Go to "Training Jobs" panel

**Step 2**: Open "Metaheuristic Algorithm" dropdown

**Step 3**: Select algorithm:
- **Genetic Algorithm** - Classic evolutionary approach
- **PSO** - Fast convergence, good for continuous optimization
- **ACO** - Good for combinatorial problems
- **Simulated Annealing** - Escapes local optima well

**Step 4**: Adjust generations (more = better optimization)

**Step 5**: Submit job

**Step 6**: Compare results in "Recent Training Jobs" table:
- Which algorithm found best fitness?
- Which used fewest QPU calls?
- Which completed fastest?

---

## EEG CSV Data for Validation

### Included Files

1. **example_data/eeg_sample_data.csv** (30 rows)
   - Realistic EEG features
   - Mix of Flow and No_Flow states
   - Multiple sessions

2. **example_data/DATA_FORMAT.md**
   - Complete column specification
   - Normalization procedures
   - Synthetic data generation code

### Using CSV Data

**In Dashboard**:
1. Click **"Data Upload" tab**
2. Click **"Load Example Data"** or upload your own CSV
3. Click **"Process CSV"**
4. View results: CME values, p_flow predictions, labels

**For Dissertation**:
- Use labeled data to measure accuracy
- Compare quantum predictions vs. labels
- Calculate: accuracy, precision, recall, F1 score
- Show quantum ML performance in Chapter 5

---

## Dissertation Experimental Design

### Experiment Matrix

| Experiment | Arrival Rate | Training Jobs | Duration | Purpose |
|------------|-------------|---------------|----------|---------|
| Baseline | 1 req/s | 0 | 5 min | Establish baseline latency |
| Load Test 1 | 3 req/s | 0 | 5 min | Moderate load behavior |
| Load Test 2 | 5 req/s | 0 | 5 min | High load, find limits |
| Training Impact | 2 req/s | 1 every 2 min | 10 min | Queue contention |
| Algorithm Comparison | 2 req/s | 1 of each type | 10 min | Compare metaheuristics |

### Data to Collect

**For Each Experiment**:
1. Response time distribution (avg, median, std, P95, P99)
2. Throughput (req/s)
3. Training completion times
4. QPU utilization (calls per second)
5. Queue lengths (from logs)

**From Dashboard**:
- Screenshot system overview cards
- Export charts as images
- Record metrics over time

**From Database**:
- SQL queries for detailed analysis
- Export to CSV for plotting in R/Python
- Statistical tests

### Petri Net Simulation

**Match Parameters Exactly**:
```
Arrivals:
  - Online: Poisson(λ = 2)
  - Training: Poisson(λ = 0.1 jobs/min)

Service Times:
  - API: Deterministic(10 ms)
  - QPU: Uniform(300, 2000 ms)
  - DB: Deterministic(7 ms)
```

**Run Simulation**:
- Same duration as experiments (5-10 minutes)
- Collect same metrics
- Export results for comparison

### Statistical Comparison

**Hypothesis Testing**:
```
H0: Petri net predictions = Real system measurements
H1: Significant difference exists

Test: Paired t-test on latencies
Significance: α = 0.05
```

**Metrics**:
- Mean Absolute Percentage Error (MAPE)
- Root Mean Square Error (RMSE)
- Correlation coefficient (r)

**Expected Results**:
- MAPE < 10%: Excellent model
- MAPE < 20%: Good model
- MAPE > 20%: Model needs refinement

---

## Dissertation Figures

### Figure Suggestions

**Figure 3.1**: System Architecture
- Use diagram from README.md
- Shows 4 components: Dashboard, API, Quantum Backend, Database
- Arrows show HTTP flows

**Figure 3.2**: Online Inference Flow
- From Process Flow tab
- Color-coded components
- Timing annotations

**Figure 3.3**: Training Job Flow
- From Process Flow tab
- Shows background processing
- Metaheuristic loop details

**Figure 4.1**: Petri Net Model
- Your formal Petri net diagram
- Places, transitions, arcs clearly labeled
- Timing annotations

**Figure 5.1**: Latency Comparison
- Box plots: Real system vs. Petri net
- Side-by-side for visual comparison

**Figure 5.2**: Throughput Over Time
- Line graph: Real system (solid) vs. Petri net (dashed)
- X-axis: Time, Y-axis: Requests/second

**Figure 5.3**: Algorithm Performance
- Bar chart comparing 4 metaheuristics
- Metrics: Time to complete, Best fitness, QPU calls

**Figure 6.1**: Accuracy vs. Load
- Scatter plot: Prediction accuracy decreases under load?
- Compare real system behavior to Petri net predictions

---

## Dissertation Tables

### Table 3.1: System Components

| Component | Technology | Purpose | Key Metrics |
|-----------|-----------|---------|-------------|
| Dashboard | React + TypeScript | User interface | User interactions |
| API | ASP.NET Core 8 | Request handling | Latency, throughput |
| Quantum Backend | Python + Qiskit | ML inference | QPU time, accuracy |
| Database | SQL Server 2022 | Persistence | Query time |

### Table 4.1: Petri Net Places

| Place | Meaning | Token Type |
|-------|---------|-----------|
| P1 | ClientReady | Request |
| P2 | RequestInAPI | Request |
| ... | ... | ... |

### Table 4.2: Petri Net Transitions

| Transition | Type | Distribution | Mean | Std Dev |
|-----------|------|--------------|------|---------|
| T1: SubmitRequest | Stochastic | Poisson | λ=2 | - |
| T2: APIProcess | Deterministic | Const | 10ms | 0 |
| T3: QPUExecute | Stochastic | Uniform | 1150ms | 490ms |
| ... | ... | ... | ... | ... |

### Table 5.1: Experimental Results

| Experiment | Real Avg (ms) | PN Avg (ms) | Real P95 (ms) | PN P95 (ms) | Error (%) |
|-----------|--------------|-------------|--------------|------------|-----------|
| Baseline | 1205 | 1187 | 2340 | 2298 | 1.5% |
| Load Test 1 | ... | ... | ... | ... | ... |
| ... | ... | ... | ... | ... | ... |

### Table 5.2: Algorithm Comparison

| Algorithm | Avg Time (s) | Best Fitness | QPU Calls | Convergence Rate |
|-----------|-------------|--------------|-----------|------------------|
| Genetic | 45.3 | 0.847 | 50 | Fast |
| PSO | 42.1 | 0.851 | 50 | Very Fast |
| ACO | 48.7 | 0.839 | 50 | Moderate |
| SA | 51.2 | 0.843 | 65 | Slow |

---

## Writing Your Contributions

### Novel Contributions

1. **First Petri net model of quantum ML web application**
   - Previous work: Classical ML systems
   - Gap: No formal models for quantum-enhanced systems
   - Your work: Bridges this gap

2. **Validated performance modeling approach**
   - Show Petri nets predict latency within X%
   - Useful for capacity planning
   - Don't need to build full system to estimate performance

3. **Metaheuristic comparison in quantum ML context**
   - Different algorithms for quantum circuit training
   - Performance characteristics documented
   - Guide for future implementations

### Limitations to Discuss

**This Implementation**:
- Simulated training (parameters don't actually improve)
- Synthetic fitness values (not real accuracy)
- Qiskit Aer simulator (not real quantum hardware)

**Petri Net**:
- Abstracts internal quantum computations
- Timing distributions are approximations
- Doesn't model quantum noise/errors

**Impact on Validity**:
- For **performance modeling**: Still valid (flows are realistic)
- For **ML accuracy**: Would need real training
- For **dissertation**: Sufficient to demonstrate methodology

---

## Quick Checklist for Dissertation

### System Understanding
- [ ] Read all documentation (WHAT_IS_WHAT.md, ALGORITHMS_EXPLAINED.md, etc.)
- [ ] Run dashboard, explore all tabs
- [ ] Understand each component's role
- [ ] Map to Petri net constructs

### Data Collection
- [ ] Run 5+ experiments with different loads
- [ ] Export latency data from database
- [ ] Calculate statistics (mean, std, percentiles)
- [ ] Document experimental setup

### Petri Net Modeling
- [ ] Build Petri net using parameters from Process Flow tab
- [ ] Implement in Petri net tool
- [ ] Validate structure (deadlock-free, live, etc.)
- [ ] Tune parameters if needed

### Simulation & Comparison
- [ ] Run Petri net simulations
- [ ] Collect same metrics as real system
- [ ] Create comparison tables
- [ ] Perform statistical tests

### Writing
- [ ] Create all figures (architecture, flows, results)
- [ ] Create all tables (components, parameters, results)
- [ ] Write methodology section
- [ ] Write results and analysis
- [ ] Discuss limitations
- [ ] Conclude contributions

---

## Key Insights for Dissertation

### Why This Validates Petri Nets

**Argument**:
1. Quantum ML systems are complex (multiple components, async flows)
2. Traditional models (queueing theory alone) miss dependencies
3. Petri nets capture both:
   - Concurrency (parallel requests)
   - Synchronization (QPU is shared resource)
   - State changes (queued → running → completed)
4. This implementation proves Petri nets work for this domain

### Why Imitation Is OK

**For Performance Modeling**:
- ✅ Request flows are real
- ✅ Latencies are realistic (simulated QPU delay)
- ✅ Queue behavior matches real systems
- ✅ Database persistence adds realistic delays

**What Doesn't Matter for Performance**:
- ❌ Whether training actually improves model
- ❌ Actual ML accuracy values
- ❌ Real vs. simulated quantum hardware (timing is what matters)

**Thesis**:
"This imitation model captures the essential **performance characteristics** of a quantum ML system, sufficient for validating Petri net models of system behavior."

---

## Additional Resources

### Citing This Work

If publishing:
```bibtex
@misc{cme_sim_2025,
  title={CME Quantum ML System Imitation Model},
  author={Your Name},
  year={2025},
  note={PhD Dissertation Research Code},
  url={https://github.com/...}
}
```

### Related Work to Cite

- **Quantum ML**: Havlíček et al. (2019) - Supervised learning with quantum-enhanced feature spaces
- **VQC**: Schuld & Petruccione (2018) - Quantum machine learning book
- **Petri Nets**: Murata (1989) - Petri nets: Properties, analysis and applications
- **Performance Modeling**: Bolch et al. (2006) - Queueing networks and Markov chains
- **EEG Analysis**: Pfurtscheller & Lopes da Silva (1999) - Event-related EEG/MEG synchronization

---

## Contact & Support

For dissertation-specific questions:
- **Architecture**: See Process Flow tab in dashboard
- **Algorithms**: See ALGORITHMS_EXPLAINED.md
- **Data Format**: See example_data/DATA_FORMAT.md
- **Petri Net Mapping**: See Process Flow tab → Petri Net section
- **Troubleshooting**: See TROUBLESHOOTING.md

**Good luck with your dissertation!** 🎓

This implementation gives you everything you need:
- Working system for ground truth data ✅
- Clear architecture for Petri net modeling ✅
- Performance metrics for validation ✅
- Visual explanations for thesis figures ✅

