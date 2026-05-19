# 📊 Metrics Explained Simply - Where Everything Comes From

## 🎯 Your Question: "Where do these metrics come from?"

**Simple Answer**: They all come from the **database**, automatically logged and computed by the system!

---

## 📍 **Exact Source of Each Metric**

### **1. Average Response Time**

**What it is**: How long a typical request takes (in milliseconds)

**Where it comes from**:
```
Database Table: InferenceRequestLogs
Column: TotalLatencyMs
Calculation: AVG(TotalLatencyMs)
```

**How it's measured**:
```csharp
// InferenceController.cs
var stopwatch = Stopwatch.StartNew();
// ... process request ...
// ... call quantum backend ...
// ... compute CME ...
stopwatch.Stop();

// Save to database
log.TotalLatencyMs = (int)stopwatch.ElapsedMilliseconds;  ← Stored here!
```

**In dashboard**: Shows in summary card and detailed table

**For Petri net**: Model the total time from T0 (Submit) to T6 (Response)

---

### **2. P95 / P99 Latencies (Tail Behavior)**

**What it is**: 
- P95: 95% of requests complete within this time
- P99: 99% of requests complete within this time

**Where it comes from**:
```
Database Table: InferenceRequestLogs
Column: TotalLatencyMs
Calculation: PERCENTILE_CONT(0.95) and PERCENTILE_CONT(0.99)
```

**How it's computed**:
```csharp
// ExperimentMetricsService.cs
var latencies = requests.Select(r => r.TotalLatencyMs).OrderBy(l => l).ToList();
var p95 = Percentile(latencies, 0.95);  // Find value at 95% position
var p99 = Percentile(latencies, 0.99);  // Find value at 99% position
```

**Example**:
- 100 requests with latencies: [1100, 1150, 1200, ..., 2300, 3100]
- Sort them
- P95 = value at position 95 → 2300ms
- Meaning: 95 out of 100 requests took ≤ 2300ms

**In dashboard**: Shows in summary card and detailed metrics table

**For Petri net**: Measure response time distribution, extract 95th and 99th percentile

---

### **3. Throughput Under Load**

**What it is**: How many requests per second the system can handle

**Where it comes from**:
```
Database Table: InferenceRequestLogs
Calculation: COUNT(*) / Experiment Duration (in seconds)
```

**How it's computed**:
```csharp
// ExperimentMetricsService.cs
var totalRequests = requests.Count;
var duration = (experiment.FinishedAt - experiment.StartedAt).TotalSeconds;
var throughput = totalRequests / duration;  // req/s
```

**Example**:
- Experiment duration: 300 seconds
- Total requests completed: 246
- Throughput = 246 / 300 = **0.82 req/s**

**Comparison with arrival rate**:
- If λ (arrival) = 1.0 req/s
- If throughput = 0.82 req/s
- Then 0.82 < 1.0 → Queue is growing! (System saturated)

**In dashboard**: Shows in summary card and inference metrics table

**For Petri net**: Count tokens exiting the system / simulation time

---

### **4. Queue Lengths (Implicit)**

**What it is**: How many requests are waiting for QPU

**Why "implicit"**: 
- We don't have a separate "Queue" entity
- But we can infer queueing from other metrics!

**How to detect queuing**:

**Method 1: P95/P50 Ratio**
```
If P95 >> P50 → High variance → Queueing is happening
Example:
  P50 = 1200 ms
  P95 = 3100 ms
  Ratio = 3100/1200 = 2.58 → Significant queueing!
```

**Method 2: Throughput vs Arrival Rate**
```
If Throughput < Arrival Rate → Requests are backing up
Example:
  λ = 2.0 req/s (arrivals)
  Throughput = 0.85 req/s (completions)
  Gap = 2.0 - 0.85 = 1.15 req/s piling up → Queue growing!
```

**Method 3: Latency Over Time**
```
If latency increases over experiment duration → Queue is growing
Plot: Latency vs Time
  If slope > 0 → Unstable system
```

**In Petri net**:
- **Direct measurement**: Count tokens in P2 (WaitingForQPU) place
- Should show same pattern as real system!

**For comparison**:
- Real: High P95/P50 ratio
- Petri net: High average queue length
- Both indicate the same thing: queueing behavior

---

## 📈 **Complete Example Experiment**

### **Setup**:
```
Name: "Moderate Load Test"
Duration: 300 seconds
Arrival Rate: 1.5 req/s
Clients: 5
```

### **Run Real System**:
```bash
npm run simulate -- --duration 300 --onlineRate 1.5 --clients 5
```

### **Metrics Automatically Computed**:

**From InferenceRequestLogs**:
```
Total Requests: 246 (from COUNT(*))
Avg Latency: 1456 ms (from AVG(TotalLatencyMs))
P50: 1380 ms (from PERCENTILE_CONT(0.50))
P95: 2680 ms (from PERCENTILE_CONT(0.95))
P99: 3245 ms (from PERCENTILE_CONT(0.99))
Throughput: 246/300 = 0.82 req/s
```

**From QpuInvocationLogs**:
```
Total QPU Calls: 296 (inference + training)
Total Busy Time: 283,200 ms (from SUM(DurationMs))
Time Window: 300,000 ms
QPU Utilization: 283,200 / 300,000 = 0.944 = 94.4%
```

**Queue Inference**:
```
P95/P50 Ratio: 2680 / 1380 = 1.94
→ Moderate queueing (some requests wait)

Throughput: 0.82 req/s
Arrival Rate: 1.5 req/s
→ System is saturated (can't keep up)
```

### **Build Petri Net**:
```
T0: Poisson(λ=1.5)  ← Match arrival rate!
T3: Uniform(300, 2000)  ← QPU time
P0: 5 tokens  ← Match clients
Duration: 300s  ← Match experiment
```

### **Run Petri Net Simulation**:
```
(PetriObjModelPaint outputs)
Mean Response Time: 1423 ms
P95 Response Time: 2635 ms
Throughput: 0.81 req/s
QPU Utilization: 92.8%
Mean Queue Length: 1.8 requests
```

### **Compare in Dashboard**:

**Enter model values** →  **Auto-calc MAPE**:
```
Avg Latency: |1456 - 1423| / 1423 = 2.32% ✅
P95 Latency: |2680 - 2635| / 2635 = 1.71% ✅
Throughput: |0.82 - 0.81| / 0.81 = 1.23% ✅
QPU Util: |94.4 - 92.8| / 92.8 = 1.72% ✅

Overall MAPE: 1.75% ✅ EXCELLENT!
```

**Verdict**: Model validated! Petri net accurately predicts real system!

---

## 🎓 **For Lab Report: Metric Source Table**

| Metric | Data Source | Computation | Logged When | Available Where |
|--------|-------------|-------------|-------------|-----------------|
| **Avg Latency** | InferenceRequestLogs.TotalLatencyMs | AVG() | Every inference request | Dashboard Experiments tab |
| **P95 Latency** | InferenceRequestLogs.TotalLatencyMs | PERCENTILE_CONT(0.95) | Every inference request | Dashboard Experiments tab |
| **P99 Latency** | InferenceRequestLogs.TotalLatencyMs | PERCENTILE_CONT(0.99) | Every inference request | Dashboard Experiments tab |
| **Throughput** | InferenceRequestLogs (count) | COUNT(*) / Duration | Every inference request | Dashboard Experiments tab |
| **QPU Utilization** | QpuInvocationLogs.DurationMs | SUM(DurationMs) / TimeWindow | Every QPU call | Dashboard Experiments tab |
| **Queue Length** | Derived from latency variance | P95/P50 ratio analysis | Implicit | Derived metric |
| **Training Duration** | TrainingJobs.StartedAt/CompletedAt | CompletedAt - StartedAt | Job completion | Dashboard Experiments tab |

**ALL AUTOMATIC!** No manual calculation needed!

---

## ✅ **Quick Verification**

### **Test That It's Working**:

**1. Check Dashboard**:
```
http://localhost:3000
```

**2. Look for banner** (top of Control Panel):
- If you see 🟡 "Using Default Parameters" → No training yet
- If you see 🟢 "Active Trained Model" → Training worked!

**3. Start a training job**:
- Click "Start Training Job"
- Wait ~60 seconds
- Banner should change to green: "Active Trained Model"!

**4. Submit inference**:
- Click "Compute CME"
- Check API logs: Should say "Using trained model from job {id}"

**5. Go to Experiments tab**:
- Create experiment
- View metrics (all auto-computed!)

**Everything working?** ✅ **System is ready for your lab!**

---

## 🆘 **If Metrics Show Zero / Empty**

### **Cause**: No data for that experiment yet

**Solution 1**: Generate some data
```bash
# Submit requests via dashboard, or
npm run simulate -- --duration 60 --onlineRate 1
```

**Solution 2**: Query all data (not filtered by experiment)
```
http://localhost:5000/api/dashboard/summary
```

This shows aggregate metrics across all requests

---

## 📝 **Final Summary**

**You asked**: "Where do metrics come from? Not clear how to compute them."

**Answer**:

✅ **Average Response Time**: From `InferenceRequestLogs`, computed as `AVG(TotalLatencyMs)`  
✅ **P95/P99 Latencies**: From `InferenceRequestLogs`, using percentile functions  
✅ **Throughput**: Count of requests / experiment duration  
✅ **Queue Lengths**: Inferred from P95/P50 ratio and throughput < arrival rate  
✅ **QPU Utilization**: From `QpuInvocationLogs`, SUM(durations) / time window  

**All visible in**:
- Dashboard → Experiments Tab → Click any experiment
- Automatic computation
- Professional charts and tables
- CSV export available

**All connected**:
- ✅ Training saves parameters
- ✅ Inference uses trained model
- ✅ Metrics tracked in database
- ✅ Comparison with Petri net built-in

**NOW IT'S A REAL SYSTEM!** 🚀

