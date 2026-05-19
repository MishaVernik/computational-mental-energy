# Visual Guide - Dashboard UI Explained

Complete walkthrough of every element in the CME Dashboard.

---

## Dashboard Tabs

The dashboard has **4 tabs** at the top:

### 1. ⚡ Control Panel (Default View)
- Submit inference requests
- Start training jobs
- View live system status

### 2. 📊 Process Flow
- Visual diagram of system architecture
- Request flow visualization
- Petri net mapping for dissertation

### 3. 📁 Data Upload
- Upload CSV files with EEG data
- Batch process multiple windows
- Download example data

### 4. 📈 Analytics
- Performance charts and visualizations
- Training job history table
- Detailed metrics

---

## Control Panel Tab Breakdown

### Top Section: System Overview Cards

**Card 1: Total Requests**
- 📊 Icon: Activity monitor
- **Shows**: Number of inference requests processed
- **Updates**: Every 5 seconds
- **Meaning**: System throughput indicator

**Card 2: Active Sessions**
- 👥 Icon: Server
- **Shows**: Number of EEG recording sessions
- **Updates**: Every 5 seconds
- **Meaning**: Concurrent users/experiments

**Card 3: Avg Response Time**
- ⚡ Icon: CPU
- **Shows**: Mean latency in milliseconds
- **Updates**: Every 5 seconds
- **Meaning**: System performance (lower = faster)

**Card 4: Training Jobs**
- 🔧 Icon: Database
- **Shows**: Total training jobs (all statuses)
- **Updates**: Every 5 seconds
- **Meaning**: Background workload

**Performance Metrics Row**:
- **Average CME**: Mean mental energy across all computations
- **P95 Latency**: 95% of requests complete within this time
- **P99 Latency**: 99% of requests complete within this time

### Blue Info Panel: "What's Being Computed & Optimized?"

**Click to expand/collapse** - Shows:
1. **Quantum Circuit**: VQC architecture (4 qubits, 3 layers)
2. **Metaheuristic**: Evolutionary algorithm details
3. **CME Formula**: Mathematical formula explained
4. **Imitation Note**: Clarification that this is a simulation

### Left Panel: Online Inference

**Purpose**: Submit a single EEG window for real-time CME computation

**Form Fields**:

1. **Session ID**
   - **What**: EEG recording session identifier
   - **Example**: `11111111-1111-1111-1111-111111111111`
   - **Hint**: Pre-filled with valid session from database

2. **Window ID**
   - **What**: Unique identifier for this 1-second segment
   - **Example**: `window-1732038456789`
   - **Hint**: Auto-generated with timestamp (auto-updates after each submission)

3. **Task Difficulty** (slider)
   - **Range**: 0.0 (very easy) to 1.0 (very difficult)
   - **What**: Subjective or measured task complexity
   - **Hint**: Shows below: "0.0 = Very Easy | 0.5 = Moderate | 1.0 = Very Difficult"
   - **Example**: 0.7 for challenging programming task

4. **EEG Features** (text area)
   - **Format**: JSON array of 8 floats
   - **Range**: Each value in [-1, 1]
   - **What**: Normalized EEG characteristics
   - **Dimensions**:
     1. Alpha power (relaxation, flow)
     2. Beta power (active thinking)
     3. Theta power (deep focus)
     4. Delta power (deep processing)
     5. Frontal asymmetry (approach/withdrawal)
     6. Parietal asymmetry (spatial processing)
     7. HRV (stress indicator)
     8. Engagement (cognitive load)
   - **Example**: `[0.5, -0.3, 0.8, 0.1, -0.2, 0.6, 0.0, -0.4]`

**Button**: "Compute CME" (green)
- **Action**: Sends request to API → Quantum Backend → Returns result
- **Time**: ~1-2 seconds
- **Result**: Displayed in green box below

**Results Display** (appears after submission):
- ⭐ **CME** (gold, large): The computed mental energy value
- **Flow Probability**: Quantum classifier output (%)
- **Shots Used**: Number of quantum measurements (1024)
- **Circuit Depth**: Quantum circuit complexity (8-12)
- **QPU Latency**: Time spent in quantum backend (ms)
- **Total Latency**: End-to-end response time (ms)

### Right Panel: Training Jobs

**Purpose**: Start a metaheuristic optimization job to train the quantum model

**Form Fields**:

1. **Metaheuristic Algorithm** (dropdown)
   - **Options**:
     - **Genetic Algorithm**: Evolution-based (selection, crossover, mutation)
     - **Particle Swarm Optimization**: Swarm intelligence
     - **Ant Colony Optimization**: Pheromone-guided search
     - **Simulated Annealing**: Temperature-based probabilistic search
   - **Description**: Shows below dropdown explaining chosen algorithm

2. **Total Generations** (slider)
   - **Range**: 5 to 50 (step: 5)
   - **What**: Number of optimization iterations
   - **Hint**: Explains what's being optimized (8 circuit parameters)
   - **QPU Impact**: Generations × 5 candidates = total quantum calls

**Button**: "Start Training Job" (blue)
- **Action**: Creates job in database → Background worker processes it
- **Time**: 30-120 seconds depending on generations
- **Result**: Job details shown in green box below

**Job Details Display** (appears after submission):
- **Job ID**: GUID of created job
- **Status**: Badge showing "Queued" initially
- **Total Generations**: Configured value
- **Created**: Timestamp
- **Hint**: Explains background worker will process automatically

### Bottom Section: Performance Metrics

**Charts** (2 side-by-side):

1. **Response Time Distribution**
   - **Type**: Bar chart
   - **X-axis**: Average, P95, P99
   - **Y-axis**: Milliseconds
   - **Color**: Blue bars
   - **Meaning**: Latency distribution (helps identify tail latencies)

2. **Training Jobs by Status**
   - **Type**: Bar chart
   - **X-axis**: Status (Queued, Running, Completed, Failed)
   - **Y-axis**: Count
   - **Color**: Green bars
   - **Meaning**: Current job distribution

**Summary Stats** (bottom row):
- Total Requests Processed
- Average CME Value
- Estimated Throughput (req/s)

### Bottom Section: Recent Training Jobs

**Purpose**: Live monitoring of training job progress

**Auto-refresh**: Every 10 seconds

**Table Columns**:

1. **Job ID**: First 8 characters of GUID + "..."
2. **Algorithm**: Badge showing algorithm type
3. **Status**: Color-coded badge
   - 🟢 Green (Completed): Job finished successfully
   - 🔵 Blue (Running): Currently processing
   - 🟡 Yellow (Queued): Waiting to start
   - 🔴 Red (Failed): Error occurred
4. **Progress**: Bar showing completed/total generations
   - Blue fill animates as job progresses
   - Text overlay: "5/10" = 5 of 10 generations done
5. **Duration**: Time elapsed since job started (seconds)
6. **QPU Calls**: Total quantum backend invocations
7. **Best Fitness**: Highest accuracy achieved (0-1 scale)

**Hover Effects**: Row highlights on mouse over

---

## Process Flow Tab

### Online Inference Path Diagram

Visual flow showing:
1. **Client/Dashboard** (purple) → HTTP POST
2. **ASP.NET Core API** (blue) → Validate, log
3. **Quantum Backend** (orange) → Circuit details shown:
   - Encode features → Ry rotations
   - Entangle → CX gates
   - Apply trained params → Ry, Rz
   - Measure 1024 shots
4. **CME Calculation** (blue) → Formula shown
5. **SQL Server** (green) → Store results
6. **Response** (purple) → Return to client

**Timing**: "1200-2000 ms typical, Bottleneck: Quantum Backend"

### Training Job Path Diagram

Visual flow showing:
1. **Client** → Submit job
2. **TrainingController** → Create record (Queued)
3. **SQL Server** → Store job
4. **Background Polling** (divider)
5. **TrainingWorkerService** (pink) → Detect and mark Running
6. **Metaheuristic Loop** (dashed pink box) → Detailed steps:
   - For each generation (10)
   - Generate 5 candidates
   - Call quantum backend for each
   - Compute fitness, track best
   - Sleep, update DB
7. **Complete** (pink) → Mark Completed, save results

**Timing**: "30-120 seconds, QPU Calls: Generations × Candidates = 50"

### Quantum Circuit Details

**ASCII art** showing:
- 4-qubit circuit diagram
- Layer-by-layer breakdown
- Parameter annotations
- Current "trained" values

### Petri Net Mapping (For Dissertation!)

**Places** (states):
- `ClientReady`, `RequestInAPI`, `WaitingForQPU`, `InQPU`, etc.

**Transitions** (events):
- `SubmitRequest`, `CallQPU`, `StartQuantumExec`, etc.

**Timing Table**:
- Distribution types (Poisson, Uniform, Deterministic)
- Parameters for each transition
- Use these values in your Petri net model!

**Validation Strategy** (5 steps):
1. Collect data from this system
2. Configure Petri net with same parameters
3. Simulate Petri net
4. Compare metrics
5. Thesis conclusion

---

## Data Upload Tab

### CSV Upload Section

**Three Ways to Load Data**:

1. **Upload File**:
   - Drag & drop or click "Upload CSV File"
   - Reads file into text area below
   - Shows filename when loaded

2. **Load Example**:
   - Click "Load Example Data" button
   - Populates with 3 sample rows
   - Good for quick testing

3. **Paste Directly**:
   - Paste CSV data into text area
   - Manual entry or copy from spreadsheet

**CSV Text Area**:
- Monospace font for readability
- Shows CSV format with headers
- Required columns listed in hint

**Action Buttons**:
- **Process CSV**: Submit up to 10 rows for batch inference
- **Download Full Example CSV**: Get complete 30-row sample file

**Format Help Box** (blue):
- Quick reminder of required columns
- Link to `DATA_FORMAT.md` for full spec

### Results Display (After Processing)

**Success Box** (green):
- "Batch Processing Complete"
- Shows: Processed X of Y rows

**Results Table**:
- **Window ID**: From CSV
- **CME**: Computed value (bold)
- **p_flow**: Flow probability (%)
- **Label**: Ground truth from CSV (colored badge)
  - Green badge: "Flow"
  - Red badge: "No_Flow"
- **Latency**: Request time (ms)

**Use Cases**:
- Test with historical EEG data
- Validate model on labeled dataset
- Compare predictions vs. actual labels
- Analyze batch performance

---

## Analytics Tab

**Shows**:
- Full Performance Metrics charts (larger view)
- Recent Training Jobs table (more space)

**Use Case**: Focused view for analysis and monitoring

---

## Color Coding System

### Status Badges
- 🟢 **Green (#10b981)**: Healthy, Completed, Success
- 🔵 **Blue (#3b82f6)**: Running, Active, In Progress
- 🟡 **Yellow (#f59e0b)**: Queued, Waiting, Pending
- 🔴 **Red (#ef4444)**: Failed, Error, Problem

### Component Colors
- **Purple (#8b5cf6)**: Client/User actions
- **Blue (#3b82f6)**: API/Backend processing
- **Orange (#f59e0b)**: Quantum Backend
- **Green (#10b981)**: Database/Storage
- **Pink (#ec4899)**: Background Workers
- **Gold (#fbbf24)**: Highlighted values (CME, important metrics)

### UI Hierarchy
- **Dark background (#0f172a)**: Base layer
- **Card background (#1e293b)**: Elevated panels
- **Input background (#0f172a)**: Form fields
- **Borders (#334155)**: Subtle separators

---

## Interactive Elements

### Buttons

**Primary** (Blue):
- Main actions (Start Training Job)
- Hover: Brighter blue, lift effect

**Success** (Green):
- Positive actions (Compute CME, Process CSV)
- Indicates data submission

**Secondary** (Gray):
- Less important actions (Refresh, Load Example)

**All buttons**:
- Disabled state when loading (cursor: not-allowed)
- Loading spinner replaces icon
- Smooth transitions

### Form Inputs

**Text inputs**:
- Dark background, light text
- Blue focus ring (accessibility)
- Placeholder text in gray

**Sliders**:
- Dark track, blue thumb
- Shows current value above
- Smooth dragging

**Dropdowns**:
- Matching dark theme
- Blue focus state
- Clear options

**Text areas**:
- Monospace font for code/CSV
- Resizable vertically
- Syntax-appropriate styling

### Progress Bars (Training Table)

- **Background**: Dark gray
- **Fill**: Blue gradient (left to right)
- **Overlay text**: Fraction (5/10)
- **Animation**: Smooth width transition
- **States**:
  - 0%: Empty bar, "0/10"
  - 50%: Half-filled, "5/10"
  - 100%: Full blue, "10/10"

---

## Hints and Tooltips

### Form Hints (Gray italic text below labels)

**Online Inference**:
- Session ID: "EEG recording session identifier"
- Window ID: "Unique ID for this 1-second EEG segment"
- Task Difficulty: "0.0 = Very Easy | 0.5 = Moderate | 1.0 = Very Difficult"
- EEG Features: "Normalized features [-1, 1]: Alpha, Beta, Theta, Delta power bands..."

**Training Jobs**:
- Algorithm: "Choose optimization algorithm for circuit parameter search"
- Generations: "Number of optimization iterations (more = better convergence, but slower)"

### Slider Hints (Below sliders)

**Training Jobs**:
"**What's being optimized:** 8 quantum circuit parameters (rotation angles: α₀-α₃, β₀-β₃) to maximize flow state detection accuracy on labeled EEG data."

---

## Reading the Charts

### Response Time Distribution Chart

**X-axis**: Three bars
- **Average**: Mean latency (typical performance)
- **P95**: 95th percentile (most requests complete by here)
- **P99**: 99th percentile (worst-case latency)

**Y-axis**: Milliseconds

**Interpretation**:
- All bars ~1200ms: Consistent performance
- P99 >> Average: High variance, some requests are very slow
- Trend over time: Performance degradation? Or stable?

### Training Jobs by Status Chart

**X-axis**: Status categories
- **Queued**: Waiting to start
- **Running**: Currently processing
- **Completed**: Finished successfully
- **Failed**: Encountered error

**Y-axis**: Count

**Interpretation**:
- Many "Running": System under load
- High "Completed": Successful training runs
- Any "Failed": Check logs for issues

---

## Workflow: Your First Inference

1. **Open Dashboard**: http://localhost:3000
2. **See "Control Panel" tab** (default)
3. **Scroll to "Online Inference" panel** (left)
4. **Keep defaults** (already filled in)
5. **Click "Compute CME"** (green button)
6. **Wait 1-2 seconds** (watch spinner)
7. **See results** in green box:
   - CME value (e.g., 45.23) in **gold**
   - p_flow percentage (e.g., 62.3%)
   - Quantum metrics (shots, depth, latency)
8. **Check top cards**: Total Requests increased to 1
9. **Done!** You've run your first quantum inference

---

## Workflow: Starting a Training Job

1. **Scroll to "Training Jobs" panel** (right)
2. **Select algorithm** from dropdown:
   - Try "Particle Swarm Optimization"
3. **Adjust generations slider**: Set to 10
4. **Read description**: Shows what PSO does
5. **Click "Start Training Job"** (blue button)
6. **See job created** in green box:
   - Job ID shown
   - Status = "Queued"
7. **Scroll down to "Recent Training Jobs" table**
8. **Watch live progress**:
   - Status changes: Queued → Running
   - Progress bar fills: 0/10 → 5/10 → 10/10
   - QPU Calls increases
   - Best Fitness updates
9. **After ~45 seconds**: Status = "Completed"
10. **Done!** Job finished and results stored

---

## Workflow: Batch CSV Processing

1. **Click "Data Upload" tab**
2. **Choose method**:
   - Option A: Click "Load Example Data"
   - Option B: Click "Upload CSV File" → select file
   - Option C: Paste CSV directly
3. **Review data** in text area
4. **Click "Process CSV"** button
5. **Wait for processing** (progress shown)
6. **See results table**:
   - Each row processed
   - CME computed for each window
   - Labels shown (Flow vs No_Flow)
   - Compare predictions to ground truth
7. **Download full example** if needed
8. **Done!** Batch analysis complete

---

## Workflow: Understanding the Process

1. **Click "Process Flow" tab**
2. **Read "Online Inference Path"**:
   - See colored boxes for each component
   - Follow arrows to understand flow
   - Note timing information
3. **Read "Training Job Path"**:
   - See background processing loop
   - Understand metaheuristic iterations
4. **View "Quantum Circuit Details"**:
   - ASCII art shows circuit structure
   - Parameter annotations
5. **Read "Petri Net Mapping"**:
   - Places and transitions listed
   - Timing parameters table
   - Validation strategy for dissertation
6. **Done!** Complete understanding of system architecture

---

## Troubleshooting UI Issues

### Dashboard Won't Load
- **Check**: Is http://localhost:3000 accessible?
- **Fix**: `docker-compose restart dashboard`

### "Failed to fetch data" Error
- **Cause**: API is down or unreachable
- **Check**: http://localhost:5000/api/dashboard/summary
- **Fix**: `docker-compose restart api`

### Charts Are Empty
- **Cause**: No data in database yet
- **Fix**: Submit at least one inference request

### CSV Upload Fails
- **Cause**: Invalid format or values
- **Check**: Features in [-1, 1]? All columns present?
- **Fix**: Use "Load Example Data" to see correct format

### Training Job Stuck in "Queued"
- **Cause**: Background worker not running
- **Check**: `docker logs cme-api | findstr Training`
- **Fix**: `docker-compose restart api`

---

## Advanced Tips

### Monitoring Performance

1. Run simulation client in terminal:
   ```bash
   cd cme-sim-client
   npm run simulate -- --duration 120 --onlineRate 2
   ```

2. Keep dashboard open in browser

3. Watch in real-time:
   - Total Requests counter increases rapidly
   - Charts update every 5 seconds
   - P95/P99 latencies reflect system load

### Comparing Algorithms

1. Start 4 training jobs, one with each algorithm
2. Watch "Recent Training Jobs" table
3. Compare:
   - Which completes fastest?
   - Which achieves best fitness?
   - QPU call efficiency

4. Note: In this simulation, all behave similarly (imitation model)
   - Real system would show algorithm differences

### CSV Data Analysis

1. Prepare CSV with both Flow and No_Flow examples
2. Upload and process
3. Check predictions vs. labels
4. Calculate accuracy: (correct predictions / total) × 100%
5. Compare to quantum probabilities (p_flow values)

---

## Color Legend for Process Flow Diagrams

- **Purple boxes**: User/Client interactions
- **Blue boxes**: API processing
- **Orange boxes**: Quantum operations
- **Green boxes**: Database operations
- **Pink boxes**: Background workers
- **Dashed boxes**: Loops/iterations

---

## Summary

Every element in the dashboard is now:
- ✅ **Clearly labeled** with descriptive text
- ✅ **Explained with hints** showing what it means
- ✅ **Color-coded** for visual clarity
- ✅ **Interactive** with real-time updates
- ✅ **Documented** in this guide

For dissertation work:
- Use **Process Flow tab** to understand architecture
- Use **Data Upload** to test with real EEG data
- Use **Recent Jobs table** to compare algorithms
- Export metrics for comparison with Petri net simulation

No more confusion! Everything is crystal clear! 🎯
