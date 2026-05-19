# CME Dashboard - Complete Guide

## What's New? 🎉

A **modern, professional web UI** has been added to the CME Quantum ML System! No more confusion about what's going on - now you have a beautiful real-time dashboard that makes everything crystal clear.

## Access the Dashboard

Open your browser and navigate to:

```
http://localhost:3000
```

## Dashboard Features

### 1. System Overview (Top Section)

Four real-time stat cards showing:
- ✅ **Total Requests**: Number of CME computations performed
- ✅ **Active Sessions**: Current EEG recording sessions
- ✅ **Avg Response Time**: System latency in milliseconds
- ✅ **Training Jobs**: Total jobs (queued + running + completed)

Plus performance metrics:
- Average CME value across all computations
- P95 and P99 latency (tail performance)

**Auto-refresh**: Every 5 seconds

### 2. Online Inference Panel (Left)

Submit EEG data for real-time CME computation:

**Input Fields**:
- Session ID (pre-filled with a valid session)
- Window ID (auto-generated with timestamp)
- Task Difficulty (slider from 0 to 1)
- EEG Features (JSON array, e.g., `[0.5, -0.3, 0.8, ...]`)

**Click "Compute CME"** to:
1. Send features to the quantum backend
2. Get flow probability from quantum circuit
3. Calculate CME value
4. Store results in database

**Results Display**:
- ⭐ **CME Value** (highlighted in gold)
- Flow Probability (percentage)
- Shots Used (quantum circuit measurements)
- Circuit Depth
- QPU Latency (quantum backend time)
- Total Latency (end-to-end time)

### 3. Training Jobs Panel (Right)

Start long-running model optimization jobs:

**Configuration**:
- Total Generations slider (5-50, default: 10)
- Each generation evaluates multiple candidate models

**Click "Start Training Job"** to:
1. Create a new training job in the database
2. Background worker picks it up automatically
3. Runs metaheuristic optimization loop
4. Each candidate calls the quantum backend

**Job Details Shown**:
- Job ID (GUID)
- Status badge (Queued/Running/Completed/Failed)
- Total generations
- Creation timestamp

### 4. Performance Metrics (Charts)

Two interactive charts:

**Response Time Distribution**:
- Bar chart showing Average, P95, P99 latencies
- Helps identify performance bottlenecks
- High P99 = some requests are very slow

**Training Jobs by Status**:
- Bar chart showing job counts per status
- Quickly see how many jobs are running/completed

**Summary Stats**:
- Total requests processed
- Average CME value
- Estimated throughput (req/s)

### 5. Recent Training Jobs (Bottom)

Live table with auto-refresh every 10 seconds:

**Columns**:
- Job ID (truncated)
- Status badge (color-coded)
- Progress bar (completed/total generations)
- Duration (seconds)
- QPU Calls (total quantum backend invocations)
- Best Fitness (optimization metric)

**Progress Indicators**:
- Blue progress bar fills as job completes
- Status changes from Queued → Running → Completed
- Live updates without page refresh

## Color Coding

### Status Badges
- 🟢 **Green**: Healthy, Completed (success states)
- 🔵 **Blue**: Running (active states)
- 🟡 **Yellow**: Queued (waiting states)
- 🔴 **Red**: Failed (error states)

### Visual Hierarchy
- **Primary Blue**: Interactive buttons, main actions
- **Gold/Yellow**: Highlighted values (CME, important metrics)
- **Dark Theme**: Optimized for long viewing sessions
- **Subtle Animations**: Hover effects, progress bars

## Example Workflow

### Quick Test Run

1. **Open Dashboard**: http://localhost:3000
2. **Check System Status**: See 2 active sessions, 0 requests initially
3. **Submit Inference**:
   - Leave Session ID as default
   - Adjust Task Difficulty to 0.7
   - Click "Compute CME"
   - Wait ~1-2 seconds
   - See CME result (e.g., 45.23)
4. **Start Training Job**:
   - Set Generations to 10
   - Click "Start Training Job"
   - See job ID and status = Queued
5. **Watch Progress**:
   - Scroll to "Recent Training Jobs"
   - Refresh shows status → Running
   - Progress bar fills (0/10 → 5/10 → 10/10)
   - Status → Completed
6. **Check Metrics**:
   - Total Requests increased to 1
   - Average Response Time ~1200ms
   - Charts updated

### Load Testing Scenario

1. **Start Background Load**:
   ```bash
   cd cme-sim-client
   npm run simulate -- --duration 300 --onlineRate 2 --clients 3
   ```

2. **Watch Dashboard in Real-Time**:
   - Total Requests counter increases rapidly
   - Response time metrics update every 5 seconds
   - P95/P99 latencies visible
   - Charts dynamically resize

3. **Submit Training Jobs During Load**:
   - Click "Start Training Job" multiple times
   - Observe QPU contention
   - Response times increase due to shared quantum backend

4. **Analyze Results**:
   - Check Training Jobs table
   - Compare QPU Calls between jobs
   - Notice longer durations under load

## Technical Details

### Architecture

```
Browser (React)
    ↓ HTTP GET/POST
Nginx (Port 3000)
    ↓ Proxy /api/* → http://api:5000/api/*
ASP.NET Core API
    ↓ SQL queries
SQL Server Database
```

### Auto-Refresh

- **Dashboard Summary**: 5 seconds
- **Training Jobs Table**: 10 seconds
- **Uses React hooks**: `useEffect` with `setInterval`
- **No WebSockets**: Simple polling (sufficient for this use case)

### API Integration

All API calls go through `src/api/client.ts`:
- `getDashboardSummary()` → GET /api/dashboard/summary
- `submitInference()` → POST /api/inference/cme
- `startTrainingJob()` → POST /api/training/start
- `listTrainingJobs()` → GET /api/training?limit=20

### Responsive Design

- **Desktop**: Full layout with all panels visible
- **Tablet**: Two-column grid adapts to single column
- **Mobile**: Stacked layout, horizontal scroll for tables
- **Min Width**: 320px (mobile devices)

## Keyboard Shortcuts

- **Ctrl+R / F5**: Refresh page
- **Tab**: Navigate between form fields
- **Enter**: Submit focused form (inference or training)

## Browser Requirements

- **Chrome/Edge**: ✅ Recommended
- **Firefox**: ✅ Full support
- **Safari**: ✅ Full support
- **IE11**: ❌ Not supported (requires ES2020)

## Troubleshooting

### Dashboard Won't Load

1. **Check if container is running**:
   ```bash
   docker ps | findstr dashboard
   ```

2. **Check logs**:
   ```bash
   docker logs cme-dashboard
   ```

3. **Verify port is not in use**:
   ```powershell
   netstat -ano | findstr :3000
   ```

4. **Restart dashboard**:
   ```bash
   docker-compose restart dashboard
   ```

### API Connection Errors

**Symptom**: Red error banner "Failed to fetch data"

**Causes**:
- API is down or unhealthy
- Network issues between dashboard and API

**Fix**:
```bash
# Check API health
curl http://localhost:5000/api/dashboard/summary

# Restart API if needed
docker-compose restart api
```

### Charts Not Showing

**Cause**: No data in database yet

**Fix**: Submit at least one inference request or training job

### Slow Response Times

**Expected**: 1-2 seconds per inference (quantum backend simulation)

**If > 5 seconds**:
- Check QPU latency settings
- Too many concurrent requests?
- Training jobs consuming QPU resources?

## Comparison: CLI vs Web UI

### CLI Simulation Client (`cme-sim-client`)
- ✅ Automated load testing
- ✅ Performance metrics collection
- ✅ Configurable traffic patterns
- ❌ No visualization
- ❌ No real-time monitoring
- **Use for**: Load testing, benchmarking, CI/CD

### Web Dashboard (`cme-dashboard`)
- ✅ Real-time visualization
- ✅ Interactive submission
- ✅ Live job monitoring
- ✅ User-friendly
- ❌ Manual operation only
- **Use for**: Development, debugging, demos, monitoring

**Best Practice**: Use both together!
- Run CLI client for load
- Watch dashboard for real-time impact

## Advanced Usage

### Custom Sessions

Create your own sessions in the database:

```sql
INSERT INTO Sessions (Id, UserId, StartedAt)
VALUES ('your-guid-here', 'researcher01', GETUTCDATE())
```

Then use that ID in the dashboard.

### Bulk Job Submission

Submit multiple training jobs via API:

```bash
for i in {1..5}; do
  curl -X POST http://localhost:5000/api/training/start \
    -H "Content-Type: application/json" \
    -d '{"totalGenerations": 10}'
done
```

Watch all 5 jobs in the dashboard table.

### Export Metrics

Query database directly for analysis:

```sql
-- Average latency over time
SELECT
  DATEPART(HOUR, RequestedAt) AS Hour,
  AVG(TotalLatencyMs) AS AvgLatency
FROM InferenceRequestLogs
GROUP BY DATEPART(HOUR, RequestedAt)
ORDER BY Hour
```

## Future Enhancements (Not Implemented)

Potential additions:
- WebSocket for instant updates (no polling)
- Historical charts (time series)
- User authentication
- Export data as CSV/JSON
- Dark/light theme toggle
- Mobile app (React Native)

## Support

- **Dashboard Issues**: Check browser console (F12)
- **API Issues**: Check docker logs
- **General Help**: See main README.md

## Summary

The dashboard makes everything **clear and visual**:
- ✅ See what's happening in real-time
- ✅ Submit requests interactively
- ✅ Monitor training job progress
- ✅ Visualize performance metrics
- ✅ Professional, polished interface

No more guessing what the system is doing - it's all right there on screen! 🎯


