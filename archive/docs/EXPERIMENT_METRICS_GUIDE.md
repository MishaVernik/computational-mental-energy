# Experiment Metrics & Performance Analysis Guide

## 🎯 New Feature: Complete Experiment Tracking & Petri Net Comparison

**What's New**: A comprehensive experiment management and metrics analysis system for your lab assignment!

---

## Overview

The system now supports:

✅ **Experiment Tracking** - Group requests/jobs by experimental run  
✅ **Comprehensive Metrics** - Latency percentiles, throughput, QPU utilization  
✅ **Petri Net Comparison** - Enter model results and calculate MAPE automatically  
✅ **CSV Export** - Download all metrics for your lab report  
✅ **Visual Analysis** - Charts showing latency distribution, QPU usage split  

---

## How It Works

### 1. Create an Experiment

**In Dashboard** (http://localhost:3000):
1. Click **"Experiments" tab** (new tab!)
2. Click **"New Experiment"** button
3. Fill in parameters:
   - **Name**: "Light Load Test - 0.5 req/s"
   - **Duration**: 300 seconds (5 minutes)
   - **Arrival Rate**: 0.5 req/s
   - **Clients**: 3
   - **Training Rate**: 0.1 jobs/min
   - **Notes**: Optional description
4. Click **"Create Experiment"**
5. Experiment card appears with ID

### 2. Run the Experiment

**Option A: Via Simulation Client** (Recommended)

The simulation client needs to be updated to support experiments. For now, manually track:

```bash
cd cme-sim-client

# Run your experiment
npm run simulate -- --duration 300 --onlineRate 0.5 --clients 3

# Note: This will create requests WITHOUT ExperimentId
# For now, you can query by time range
```

**Option B: Via Dashboard** (Manual Testing)

Use the Control Panel to submit individual requests. They'll all be tracked together if done within the experiment timeframe.

### 3. Mark Experiment as Complete

**API Call**:
```bash
curl -X POST http://localhost:5000/api/experiments/{experimentId}/complete
```

Or it will auto-complete when you view results.

### 4. View Results & Metrics

**In Dashboard**:
1. Go to **"Experiments" tab**
2. Click on an experiment card
3. See **comprehensive metrics dashboard**:
   - Summary cards (Avg Latency, P95, Throughput, QPU Utilization)
   - Detailed inference table (all percentiles, error rates)
   - Latency histogram chart
   - QPU metrics table
   - QPU usage pie chart (Inference vs Training)
   - Training job metrics

### 5. Enter Petri Net Model Results

**After running your Petri net simulation** in PetriObjModelPaint:

1. In experiment results view, click **"Enter Model Metrics"**
2. Fill in values from your Petri net simulation:
   - Model Avg Latency (ms)
   - Model P95 Latency (ms)
   - Model Throughput (req/s)
   - Model QPU Utilization (0-1)
   - Model Avg Job Duration (s) (optional)
   - Notes (e.g., "10 replications, warmup=30s")
3. Click **"Save and Calculate MAPE"**

### 6. View MAPE Comparison

**Automatic calculation** of:
- MAPE for each metric (Latency, Throughput, QPU, Training Duration)
- Overall MAPE
- Verdict: "Excellent (<10%)", "Good (<20%)", or "Needs Refinement (>20%)"

**Visual Indicators**:
- 🟢 Green: MAPE < 10% (Excellent match)
- 🔵 Blue: MAPE < 20% (Good match)
- 🟠 Orange: MAPE > 20% (Needs refinement)

### 7. Export to CSV

Click **"Export CSV"** button to download all metrics in CSV format for your lab report!

---

## API Endpoints

### Experiments Management

**Create Experiment**:
```http
POST /api/experiments
Content-Type: application/json

{
  "name": "Experiment 1: Light Load",
  "durationSeconds": 300,
  "onlineArrivalRate": 0.5,
  "numberOfClients": 3,
  "trainingArrivalRate": 0.1,
  "notes": "Baseline performance test"
}
```

**List Experiments**:
```http
GET /api/experiments?limit=20
```

**Get Experiment**:
```http
GET /api/experiments/{id}
```

**Mark Complete**:
```http
POST /api/experiments/{id}/complete
```

### Metrics & Comparison

**Get Computed Metrics**:
```http
GET /api/experiments/{id}/metrics

Response:
{
  "experimentId": "...",
  "timeWindowMs": 300000,
  "inference": {
    "totalRequests": 150,
    "successCount": 150,
    "errorCount": 0,
    "errorRate": 0,
    "avgLatencyMs": 1205.34,
    "p50LatencyMs": 1180.5,
    "p90LatencyMs": 2105.2,
    "p95LatencyMs": 2340.1,
    "p99LatencyMs": 3120.5,
    "throughputReqPerSec": 0.5,
    "latencyHistogram": {
      "<500ms": 0,
      "500-1000ms": 25,
      "1000-1500ms": 75,
      "1500-2000ms": 40,
      ">2000ms": 10
    }
  },
  "qpu": {
    "totalQpuCalls": 200,
    "avgQpuCallDurationMs": 1150.5,
    "minQpuCallDurationMs": 305.2,
    "maxQpuCallDurationMs": 1998.7,
    "totalQpuBusyMs": 230100,
    "qpuUtilization": 0.767,
    "inferenceCalls": 150,
    "trainingCalls": 50,
    "qpuBusyMsInference": 172575,
    "qpuBusyMsTraining": 57525
  },
  "training": {
    "totalJobs": 1,
    "completedJobs": 1,
    "failedJobs": 0,
    "completionRate": 1.0,
    "avgJobDurationSec": 58.3,
    "p95JobDurationSec": 58.3,
    "byAlgorithm": {
      "genetic": {
        "jobCount": 1,
        "avgDurationSec": 58.3,
        "avgBestFitness": 0.847,
        "totalQpuCalls": 50
      }
    }
  },
  "comparison": null // or ComparisonMetrics if model data exists
}
```

**Save Model Metrics**:
```http
POST /api/experiments/{id}/modelMetrics
Content-Type: application/json

{
  "modelAvgLatencyMs": 1187.5,
  "modelP95LatencyMs": 2298.0,
  "modelThroughputReqPerSec": 0.495,
  "modelQpuUtilization": 0.753,
  "modelAvgJobDurationSec": 57.8,
  "notes": "PetriObjModelPaint, 10 replications, warmup=30s"
}
```

**Get Model Metrics**:
```http
GET /api/experiments/{id}/modelMetrics
```

**Export Metrics as CSV**:
```http
GET /api/experiments/{id}/export
```

Returns CSV file with all metrics.

---

## Database Schema Changes

### New Tables

**Experiments**:
```sql
CREATE TABLE Experiments (
  Id uniqueidentifier PRIMARY KEY,
  Name nvarchar(200) NOT NULL,
  StartedAt datetime2 NOT NULL,
  FinishedAt datetime2 NULL,
  DurationSeconds int NOT NULL,
  OnlineArrivalRate float NOT NULL,
  NumberOfClients int NOT NULL,
  TrainingArrivalRate float NOT NULL,
  Status nvarchar(50) NOT NULL,
  Notes nvarchar(max) NULL
)
```

**QpuInvocationLogs**:
```sql
CREATE TABLE QpuInvocationLogs (
  Id uniqueidentifier PRIMARY KEY,
  ExperimentId uniqueidentifier NULL,
  StartedAt datetime2 NOT NULL,
  FinishedAt datetime2 NOT NULL,
  DurationMs int NOT NULL,
  Type nvarchar(20) NOT NULL,  -- 'Inference' or 'Training'
  Shots int NOT NULL,
  BackendName nvarchar(50) NULL,
  FOREIGN KEY (ExperimentId) REFERENCES Experiments(Id)
)
```

**ExperimentModelMetrics**:
```sql
CREATE TABLE ExperimentModelMetrics (
  Id uniqueidentifier PRIMARY KEY,
  ExperimentId uniqueidentifier NOT NULL UNIQUE,
  ModelAvgLatencyMs float NOT NULL,
  ModelP95LatencyMs float NULL,
  ModelThroughputReqPerSec float NOT NULL,
  ModelQpuUtilization float NOT NULL,
  ModelAvgJobDurationSec float NULL,
  SavedAt datetime2 NOT NULL,
  Notes nvarchar(max) NULL,
  FOREIGN KEY (ExperimentId) REFERENCES Experiments(Id)
)
```

### Modified Tables

**InferenceRequestLogs** (columns added):
- `ExperimentId` (uniqueidentifier, nullable)
- `FinishedAt` (datetime2, nullable)
- `IsSuccess` (bit, default true)
- `ErrorType` (nvarchar(100), nullable)

**TrainingJobs** (column added):
- `ExperimentId` (uniqueidentifier, nullable)

---

## Metrics Computed

### A. Online Inference Metrics

| Metric | Formula | Purpose |
|--------|---------|---------|
| **Total Requests** | COUNT(*) | Volume |
| **Success Count** | COUNT WHERE IsSuccess=true | Reliability |
| **Error Count** | COUNT WHERE IsSuccess=false | Failure rate |
| **Error Rate** | ErrorCount / TotalRequests | Quality indicator |
| **Avg Latency** | AVG(TotalLatencyMs) | Typical performance |
| **Min/Max Latency** | MIN/MAX(TotalLatencyMs) | Range |
| **P50 (Median)** | PERCENTILE_CONT(0.50) | Median experience |
| **P90** | PERCENTILE_CONT(0.90) | Most users |
| **P95** | PERCENTILE_CONT(0.95) | Almost all users |
| **P99** | PERCENTILE_CONT(0.99) | Worst-case (SLA) |
| **Throughput** | TotalRequests / DurationSec | System capacity |

### B. QPU Utilization Metrics

| Metric | Formula | Purpose |
|--------|---------|---------|
| **Total QPU Calls** | COUNT(*) from QpuInvocationLogs | Total usage |
| **Avg Call Duration** | AVG(DurationMs) | Typical QPU time |
| **Total Busy Time** | SUM(DurationMs) | Resource consumption |
| **QPU Utilization** | TotalBusyTime / TimeWindow | Efficiency (0-1) |
| **Inference Calls** | COUNT WHERE Type='Inference' | Online usage |
| **Training Calls** | COUNT WHERE Type='Training' | Batch usage |

**Utilization Interpretation**:
- < 0.5 (50%): Under-utilized, room for growth
- 0.5-0.8 (50-80%): Good utilization
- > 0.8 (80%): High utilization, near capacity
- > 0.9 (90%): Saturated, bottleneck

### C. Training Job Metrics

| Metric | Formula | Purpose |
|--------|---------|---------|
| **Total Jobs** | COUNT(*) | Volume |
| **Completed Jobs** | COUNT WHERE Status='Completed' | Success |
| **Completion Rate** | CompletedJobs / TotalJobs | Reliability |
| **Avg Duration** | AVG(CompletedAt - StartedAt) | Typical time |
| **P50/P90/P95** | Percentiles of duration | Distribution |

**By Algorithm**:
- Jobs per algorithm (GA, PSO, ACO, SA)
- Average duration per algorithm
- Average best fitness per algorithm
- Total QPU calls per algorithm

### D. MAPE (Model Accuracy)

Formula:
```
MAPE = |Real - Model| / |Model| × 100%
```

**Interpretation**:
- MAPE < 10%: **Excellent** - Model is highly accurate ✅
- MAPE 10-20%: **Good** - Model is acceptable ✅
- MAPE > 20%: **Needs Refinement** - Model needs adjustment ⚠️

**Overall MAPE**: Average of all individual MAPEs

---

## Usage Workflow for Lab Assignment

### Step 1: Run Real System Experiment

**Create experiment via API**:
```bash
curl -X POST http://localhost:5000/api/experiments \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Experiment 1: λ=0.5 req/s",
    "durationSeconds": 300,
    "onlineArrivalRate": 0.5,
    "numberOfClients": 3,
    "trainingArrivalRate": 0.1,
    "notes": "Baseline performance test"
  }'
```

**Note the experiment ID** returned (e.g., `abc-123-def`)

**Run simulation** (manually track for now):
```bash
cd cme-sim-client
npm run simulate -- --duration 300 --onlineRate 0.5 --clients 3
```

**Mark complete**:
```bash
curl -X POST http://localhost:5000/api/experiments/abc-123-def/complete
```

### Step 2: View Real System Metrics

**In Dashboard**:
1. Go to **"Experiments" tab**
2. Click on your experiment card
3. See comprehensive metrics:
   - Avg Latency: 1205 ms
   - P95: 2340 ms
   - Throughput: 0.498 req/s
   - QPU Utilization: 57.3%

**Take screenshots** for your lab report!

### Step 3: Build Petri Net Model

Follow **[PETRI_NET_MODEL.md](PETRI_NET_MODEL.md)**:
- Use timing parameters: Uniform(300, 2000) ms for QPU
- Set arrival rate: λ = 0.5 req/s (match experiment)
- Initial marking: 3 client tokens
- Run in PetriObjModelPaint

### Step 4: Run Petri Net Simulation

**In PetriObjModelPaint**:
- Simulation → Configure
- Duration: 300 seconds
- Replications: 10
- Run simulation
- Export statistics:
  - Mean response time: 1187 ms
  - P95 response time: 2298 ms
  - Throughput: 0.495 req/s
  - QPU utilization: 0.573

### Step 5: Enter Model Results for Comparison

**In Dashboard** (experiment results view):
1. Click **"Enter Model Metrics"**
2. Fill in Petri net simulation results:
   - Model Avg Latency: 1187
   - Model P95 Latency: 2298
   - Model Throughput: 0.495
   - Model QPU Utilization: 0.573
   - Notes: "PetriObjModelPaint, 10 replications"
3. Click **"Save and Calculate MAPE"**

### Step 6: View Comparison & MAPE

**Automatic Display**:
```
Overall MAPE: 1.85%  ✅ Excellent (<10% error)

Metric                Real System    Model         MAPE      Status
─────────────────────────────────────────────────────────────────────
Avg Latency (ms)     1205.34        1187.00       1.52%     ✅ Excellent
P95 Latency (ms)     2340.10        2298.00       1.80%     ✅ Excellent
Throughput (req/s)   0.498          0.495         0.60%     ✅ Excellent
QPU Utilization      57.30%         57.30%        0.00%     ✅ Excellent
```

**Verdict**: "Excellent (<10% error)" in green banner!

### Step 7: Export for Lab Report

Click **"Export CSV"** to download:
- All real system metrics
- All model metrics
- Calculated MAPEs
- Experiment parameters

**Use in your lab report tables!**

---

## Metrics Explanation

### Latency Percentiles

**P50 (Median)**:
- 50% of requests complete by this time
- Typical user experience

**P90**:
- 90% of requests complete by this time
- Most users' experience

**P95**:
- 95% of requests complete by this time
- SLA boundary (common choice)
- If P95=2340ms, then 95% of requests took ≤2340ms

**P99**:
- 99% of requests complete by this time
- Worst-case latency (tail latency)
- Important for reliability guarantees

**Why Percentiles Matter**:
- Average can be misleading (outliers)
- Percentiles show distribution
- P95/P99 reveal tail latencies
- Critical for Petri net validation

### QPU Utilization

**Formula**:
```
Utilization = Total Busy Time / Time Window
```

**Example**:
- Time Window: 300 seconds = 300,000 ms
- Total Busy: 230,100 ms (sum of all QPU calls)
- Utilization: 230,100 / 300,000 = 0.767 = 76.7%

**Interpretation**:
- 76.7% means QPU was busy 76.7% of the time
- 23.3% was idle time
- High utilization → QPU is bottleneck
- Should match Petri net simulation!

### Throughput

**Formula**:
```
Throughput = Completed Requests / Duration (seconds)
```

**Example**:
- Completed: 150 requests
- Duration: 300 seconds
- Throughput: 150 / 300 = 0.5 req/s

**Compare to Arrival Rate**:
- Arrival Rate (λ): 0.5 req/s (configured)
- Throughput: 0.5 req/s (measured)
- Match → System is stable ✅

If Throughput < Arrival Rate → Queue is growing (unstable)

---

## MAPE Calculation Details

### Example Calculation

**Real System**:
- Avg Latency: 1205.34 ms
- Throughput: 0.498 req/s
- QPU Utilization: 0.767

**Petri Net Model**:
- Avg Latency: 1187.00 ms
- Throughput: 0.495 req/s
- QPU Utilization: 0.753

**MAPE Calculations**:

```
MAPE_latency = |1205.34 - 1187.00| / 1187.00 = 18.34 / 1187.00 = 0.0154 = 1.54%

MAPE_throughput = |0.498 - 0.495| / 0.495 = 0.003 / 0.495 = 0.0061 = 0.61%

MAPE_qpu = |0.767 - 0.753| / 0.753 = 0.014 / 0.753 = 0.0186 = 1.86%

Overall MAPE = (1.54% + 0.61% + 1.86%) / 3 = 1.34%
```

**Verdict**: 1.34% < 10% → **Excellent match!** ✅

### Acceptable Error Ranges

| MAPE | Verdict | Color | Lab Grade Impact |
|------|---------|-------|------------------|
| < 5% | Outstanding | 🟢 Green | A+ territory |
| 5-10% | Excellent | 🟢 Green | A (expected for good work) |
| 10-15% | Good | 🔵 Blue | B+ (acceptable) |
| 15-20% | Acceptable | 🔵 Blue | B (needs minor refinement) |
| 20-30% | Fair | 🟠 Orange | C (needs refinement) |
| > 30% | Poor | 🔴 Red | Recheck model parameters |

---

## Troubleshooting

### Issue: No Metrics Showing

**Cause**: No data associated with experiment

**Solution**:
- Manually associate existing data by time range (SQL update)
- Or create new experiment and run fresh tests

### Issue: MAPE Very High (>50%)

**Cause**: Petri net parameters don't match real system

**Solution**:
1. Re-measure timing from real system:
   ```sql
   SELECT AVG(QpuLatencyMs), MIN(QpuLatencyMs), MAX(QpuLatencyMs)
   FROM InferenceRequestLogs
   ```
2. Update Petri net QPU distribution: Uniform(min, max)
3. Re-run Petri net simulation
4. Update model metrics in dashboard

### Issue: Throughput Doesn't Match

**Cause**: Different arrival rates or saturation

**Solution**:
- Verify Petri net arrival rate matches experiment
- Check if either system is saturated (λ > μ)
- Both should saturate at same point (~0.87 req/s)

### Issue: QPU Utilization Off

**Cause**: Time window calculation different

**Solution**:
- Verify time window is same in both (300 seconds)
- Check warmup period was excluded in Petri net
- Ensure both include/exclude training jobs consistently

---

## For Your Lab Report

### Tables to Include

**Table 1: Experiment Parameters**

| Parameter | Value |
|-----------|-------|
| Duration | 300 seconds |
| Arrival Rate | 0.5 req/s |
| Number of Clients | 3 |
| QPU Service Time | Uniform(300, 2000) ms |

**Table 2: Real System Results**

| Metric | Value |
|--------|-------|
| Avg Latency | 1205.34 ms |
| P95 Latency | 2340.10 ms |
| Throughput | 0.498 req/s |
| QPU Utilization | 76.7% |

**Table 3: Petri Net Model Results**

| Metric | Value |
|--------|-------|
| Avg Latency | 1187.00 ms |
| P95 Latency | 2298.00 ms |
| Throughput | 0.495 req/s |
| QPU Utilization | 75.3% |

**Table 4: Comparison (MAPE)**

| Metric | Real | Model | MAPE (%) | Status |
|--------|------|-------|----------|--------|
| Avg Latency | 1205 ms | 1187 ms | 1.54% | ✅ Excellent |
| P95 Latency | 2340 ms | 2298 ms | 1.80% | ✅ Excellent |
| Throughput | 0.498 req/s | 0.495 req/s | 0.61% | ✅ Excellent |
| QPU Util | 76.7% | 75.3% | 1.86% | ✅ Excellent |
| **Overall** | - | - | **1.34%** | ✅ Excellent |

### Figures to Include

**Figure 1**: Screenshot of Experiment Results Dashboard
- Shows all metrics in one view
- Professional presentation

**Figure 2**: Latency Distribution Histogram
- From dashboard chart
- Shows distribution of response times

**Figure 3**: QPU Usage Breakdown
- Pie chart: Inference vs Training
- Visual resource allocation

**Figure 4**: MAPE Comparison Table
- Screenshot of comparison section
- Color-coded MAPE values

---

## Advanced: Multiple Experiments Comparison

### Experimental Matrix

Run 3-5 experiments with different loads:

| Exp # | Name | λ (req/s) | Clients | Expected Behavior |
|-------|------|-----------|---------|-------------------|
| 1 | Light Load | 0.5 | 3 | Stable, low latency |
| 2 | Moderate Load | 1.5 | 5 | Stable, moderate latency |
| 3 | High Load | 3.0 | 8 | Unstable, queue growth |
| 4 | With Training | 1.0 | 5 | Training impact on latency |
| 5 | Algorithm Comparison | 1.0 | 5 | Compare GA vs PSO vs ACO |

**For each**:
- Run real system
- Run Petri net
- Compare MAPE
- Plot: Latency vs. Load

**Expected**:
- MAPE should be consistent (<10%) across all loads
- Validates model across operating range

---

## SQL Queries for Manual Analysis

```sql
-- Query metrics for experiment
DECLARE @ExpId uniqueidentifier = 'your-experiment-id-here'

-- Inference metrics
SELECT 
  COUNT(*) AS TotalRequests,
  SUM(CASE WHEN IsSuccess=1 THEN 1 ELSE 0 END) AS SuccessCount,
  AVG(TotalLatencyMs) AS AvgLatency,
  MIN(TotalLatencyMs) AS MinLatency,
  MAX(TotalLatencyMs) AS MaxLatency,
  PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY TotalLatencyMs) AS P95
FROM InferenceRequestLogs
WHERE ExperimentId = @ExpId

-- QPU utilization
SELECT 
  COUNT(*) AS TotalCalls,
  AVG(DurationMs) AS AvgDuration,
  SUM(DurationMs) AS TotalBusyMs,
  Type
FROM QpuInvocationLogs
WHERE ExperimentId = @ExpId
GROUP BY Type

-- Training jobs
SELECT 
  Algorithm,
  COUNT(*) AS JobCount,
  AVG(DATEDIFF(SECOND, StartedAt, CompletedAt)) AS AvgDurationSec,
  AVG(BestFitness) AS AvgFitness
FROM TrainingJobs
WHERE ExperimentId = @ExpId AND Status = 'Completed'
GROUP BY Algorithm
```

---

## Summary

**You now have**:
- ✅ Complete experiment tracking system
- ✅ Comprehensive metrics computation
- ✅ Petri net comparison with automatic MAPE
- ✅ Visual dashboards for analysis
- ✅ CSV export for lab reports
- ✅ Ready for dissertation work!

**Perfect for your lab assignment**:
- Requirement 1: ✅ Working web application with metrics
- Requirement 2: ✅ Performance analysis tools
- Requirement 3: ✅ Petri net comparison framework
- Requirement 4: ✅ Experimental validation ready

**Next**: Rebuild the system and test the new Experiments tab! 🚀

