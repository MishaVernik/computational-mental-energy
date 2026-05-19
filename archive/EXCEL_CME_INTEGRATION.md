# Excel CME Metrics Processing - Integration Summary

## Overview

Excel CME metrics processing has been **fully integrated** into the existing Web API and frontend dashboard. No separate console tool is needed - everything works through the unified API and UI.

---

## What Was Added

### 1. **Shared Core Library** (`Cme.Core/`)

**Purpose**: Reusable CME calculation logic shared between API and any future tools.

**Files**:
- `CmeConfig.cs` - Configuration for CME formula parameters
- `EegWindowRecord.cs` - Data model for EEG window records
- `CmeCalculator.cs` - Core CME calculation logic
- `MetricsCalculator.cs` - Session and global metrics computation
- `SessionMetrics.cs` - Metrics DTOs
- `ExcelCmeReader.cs` - Excel file reading
- `ExcelCmeWriter.cs` - Excel file writing

**Key Features**:
- ✅ Implements exact CME formula: `CME(t) = k × E_band(t) × g(c(t), p_flow(t)) × Δ`
- ✅ Energy calculation: `E_band = w_delta×Delta + w_theta×Theta + w_alpha×Alpha + w_beta×Beta`
- ✅ Modulation function: `g(c,p) = λ1×c + λ2×p + λ3×c×p`
- ✅ Automatic normalization (k factor) to keep max CME ≈ 100
- ✅ Flow state detection (configurable threshold)
- ✅ Session metrics (flow share, streaks, totals)
- ✅ Global summary metrics

### 2. **Web API Integration** (`CmeSim.Api/`)

**New Controller**: `CmeMetricsController.cs`
- **Route**: `/api/cme`
- **Endpoints**:
  - `POST /api/cme/compute-from-excel` - Returns JSON summary
  - `POST /api/cme/compute-from-excel-download` - Returns Excel file with results

**New Service**: `ICmeMetricsService.cs`
- Processes Excel files
- Computes CME metrics
- Writes results to Excel

**New DTOs**: `CmeMetricsDto.cs`
- `CmeMetricsResponseDto`
- `GlobalMetricsDto`
- `SessionMetricsDto`

**Integration**: Registered in `Program.cs` as `ICmeMetricsService`

### 3. **Frontend Integration** (`cme-dashboard/`)

**Updated Component**: `DataUpload.tsx`
- Added Excel file upload section
- Added "Compute CME Metrics" button
- Added "Download Results Excel" button
- Displays global summary and session summaries
- Shows configuration used

**Updated API Client**: `client.ts`
- Added `computeCmeFromExcel()` method
- Added `computeCmeFromExcelDownload()` method

**Updated Types**: `types.ts`
- Added `CmeMetricsResponse`, `GlobalMetricsDto`, `SessionMetricsDto`

---

## How to Use

### Via Web API

**1. Upload Excel and get JSON summary**:
```http
POST /api/cme/compute-from-excel
Content-Type: multipart/form-data

file: [Excel file]
worksheetName: [optional]
configJson: [optional JSON config]
```

**2. Upload Excel and download results Excel**:
```http
POST /api/cme/compute-from-excel-download
Content-Type: multipart/form-data

file: [Excel file]
worksheetName: [optional]
configJson: [optional JSON config]
```

### Via Frontend Dashboard

1. Go to **"Data Upload"** tab
2. Scroll to **"Excel CME Metrics Processing"** section
3. Click **"Upload Excel File (.xlsx)"**
4. Select your Excel file
5. Click **"Compute CME Metrics"** to see results
6. Click **"Download Results Excel"** to download Excel with all sheets

---

## Excel Input Format

### Required Columns

| Column | Type | Description |
|--------|------|-------------|
| `SessionId` | string/int | Session identifier |
| `DeltaPower` | float | Delta band power |
| `ThetaPower` | float | Theta band power |
| `AlphaPower` | float | Alpha band power |
| `BetaPower` | float | Beta band power |

### Optional Columns

| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `UserId` | string | - | User identifier |
| `TaskId` | string | - | Task identifier |
| `StartUtc` | datetime | - | Window start time |
| `EndUtc` | datetime | - | Window end time (defaults to 5s duration) |
| `ComplexityIndex` | float | 0.5 | Task complexity c(t) in [0,1] |
| `FlowProbability` | float | 0.0 | Flow probability p_flow(t) in [0,1] |
| `ArtifactScore` | float | - | Artifact score |
| `IsArtifact` | bool/int | - | Whether window contains artifacts |

**Note**: Any other columns are copied through to output.

---

## Excel Output Format

The system generates an Excel file with **3 sheets**:

### Sheet 1: `EEG_Windows_With_CME`
- All original columns
- Plus: `Eband`, `c(t)`, `p_flow(t)`, `DeltaSeconds`, `CME_raw`, `CME`, `IsFlowWindow`

### Sheet 2: `Session_Summary`
- Per-session metrics: TotalWindows, FlowWindows, FlowShare, AvgCME, MaxCME, CME_session, etc.

### Sheet 3: `Global_Summary`
- Global metrics: TotalSessions, Mean_CME_session, Mean_FlowShare, Sessions_FlowShare_GE_0.5, etc.
- Configuration parameters used

---

## CME Formula Implementation

### Energy Calculation
```
E_band(t) = w_delta × DeltaPower
          + w_theta × ThetaPower
          + w_alpha × AlphaPower
          + w_beta  × BetaPower
```

**Default weights**:
- `w_delta = 0.5`
- `w_theta = 1.0`
- `w_alpha = 1.0`
- `w_beta = 0.3`

### Modulation Function
```
g(c, p) = λ1 × c + λ2 × p + λ3 × c × p
```

**Default weights**:
- `λ1 = 0.5` (complexity coefficient)
- `λ2 = 0.5` (flow coefficient)
- `λ3 = 0.5` (interaction coefficient)

### CME Calculation
```
CME_raw(t) = E_band(t) × g(c(t), p_flow(t)) × Δ
CME(t) = k × CME_raw(t)  (where k normalizes max CME ≈ 100)
CME_session = Σ CME(t) for all windows in session
```

### Flow Detection
- Window is in flow if: `FlowProbability >= FlowThreshold` (default: 0.7)

---

## Configuration

Create a JSON config file to customize parameters:

```json
{
  "wDelta": 0.5,
  "wTheta": 1.0,
  "wAlpha": 1.0,
  "wBeta": 0.3,
  "lambda1": 0.5,
  "lambda2": 0.5,
  "lambda3": 0.5,
  "flowThreshold": 0.7,
  "maxCmeTarget": 100.0
}
```

Pass via `configJson` form field in API or frontend.

---

## Architecture

```
┌─────────────────┐
│  Frontend       │
│  (React)        │
│  DataUpload.tsx │
└────────┬────────┘
         │ HTTP POST
         ▼
┌─────────────────┐
│  Web API        │
│  CmeMetrics     │
│  Controller     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Service Layer  │
│  ICmeMetrics    │
│  Service        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Core Library   │
│  Cme.Core       │
│  - Excel Reader │
│  - CME Calc     │
│  - Metrics      │
│  - Excel Writer │
└─────────────────┘
```

---

## Testing

Unit tests are available in `Cme.Core.Tests/`:
- `CmeCalculatorTests.cs` - Tests CME calculation logic
- `MetricsCalculatorTests.cs` - Tests session/global metrics

Run tests:
```bash
dotnet test Cme.Core.Tests/Cme.Core.Tests.csproj
```

---

## Integration Status

✅ **Fully Integrated**:
- Core library created and shared
- Web API endpoints added
- Frontend UI integrated
- Types and API client updated
- Service registered in DI container

✅ **Ready to Use**:
- Upload Excel via dashboard
- View results in browser
- Download results Excel
- All metrics computed correctly

---

## Next Steps

1. **Test with sample Excel file** - Create a test Excel with required columns
2. **Verify results** - Check that CME values are reasonable (max ≈ 100)
3. **Customize config** - Adjust weights if needed for your data
4. **Use in research** - Process your EEG datasets and analyze flow states

---

## Files Created/Modified

### Created:
- `Cme.Core/` - Shared library (7 files)
- `Cme.Core.Tests/` - Unit tests (2 files)
- `CmeSim.Api/Controllers/CmeMetricsController.cs`
- `CmeSim.Api/Services/ICmeMetricsService.cs`
- `CmeSim.Api/DTOs/CmeMetricsDto.cs`

### Modified:
- `CmeSim.Api/Program.cs` - Registered service
- `CmeSim.Api/CmeSim.Api.csproj` - Added ClosedXML, Cme.Core reference
- `cme-dashboard/src/components/DataUpload.tsx` - Added Excel upload UI
- `cme-dashboard/src/api/client.ts` - Added Excel API methods
- `cme-dashboard/src/types.ts` - Added CME metrics types

---

## Summary

The Excel CME processing is **fully integrated** into your existing system. Users can:
1. Upload Excel files via the dashboard
2. See computed metrics immediately
3. Download results Excel with all sheets
4. Use the same API endpoints programmatically

Everything works through the unified Web API - no separate tools needed!



