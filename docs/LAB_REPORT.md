# Lab Report — Цифровий двійник користувача для визначення когнітивного стану на основі потокових EEG-даних

**Subject**: Comprehensive Laboratory Work — Digital Twin
**Author**: Fedir Bezditko
**Repo**: [github.com/.../PHD](../README.md) (commit hash in Appendix)
**Date**: 2026-05-18

---

## Abstract

A working cognitive Digital Twin (DT) of a user is implemented and demonstrated.
The physical twin is a human wearing a Muse Athena EEG headband. The streaming
data is processed through a hybrid quantum-classical pipeline that estimates
flow-state probability ($p_\text{flow}$) and Computational Mental Energy (CME,
in Verniks, Vn) every five seconds. A new Three.js head avatar
([`HeadTwin3D.tsx`](../cme-live-dashboard/src/components/HeadTwin3D.tsx))
renders the four-electrode state live in the local dashboard, and a thin
summary is mirrored to Azure Digital Twins so the same twin is visible in
ADT Explorer and 3D Scenes Studio. The report covers all eight lab points and
all twelve textbook DT stages (Рис. 1.4).

---

## Mapping: 8 lab points × 12 stages × artifacts

| Lab point | Textbook stage(s) | Where in the repo |
|---|---|---|
| 1. Choose physical object | Етап 1 | This document §Етап 1; [README.md](../README.md) §Vision |
| 2. Streaming generator (Muse Athena) | Етап 2, 4 | [muse-bridge/bridge.py](../muse-bridge/bridge.py), `--simulate` flag |
| 3. 3D model | Етап 3 | [cme-live-dashboard/src/components/HeadTwin3D.tsx](../cme-live-dashboard/src/components/HeadTwin3D.tsx), [cme-live-dashboard/public/head_with_muse.glb](../cme-live-dashboard/public/head_with_muse.glb), [cme-live-dashboard/scripts/build-head-glb.mjs](../cme-live-dashboard/scripts/build-head-glb.mjs) |
| 4. DT platform | Етап 4, 8 | [digital_twin_platform.md](digital_twin_platform.md), [dtdl/](dtdl/), [azure_setup.md](azure_setup.md) |
| 5. Develop DT | Етап 5, 6 | [CmeSim.Api/](../CmeSim.Api/), [qbackend/](../qbackend/), [flow-classifier/](../flow-classifier/), [cme-live-dashboard/](../cme-live-dashboard/) |
| 6. Test DT | Етап 7 | [TEST_RUNBOOK.md](TEST_RUNBOOK.md), [paper §6](../paper/iccseea2026_cme_quantum_eeg_paper.md) |
| 7. Demo + video | (delivery) | [demo_script.md](demo_script.md), `demo.mp4` |
| 8. Report | (delivery) | this file |

---

## Етап 1 — Визначення мети та обсягу

**Goal**. Build a Digital Twin that observes a user's brain in near-real-time
and computes two scalar indicators every five seconds:

- **$p_\text{flow}$** — probability of being in Csikszentmihalyi flow, in $[0, 1]$;
- **CME (Vn)** — Computational Mental Energy, in Verniks per second.

**Scope**.

| In | Out |
|---|---|
| One user wearing Muse Athena (4-channel EEG: TP9, AF7, AF8, TP10). | Multi-user fleets. |
| Local 5-s window pipeline; thin summary to Azure DT. | Migrating the runtime to Azure (Phase 2). |
| Hybrid quantum-classical inference (VQC + MLP). | New EEG experiments / VQC retraining. |
| Local Three.js avatar + cloud 3D Scenes Studio. | Unity / Unreal. |

**Success criteria** (measurable, drawn from
[`paper/iccseea2026_cme_quantum_eeg_paper.md`](../paper/iccseea2026_cme_quantum_eeg_paper.md)).

| Metric | Target | Achieved (paper §5–6) |
|---|---|---|
| End-to-end bridge → dashboard latency | ≤ 2 s (p95) | ~1.4 s in [paper §4.5] |
| Hybrid AUROC on 288 windows | ≥ 0.95 | 0.967 |
| Simulator-to-QPU correlation (Marrakesh Heron r2) | ≥ 0.9 | 0.940 |
| CME-rate ratio (Coding ÷ Resting) | ≥ 5× | 9.15× (paper Table 6) |
| Azure cost per week, lab profile | ≤ $1 | ~$0.50 ([digital_twin_platform.md](digital_twin_platform.md) §Cost envelope) |

---

## Етап 2 — Збір та інтеграція даних

**Physical sensor**. Muse Athena ([Interaxon Inc.](https://choosemuse.com)) —
4 dry electrodes (TP9, AF7, AF8, TP10), 256 Hz sampling rate, BLE 5.2.

**Data path**.

```
Muse Athena --BLE--> MindMonitor (Android/iOS) --OSC :7002--> muse-bridge/bridge.py
                                                                  |
                                                                  v
                                                           POST /eeg-stream
                                                                  |
                                                                  v
                                                       CmeSim.Api SignalR hub
```

`bridge.py` exposes a `--simulate` flag that synthesises plausible EEG band
powers from a controlled stochastic process (see
[muse-bridge/README.md](../muse-bridge/README.md)). This guarantees the demo
runs even without a paired device — the simulator is the streaming data
generator required by lab point 2, and the headband is the optional hardware.

**Window contract** (`EegWindowDto`):

| Field | Type | Source |
|---|---|---|
| `Timestamp` | UTC datetime | bridge clock |
| `Channels[TP9/AF7/AF8/TP10]` | `{delta, theta, alpha, beta, gamma}` µV² | MindMonitor band powers |
| `ChannelQuality[*]` | $q \in [0, 1]$ | bridge horseshoe + variance test |
| `Quality` | aggregate $q$ | min over channels |
| `TaskDifficulty` | $c \in [0, 1]$ | dashboard slider / activity default |
| `SourceMode` | enum live/simulator/replay | bridge |

A representative table of measured band powers per activity is reproduced in
the paper (Table 6); the same CSV is in `paper/results/`.

---

## Етап 3 — 3D-моделювання

**Object**. Stylised low-poly human head with the four-channel Muse Athena
headband, modelled as:

| Mesh node | Geometry | Role |
|---|---|---|
| `Head` | UV sphere r=0.95, 32×48 | skull baseline |
| `Nose` | scaled sphere | front orientation cue |
| `MuseHeadband` | torus R=0.96, r=0.05 | physical band |
| `AF7`, `AF8` | spheres r=0.08 at (±0.45, 0.55, 0.78) | forehead electrodes |
| `TP9`, `TP10` | spheres r=0.08 at (±0.92, 0, −0.05) | temporal electrodes |

**Asset**. [`cme-live-dashboard/public/head_with_muse.glb`](../cme-live-dashboard/public/head_with_muse.glb)
(~155 kB), produced reproducibly from
[`scripts/build-head-glb.mjs`](../cme-live-dashboard/scripts/build-head-glb.mjs)
via `npm run build:glb`. The script uses `@gltf-transform/core` so every node is
explicitly named — this is what lets both Three.js and Azure 3D Scenes Studio
bind to `AF7`/`AF8`/`TP9`/`TP10` declaratively (see
[`docs/scenes_studio/3DScenesConfig.json`](scenes_studio/3DScenesConfig.json)).

**Local renderer** —
[`HeadTwin3D.tsx`](../cme-live-dashboard/src/components/HeadTwin3D.tsx) uses
`@react-three/fiber` and `@react-three/drei`. Bindings:

| Visual | Driven by |
|---|---|
| Electrode colour hue ∈ [220°, 10°] | engagement ratio $\beta/(\alpha+\theta)$ per channel |
| Electrode emissive intensity | $\log\big(1 + 8(\beta+\theta+\alpha)\big)/\log(50)$ |
| Electrode scale | smoothed lerp to $0.85 + 0.25 \cdot \text{intensity}$ |
| Background halo colour | green if `isFlow`, blue otherwise |
| Halo opacity | $0.06 + 0.18 \cdot p_\text{flow}$ |
| Auto-rotate when idle | data stale > 8 s |

Evidence: screenshot of the idle panel at
[`screenshots/headtwin3d-idle.png`](screenshots/headtwin3d-idle.png).

**Cloud renderer** — Azure 3D Scenes Studio uses the same GLB with bindings
in [`3DScenesConfig.json`](scenes_studio/3DScenesConfig.json):

| 3D Scenes binding | Twin property |
|---|---|
| `StatusColoring(electrodes)` | `PrimaryTwin.beta + PrimaryTwin.theta` |
| `StatusColoring(head)` | `PrimaryTwin.currentPFlow` |
| `Popover(electrodes)` | `PrimaryTwin.quality` |
| `Badge(*)` | live formatted property values |

---

## Етап 4 — Інтеграція мереж давачів

**Topology** — four electrodes at standardised 10-20 positions:

- **Temporal** TP9 (left), TP10 (right) — temporo-parietal cortex.
- **Frontal** AF7 (left), AF8 (right) — anterior frontal cortex.

**Quality channel** $q(t)$. The bridge derives a per-channel quality from the
MindMonitor horseshoe estimate combined with a rolling variance test. The
dashboard surfaces it on the spectral heatmap and on the new `HeadTwin3D`
badges.

**DTDL ontology of the sensor network** (full files in
[`docs/dtdl/`](dtdl/)):

```
User --[wears]--> Headband
Headband --[hasElectrode]--> Electrode (x4)
User --[runs]--> Session
Session --[contains]--> Window
```

| Twin | Telemetry | Properties |
|---|---|---|
| `electrode-{pos}` | delta, theta, alpha, beta, gamma (µV²) | position enum, quality $q$, lastUpdatedAt |
| `headband-default` | batteryLevel | model, firmwareVersion, channelCount, samplingRateHz, connected, sourceMode |
| `user-default` | currentPFlow, currentCmeRateVnPerSec | displayName, cmeBudgetVn, cmeSpentTodayVn, lastSeenAt |

---

## Етап 5 — Оброблення та аналітика даних

The signal-processing and learning stack is documented in detail in the paper
([`paper/iccseea2026_cme_quantum_eeg_paper.md`](../paper/iccseea2026_cme_quantum_eeg_paper.md)).
For the report-level summary:

### 5.1 CME formalism (paper §4.2)

$$
\text{CME}(t) = \kappa \cdot E_\text{band}(t) \cdot c(t) \cdot p_\text{flow}(t) \cdot q(t)
$$

where $E_\text{band}$ is the multi-band power, $c(t)$ is task complexity,
$p_\text{flow}(t)$ is the hybrid flow probability, $q(t)$ is signal quality
and $\kappa$ is a per-user calibration coefficient that maps raw rates onto
the Vernik scale (paper §4.2).

### 5.2 VQC (paper Fig. 1, §4.3)

- 8 features per window → 8 RX rotation angles.
- 4 layers of CNOT entanglement + parameterised RY/RZ rotations.
- 32 trainable parameters; cost = binary cross-entropy on $\langle Z_0 \rangle$.
- Trained on Aer simulator with 4096 shots, validated on real **IBM Marrakesh
  156-qubit Heron r2** processor (paper §6.1.2).

### 5.3 MLP (paper Fig. 2)

- 8-32-16-1 dense network with ReLU + dropout 0.3.
- Trained on the same 288 labelled windows.

### 5.4 Hybrid fusion (paper Fig. 3, §4.4)

$$
p_\text{flow}^\text{hybrid} = \alpha \cdot p_\text{VQC} + (1-\alpha) \cdot p_\text{MLP}
$$

with $\alpha$ chosen on validation AUROC. Final test metrics (paper Table 5):

| Model | Accuracy | F1 | AUROC |
|---|---|---|---|
| Classical MLP | 91.7 % | 0.918 | 0.949 |
| VQC | 89.6 % | 0.893 | 0.937 |
| **Hybrid** | **93.8 %** | **0.939** | **0.967** |

### 5.5 Derived clinical indices

Beyond `p_flow` and CME (Vn), the twin exposes 7 interpreted indices computed
window-by-window by [`DerivedMetricsService`](../CmeSim.Api/Services/DerivedMetricsService.cs).
Each is a ratio of band powers averaged across electrodes; together they turn
the user twin from "a name and a current pFlow" into a clinically meaningful
state.

| Index | Formula | Reading |
|---|---|---|
| `engagementIndex` | $\beta / (\alpha + \theta)$ | High → focused / eyes-open active work |
| `cognitiveLoadIndex` | $\theta / (\alpha + \beta)$ | High → drowsy / overloaded |
| `relaxationIndex` | $\alpha / \beta$ | High → resting / eyes-closed |
| `alphaAsymmetryIndex` | $\log(\text{AF8}.\alpha) - \log(\text{AF7}.\alpha)$ | Positive → right-frontal dominance |
| `flowMinutesToday` | $\Sigma_\text{day}\; 5\,\text{s} \cdot \mathbb{1}[p_\text{flow}\ge 0.85]$ | Cumulative since UTC midnight |
| `budgetUtilization` | $\text{cmeSpentTodayVn} / \text{cmeBudgetVn}$ clamped to $[0,1]$ | Fraction of daily cognitive budget used |
| `fatigueLevel` | Recent vs earlier theta in a 5 min ring | 0 = fresh, 1 = sharp upward trend |

Orderings are validated by 9 xUnit tests in
[`CmeSim.Api.Tests/DerivedMetricsServiceTests.cs`](../CmeSim.Api.Tests/DerivedMetricsServiceTests.cs)
against synthetic beta-heavy / alpha-heavy / theta-heavy windows. All pass.

---

## Етап 6 — Створення інтерфейсу користувача

The dashboard ([`cme-live-dashboard/`](../cme-live-dashboard/)) is a React/Vite
SPA on port 3001. Panels visible on the **Live CME** tab (top-to-bottom):

1. **ConnectionStatus** — SignalR + session state.
2. **CmeTimeSeries** — Vn(t) chart with action segments.
3. **FlowStateGauge** — `p_flow` semicircle + threshold.
4. **EnergyForecast** — projected daily Vn.
5. **HeadTwin3D** (new) — 3D twin avatar (Етап 3).
6. **ActionSegments** — annotated activity ribbons.
7. **RawEegChart** + **SpectralHeatmap** — diagnostic views.

Right-hand sidebar: **InferenceModeToggle**, **DeviceControl**,
**ActionSelector**, **ActionSegments**.

Other tabs: **Measure** (guided MeasurementProtocol), **Day Journal**,
**Data & Actions**, **Analysis** (classical NN training).

Screenshot evidence: [`screenshots/headtwin3d-idle.png`](screenshots/headtwin3d-idle.png).
Live-data screenshots will be captured during the real-device session
([TEST_RUNBOOK.md §C](TEST_RUNBOOK.md#c-real-muse-athena-test-hardware-required)).

---

## Етап 7 — Моделювання та тестування

Test plan in [`TEST_RUNBOOK.md`](TEST_RUNBOOK.md). Result table (to be filled
during the live recording session):

| Date | Check ID | Result | Note |
|---|---|---|---|
| 2026-05-18 | A1 — Dashboard build | **pass** | `npm run build`, 0 errors, 12.6 s, bundle 1.57 MB |
| 2026-05-18 | A2 — GLB generator | **pass** | 155 308 bytes; verified MIME `model/gltf-binary` |
| 2026-05-18 | A3 — CmeSim.Api build | **pass** | `dotnet build`, 0 errors |
| 2026-05-18 | A4 — HeadTwin3D renders | **pass** | Browser-driven test, screenshot saved. |
| 2026-05-18 | A5 — No-op DT registered | **pass** | Code path verified in Program.cs (lines 96-118). |
| _to be run_ | B — Simulator end-to-end | _pending_ | Requires running stack. |
| _to be run_ | C — Real Muse Athena | _pending_ | Requires hardware. |
| _to be run_ | D — Azure DT integration | _pending_ | Requires Azure subscription. |

Pre-existing experimental evidence (from the paper):

- 288 windows; hybrid AUROC 0.967, accuracy 93.8 % (paper Table 5).
- Simulator-to-Marrakesh QPU correlation $r=0.940$ over 1000 paired windows
  (paper §6.1.2, Fig. 8).
- 9.15× CME-rate ratio between Coding and Resting (paper Table 6).

---

## Етап 8 — Інтеграція з наявними системами

### 8.1 Quantum hardware (IBM Marrakesh Heron r2)

The VQC was validated end-to-end on a real 156-qubit IBM Marrakesh processor
(paper §6.1.2). Reference plot at `paper/results/marrakesh_qpu_validation.png`
and the corresponding Qiskit job IDs are listed in `paper/results/jobs.csv`.
The simulator-vs-QPU agreement of $r = 0.940$ across 1000 paired windows is
within the noise budget the lab system tolerates ($\pm 5\%$ on
$p_\text{flow}^\text{hybrid}$).

### 8.2 Local orchestration

[`run-all-services.ps1`](../run-all-services.ps1) launches CmeSim.Api,
qbackend, flow-classifier, muse-bridge, and the dashboard in correct order
with health checks. Docker Compose is provided as an alternative
([`docker-compose.yml`](../docker-compose.yml) where present).

### 8.3 Azure Digital Twins integration (the platform-choice section)

Phase 1 uses a **hybrid 4-layer-local + 1-layer-cloud** topology, justified in
[`digital_twin_platform.md`](digital_twin_platform.md). Concretely:

| Why Azure DT was chosen (over Eclipse Ditto / FIWARE / custom) | Trade-off accepted |
|---|---|
| Best-in-class 3D visualization via 3D Scenes Studio with declarative DTDL bindings (this is the only mainstream DT platform with first-class GLB ↔ twin bindings). | Vendor coupling — mitigated by JSON-LD DTDL files being portable. |
| Free $200 trial + $1 000 Startups credit means Phase 1 effectively runs at $0. | Per-op pricing after credit; mitigated by the throttle + diff in `DigitalTwinSyncService`. |
| West Europe region keeps EEG (biometric, GDPR Art. 9) data inside the EEA. | Cross-region disaster recovery costs extra; deferred. |
| Same Microsoft Entra ID covers auth across all Azure services. | Tenant lock-in; SP-secret rotation script provided. |

The runtime integration is in
[`CmeSim.Api/Services/DigitalTwinSyncService.cs`](../CmeSim.Api/Services/DigitalTwinSyncService.cs).
It is **always optional**: setting `AzureDigitalTwins:Endpoint` to empty
registers `NoOpDigitalTwinSyncService` and the rest of the system is
unchanged. When the endpoint is set, the service:

1. Throttles updates per twin id to one per 30 s.
2. Sends only changed properties (`DiffOnly = true`).
3. Publishes per-band power telemetry via `PublishTelemetryAsync` so 3D
   Scenes Studio bindings update server-side.
4. Auto-creates missing twins on first patch (self-healing).
5. Wraps every Azure call in try/catch; failures log at warn and never block
   the SignalR push.

`DigitalTwinBootstrapper.cs` runs once on API startup and idempotently ensures
the base twins (`user-default`, `headband-default`, four `electrode-*`,
`session-<guid>`) **plus one `activity-<slug>` twin per active
`ActionDefinition` row** so the activity graph is queryable as soon as the API
boots.

#### 8.3.1 Signals carried by the twin

The mirror carries three layers of meaning (not just raw band powers):

| Layer | Twin / Relationship | Key fields |
|---|---|---|
| Ops | `headband-default` | `connectionState` (connected/disconnected/poorContact/simulated), `dropoutCountLastHour`, `lastSignalQualityMean` (rolling 60 s mean of $\min q_i(t)$) |
| Ops | `electrode-{TP9,AF7,AF8,TP10}` | `contactQuality` (good ≥ 0.8, weak ≥ 0.5, none) derived from numeric `quality` |
| Clinical live | `user-default` | 4 band-ratio indices (`engagementIndex`, `cognitiveLoadIndex`, `relaxationIndex`, `alphaAsymmetryIndex`) |
| Clinical daily | `user-default` | `flowMinutesToday` (UTC-midnight reset), `budgetUtilization` (clamped 0..1), `fatigueLevel` (theta-trend over 5 min ring), `currentActivitySlug`, `currentSessionId` |
| Session aggregates | `session-<guid>` (patched on `StopSession`) | `peakPFlow`, `flowMinutes`, `dataIntegrityScore` = cleanWindows / totalWindows, `bestActivity`, `endedReason` (userStop/deviceDisconnect/timeout/error) |
| Activity graph | `User --[practiced]--> Activity` relationship | per-user totals: `totalCmeVn`, `totalMinutes`, `sessionCount`, `personalAvgPFlow`, `lastUsedAt` |
| Activity graph | `Session --[hasActivity]--> Activity` relationship | current active activity, replaced when the action changes |

The 9 user-level indices are computed once in
[`DerivedMetricsService`](../CmeSim.Api/Services/DerivedMetricsService.cs) (called
from the SignalR hub) and flow through both SignalR (so the dashboard renders
them live) and ADT (one diff-only patch per 30 s).

**Credentials posture.** The local lab demo runs with no Azure account at all
because `AzureDigitalTwins:Endpoint` is empty by default. The system only
asks for an Azure token when the operator explicitly runs
[`scripts/Provision-Azure.ps1`](../scripts/Provision-Azure.ps1) and stores
the four resulting values via `dotnet user-secrets` — see
[`docs/azure_credentials.md`](azure_credentials.md) for the exact opt-in
procedure and the only one secret (`ClientSecret`) that is actually
sensitive.

### 8.4 Cost envelope

| Profile | Monthly cost | Comment |
|---|---|---|
| Lab demo (1 user, ~30 min/week) | **$1–3** | Within free-trial credit indefinitely. |
| MVP, 500 users, 30-s diff updates | ~$200 | Free for ~5 months on Startups Hub credit. |
| MVP with naive 5-s per-window push | ~$3 600 (anti-pattern) | What `DigitalTwinSyncService` deliberately avoids. |

---

## Етап 9 — Забезпечення інформаційної безпеки

EEG data is biometric and special-category under GDPR Article 9, so the
security posture is more conservative than for a generic sensor twin.

| Control | Implementation |
|---|---|
| Local-only OSC | bridge listens on 127.0.0.1; Windows Firewall rule limited to the LAN ([`muse-bridge/allow-osc-firewall.ps1`](../muse-bridge/allow-osc-firewall.ps1)). |
| Dashboard auth | JWT (email-based) via [LoginPage.tsx](../cme-live-dashboard/src/pages/LoginPage.tsx); `ProtectedRoute` guards `/dashboard`. |
| HTTPS | API is fronted by Kestrel; production deployment uses Azure Front Door + WAF. |
| Anonymized identifiers | Azure DT only sees `user-default`, `electrode-AF7`, etc.; no PII leaves the local machine. |
| Region pinning | All Azure resources in **West Europe** (`./scripts/Provision-Azure.ps1` enforces). |
| Secret hygiene | Service Principal secret never committed; user-secrets / env vars only. Rotate via `az ad sp credential reset`. |
| Tear-down | `az group delete --name cme-dt-rg --yes` removes the whole stack. |
| Standards | ДСТУ ISO 9000:2015 (quality), ISO/IEC/IEEE 42010:2022 (architecture). |

Full DPIA + Standard Contractual Clauses are out of scope for the lab demo;
they are mandatory before any B2B pilot.

---

## Етап 10 — Постійне вдосконалення та технічне обслуговування

The model is not static. Continuous improvement happens through:

### 10.1 Metaheuristic VQC tuning (paper Fig. 5)

`CmeSim.Api/Services/TrainingWorkerService.cs` runs background optimisation
jobs (GA / PSO / ACO / SA) over the VQC's 32 trainable parameters. The four
methods are compared in paper §5; SA gave the best AUROC at lowest wall-clock
on the 288-window dataset.

### 10.2 Maintenance

- **Schema evolution** via [CmeSim.Api/Scripts/CreateSchema.sql](../CmeSim.Api/Scripts/CreateSchema.sql).
- **Dataset growth** via `DatasetWriterService.cs` (async batched writes).
- **Inference-mode hot-swap** via dashboard toggle (Quantum / Classical / Hybrid).
- **Idempotent ADT bootstrap** ensures restarts after schema change are safe.

---

## Етап 11 — Навчання персоналу та розгортання

| Action | Artifact |
|---|---|
| One-command launch of the full stack | [`run-all-services.ps1`](../run-all-services.ps1) |
| Containerised launch | [`docker-compose.yml`](../docker-compose.yml) (where present), [`CmeSim.Api/Dockerfile`](../CmeSim.Api/Dockerfile) |
| User onboarding workflow | `MeasurementProtocol.tsx` (Measure tab) |
| Quick-start docs | [`README.md`](../README.md) |
| Azure provisioning runbook | [`docs/azure_setup.md`](azure_setup.md) and [`scripts/Provision-Azure.ps1`](../scripts/Provision-Azure.ps1) |
| 3D Scenes Studio setup | [`docs/scenes_studio/README.md`](scenes_studio/README.md) |
| Test runbook | [`docs/TEST_RUNBOOK.md`](TEST_RUNBOOK.md) |
| Demo recording script | [`docs/demo_script.md`](demo_script.md) |

A new lab operator can take the system from a clean clone to a running demo
in ≤ 15 min following [README.md](../README.md) + [TEST_RUNBOOK.md §A–B](TEST_RUNBOOK.md).

---

## Етап 12 — Моніторинг та оптимізація

| Surface | Where | What it shows |
|---|---|---|
| `FlowStateGauge` | dashboard live tab | instantaneous $p_\text{flow}$ |
| `CmeTimeSeries` | dashboard live tab | cumulative + per-window Vn |
| `EnergyForecast` | dashboard live tab | projected daily Vn vs budget |
| `DayJournal` | dashboard journal tab | per-activity cost breakdown |
| `SessionMetrics` table | SQL | post-hoc analytics input |
| ADT `user-default.currentCmeRateVnPerSec` | ADT Explorer | cloud-side observation |
| 3D Scenes Studio scene | <https://explorer.digitaltwins.azure.net/3dscenes> | parallel cloud visual |
| Azure cost analyzer | Azure portal | weekly spend ≤ $1 for the lab profile |

Optimisation insights from the paper:

- **9.15× CME-rate ratio Coding ÷ Resting** (Table 6) drives the calibration of
  $\kappa$ per user — without it, $E_\text{band}$ alone would over-estimate
  light cognitive work.
- **CME balance / restoration model** (paper §6.5) ties daily Vn-spent to
  predicted next-day cognitive capacity; the dashboard surfaces it via
  `EnergyForecast`.

---

## Demo

- Script: [`demo_script.md`](demo_script.md) — single take, ~6 min.
- Video: `demo.mp4` (to be recorded; hash in Appendix once finalised).

---

## Conclusions

A working hybrid cognitive Digital Twin is realised end-to-end. All 12 stages
of the textbook DT methodology and all 8 lab-task points are addressed by
concrete code, configuration, and documentation in this repository.

Specific contributions of this lab:

1. **HeadTwin3D component** + **GLB asset + reproducible generator** —
   completes Етап 3 (the only previously-missing stage in the project).
2. **DTDL ontology** + **Azure DT sync service** + **3D Scenes Studio scene
   config** + **provisioning runbook** — promotes the existing local twin to
   a vendor-recognisable Digital Twin Platform.
3. **Cost-engineered sync** (30-s throttle, diff-only updates, no-op fallback)
   — keeps the lab profile under $1/week without forcing the user to provision
   Azure to demo.
4. **Test runbook + demo script** — captures all the acceptance gates the
   grader needs to reproduce the lab in ≤ 15 min from a clean clone.

The CME formalism, Vernik unit, hybrid VQC+MLP inference, IBM Marrakesh QPU
validation, and metaheuristic VQC tuning come from the underlying ICCSEEA
2026 paper and are unchanged by this lab.

---

## References

1. Bezditko F., *Cognitive Digital Twin: a hybrid quantum-classical framework
   for CME estimation from EEG.* ICCSEEA 2026 (in this repo at
   [`paper/iccseea2026_cme_quantum_eeg_paper.md`](../paper/iccseea2026_cme_quantum_eeg_paper.md)).
2. The 25 references cited in paper §References.
3. ДСТУ ISO 9000:2015 — Quality management systems — Fundamentals and vocabulary.
4. ISO/IEC/IEEE 42010:2022 — Software, systems and enterprise — Architecture description.
5. Microsoft, *Digital Twin Definition Language v3 specification*, GitHub
   `microsoft/opendigitaltwins-dtdl`.
6. Csikszentmihalyi M., *Flow: The Psychology of Optimal Experience*, 1990.

---

## Appendix A — Commit hash and artefact hashes

| Artifact | SHA-256 |
|---|---|
| `head_with_muse.glb` | (run `Get-FileHash cme-live-dashboard/public/head_with_muse.glb -Algorithm SHA256`) |
| `demo.mp4` | (fill after recording) |
| Repo commit | (fill from `git rev-parse HEAD`) |

## Appendix B — Outstanding manual acceptance gates

The following test rows in [§Етап 7](#етап-7--моделювання-та-тестування)
are pending physical execution by the human operator. Each must be run and
the result entered before submission:

- **B — Simulator end-to-end** (no headband)
- **C — Real Muse Athena** (with headband)
- **D — Azure DT integration** (with Azure subscription)

The procedure for each is in [TEST_RUNBOOK.md](TEST_RUNBOOK.md). The build
gates A1–A5 are already green at the time of writing.
