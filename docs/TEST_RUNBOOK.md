# Test Runbook — Digital Twin Lab

End-to-end test plan for the cognitive Digital Twin. Covers automated checks
that can run with no hardware and the manual checks that require a Muse Athena
device. Use this as the acceptance checklist for §6 of the lab report.

## A. Automated checks (no hardware, no Azure)

These run in any clean clone and are already known-green in the current build
(captured 2026-05-18).

| ID | Check | Command | Expected |
|---|---|---|---|
| A1 | Dashboard TypeScript build | `cd cme-live-dashboard && npm run build` | `built in <15 s`, 0 TS errors. |
| A2 | GLB asset generator | `cd cme-live-dashboard && npm run build:glb` | Writes `public/head_with_muse.glb` (~155 KB). |
| A3 | CmeSim.Api build | `cd CmeSim.Api && dotnet build` | `Build succeeded`, 0 errors. |
| A4 | Dashboard renders the new panel | `npm run dev` then navigate to `/dashboard` after login | "3D Twin · Head + Muse Athena" panel visible with 4 electrode labels (TP9, AF7, AF8, TP10), status pill `○ idle`. Screenshot evidence: [`screenshots/headtwin3d-idle.png`](screenshots/headtwin3d-idle.png). |
| A5 | No-op DT sync registered when Endpoint is empty | Start API; check logs | One INFO line: `AzureDigitalTwins:Endpoint is empty - digital twin sync is disabled (local-only mode).` |

## B. Local simulator test (no headband)

This is the demo path the **video uses**. Run from a clean repo state.

### Prereqs

- SQL Server reachable (local or Azure SQL via the `DefaultConnection` string).
- Python venv created and dependencies installed in `muse-bridge/` and
  `qbackend/` and `flow-classifier/`.

### Steps

1. `./run-all-services.ps1`
2. `cd muse-bridge && python bridge.py --simulate`
3. Open `http://localhost:3001`, log in (any email).
4. Click **Start Session**.

### Expected outcomes

| Acceptance criterion | Evidence |
|---|---|
| Status pill on HeadTwin3D switches from `○ idle` to `● live` within ~5 s. | Live screenshot. |
| All 4 electrode badges show non-zero `q` (signal quality) values. | Live screenshot. |
| Electrode spheres pulse and change colour with simulated EEG. | Visible in video. |
| `pFlow`, `CMEi` indicator strings on the panel header update at ~5-s cadence. | Visible in video. |
| End-to-end latency (bridge timestamp → dashboard update) ≤ 2 s. | Check browser DevTools: time between `ReceiveRawEeg` push and DOM repaint. |
| CME timeseries chart, FlowStateGauge, Energy Forecast all update simultaneously. | Visible in video. |
| Switching inference mode (Quantum / Classical / Hybrid) does not break the panel. | Visible in video. |

## C. Real Muse Athena test (hardware required)

This is the report-evidence path for §6 of the lab report. **Must be run by the
human operator wearing the headband**; an AI cannot perform it.

### Prereqs

- Muse Athena charged and paired with the operator's phone.
- MindMonitor app installed; OSC stream configured to target the dev box on
  port 7002.
- Same software stack running as in §B.

### Steps

1. Power up the Muse Athena, fit the band, and confirm signal quality on the
   phone.
2. In MindMonitor, enable OSC streaming.
3. On the dev box: `python bridge.py` (no `--simulate`).
4. In the dashboard: **Start Session** + **Start Action: Resting**.
5. Sit quietly with **eyes open** for 30 s.
6. **Close eyes** for 30 s. (Alpha band power should rise sharply on AF7/AF8.)
7. **Open eyes** + **Start Action: Mental arithmetic** for 60 s.

### Expected outcomes

| Acceptance criterion | Evidence |
|---|---|
| Eye-open → eye-close: AF7 + AF8 electrodes turn **bluer** (lower beta/theta engagement) and grow as alpha power rises. | Side-by-side screenshot pair: `screenshots/real-eyes-open.png`, `screenshots/real-eyes-closed.png`. |
| Mental arithmetic: AF7/AF8 turn **redder** and pulse faster; pFlow rises. | `screenshots/real-arithmetic.png`. |
| No console errors in the dashboard during a 5-min session. | Browser DevTools, attached as `logs/real-session-console.txt`. |
| `kappa` calibration converges within 24 windows (≈2 min). | Calibration state in `CmeMetricsService` log. |
| Session row appears in the `Sessions` SQL table with sensible `EndedAt`, `cumulativeCmeVn`. | `SELECT TOP 5 * FROM Sessions ORDER BY StartedAt DESC` screenshot. |

## D. Azure Digital Twins integration test (Azure subscription required)

Optional in Phase 1 — required only for the §4 / §8 platform-choice evidence
in the lab report.

### Prereqs

- Azure subscription with $200 free trial active.
- `./scripts/Provision-Azure.ps1` has been run successfully.
- Service Principal credentials set as user-secrets in `CmeSim.Api`.

### Steps

1. `dotnet user-secrets set "AzureDigitalTwins:Endpoint" "https://cme-dt-xx.api.weu.digitaltwins.azure.net" --project CmeSim.Api`
2. Repeat for `TenantId`, `ClientId`, `ClientSecret`.
3. Start the stack as in §B, run for 60 s.
4. Open **Azure Digital Twins Explorer** and query:

   ```cypher
   SELECT * FROM digitaltwins WHERE STARTSWITH($dtId, 'electrode-')
   ```

### Expected outcomes

| Acceptance criterion | Evidence |
|---|---|
| Twin instances exist: `user-default`, `headband-default`, `electrode-TP9`, `electrode-AF7`, `electrode-AF8`, `electrode-TP10`, `session-<guid>`. | ADT Explorer twin list screenshot, saved as `screenshots/adt-explorer-twins.png`. |
| Within ~30 s of `RecordWindow` start, `electrode-AF7.quality` is updated and visible in ADT Explorer. | ADT Explorer property-watch screenshot. |
| `user-default.cmeSpentTodayVn` increases monotonically across the session. | ADT Explorer property-watch screenshot. |
| `session-<guid>.endedAt` is set after **Stop Session**. | ADT Explorer twin detail screenshot. |
| 3D Scenes Studio scene shows the head and electrodes coloured per ADT properties. | `screenshots/scenes-studio-live.png`. |
| No more than ~0.5–1.5 ADT operations per second on average (verify in Azure cost analysis). | Cost analysis screenshot. |

## E. Failure-mode tests

| Scenario | Procedure | Expected behaviour |
|---|---|---|
| ADT credentials wrong | Set bad ClientSecret, restart API | API still runs; warning logs from `DigitalTwinSyncService`; SignalR + dashboard unaffected. |
| Network to Azure cut | Disable internet during a session | Same: warnings logged, local stack continues. |
| GLB asset missing | Delete `public/head_with_muse.glb`, restart dev server | HeadTwin3D still renders (procedural fallback). 404 on the asset is silently ignored. |
| Bridge dies mid-session | Kill `bridge.py` | HeadTwin3D status pill flips to `○ idle` within ~8 s (per `STALE_MS` constant). |

## F. Performance budget

| Metric | Budget | How to measure |
|---|---|---|
| Bridge → dashboard render latency | ≤ 2 s p95 | Stamped `data.Timestamp` vs `performance.now()` at component update. |
| HeadTwin3D frame time | ≤ 16 ms (60 fps) on integrated GPU | Chrome DevTools Performance panel. |
| ADT update frequency per session | ≤ 4 ops/min/twin | Azure cost analysis. |
| API CPU during steady state | < 20 % single core | Task Manager during real session. |

## Reporting template

Each test run, append to [LAB_REPORT.md](LAB_REPORT.md) §Етап 7 a line in the
form:

```
| 2026-05-XX | A1 | pass | npm run build, 0 TS errors |
```
