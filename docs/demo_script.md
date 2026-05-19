# Demo Script — Digital Twin Lab Video

Target: **single-take, 6 minutes**, recorded with OBS to `docs/demo.mp4`.
Camera A is screen capture (1920×1080). Camera B is a phone on a tripod for
the headband don-on shot. The video tells the story of all 12 textbook
stages by walking through the running system in the same order as the lab
report sections.

## Pre-roll setup (do BEFORE pressing record)

1. Charge Muse Athena to ≥ 50 %.
2. Pair Muse with phone; open MindMonitor; configure OSC: `192.168.x.y:7002`.
3. On the dev box:
   - `./run-all-services.ps1` until all 5 services log "ready".
   - `bridge.py --simulate` running in one terminal.
   - `bridge.py` (real-device variant) command ready to paste in another.
4. Open 3 browser tabs:
   - **Tab 1**: `http://localhost:3001/dashboard` (logged in).
   - **Tab 2**: `https://explorer.digitaltwins.azure.net/` attached to the
     provisioned ADT instance (graph view of the 6 base twins).
   - **Tab 3**: `https://explorer.digitaltwins.azure.net/3dscenes`, scene
     **CME Cognitive Twin** open.
5. Stage a single window with the IBM Marrakesh result picture from
   `paper/results/marrakesh_qpu_validation.png` ready to alt-tab to.
6. OBS scenes pre-arranged:
   - `S1 — Physical twin` (phone full-screen)
   - `S2 — Dashboard` (Chrome Tab 1 full-screen)
   - `S3 — ADT Explorer` (Chrome Tab 2 full-screen)
   - `S4 — 3D Scenes Studio` (Chrome Tab 3 full-screen)
   - `S5 — Image overlay` (paper screenshot full-screen)

## Shot list (with rough timings)

| t | Scene | Action / VO line |
|---|---|---|
| **0:00–0:25** | S1 | Operator puts on Muse Athena. **VO**: "This is a digital twin of a user's cognitive state. The physical twin is me wearing a Muse Athena EEG headband." |
| **0:25–0:55** | S2 (still) | Show the dashboard layout; point at the new `HeadTwin3D` panel. **VO**: "The local digital twin lives in this dashboard. The new 3D Twin panel maps live EEG band-powers from four electrodes — TP9, AF7, AF8, TP10 — onto a head avatar in real time." |
| **0:55–1:35** | S2 (simulator) | In simulator terminal, signal already running. Click **Start Session**. **VO**: "First, simulator mode — a deterministic stochastic process so the visualisation is reproducible. The status pill flips to live, the four electrodes start pulsing, and CME accumulates in Verniks." |
| **1:35–2:25** | S2 (real) | Cut simulator with Ctrl-C; start real bridge (`python bridge.py`). Sit still. After ~10 s, **close eyes 20 s** then **open**. **VO**: "Now the real headband. Watch AF7 and AF8 — that's the forehead pair. When I close my eyes, alpha rises, and you see the electrodes shift colour and grow. This is the classic alpha-on-eye-close." |
| **2:25–3:25** | S2 (activities) | Click **Resting** for 30 s, then **Mental arithmetic** for 30 s. **VO**: "The system distinguishes activities by their cognitive cost. Resting accumulates Verniks at a base rate; mental arithmetic accelerates it almost ten-fold, matching the activity hierarchy in the paper." |
| **3:25–4:10** | S3 | Alt-tab to ADT Explorer. Highlight the graph: User → Headband → 4 Electrodes → Session. Click `electrode-AF7`, show `delta/theta/alpha/beta/gamma` updating. **VO**: "In parallel, the local backend mirrors a thin summary to Azure Digital Twins every thirty seconds. Same user, same headband, same four electrodes — but now as a formal industrial digital twin with a DTDL ontology." |
| **4:10–4:45** | S4 | Alt-tab to 3D Scenes Studio. Show the same head + electrodes coloured live. Hover one electrode to show the badge with `beta/theta/alpha`. **VO**: "Microsoft's 3D Scenes Studio renders the same GLB head model, but driven by Azure-side twin properties. So the local Three.js view and the cloud view are the same digital twin from two angles." |
| **4:45–5:20** | S5 | Show IBM Marrakesh QPU validation figure. **VO**: "The hybrid quantum-classical classifier behind the flow probability has been validated on a real 156-qubit IBM Marrakesh Heron r2 QPU. The simulator-to-QPU correlation across one thousand windows is 0.94." |
| **5:20–5:50** | S2 (close-out) | Back to the dashboard, point at FlowStateGauge, EnergyForecast, DayJournal tab. **VO**: "All twelve stages of the textbook DT methodology are realised: physical twin, streaming sensor net, 3D model, hybrid platform, processing and analytics, UI, testing, integration with quantum hardware, GDPR-aware security, continuous improvement via metaheuristics, deployment scripts, and daily cognitive-budget monitoring." |
| **5:50–6:00** | S2 + caption overlay | "F. Bezditko, ICCSEEA 2026 — Cognitive Digital Twin (CME) — Muse Athena + Azure DT + IBM Heron r2." End. |

## VO key phrases (must say each at least once)

- "cognitive digital twin"
- "Vernik" / "Verniks" (Vn)
- "four electrodes: TP9, AF7, AF8, TP10"
- "alpha rises on eye close"
- "hybrid quantum-classical"
- "Azure Digital Twins" / "ADT"
- "3D Scenes Studio"
- "IBM Marrakesh Heron r2"
- "two-second end-to-end latency"

## Take-2 contingencies

| Failure | Recovery |
|---|---|
| BLE disconnect mid-take | Mention it on camera, switch to `--simulate`, redo the alpha-on-eye-close visually with simulator profile that injects alpha. |
| ADT Explorer slow to refresh | Cut to a pre-recorded 10-s screen capture of ADT Explorer in S3. |
| 3D Scenes Studio not loading | Cut to `docs/screenshots/scenes-studio-live.png` as a still in S4 with a 5-s voiceover. |
| Dashboard SignalR drops | Hub auto-reconnects; just wait 3 s, the spinner is fine on camera. |

## Post

- Trim to ≤ 6:00.
- Add a sparse caption track for the VO key phrases.
- Export `docs/demo.mp4` (H.264, 1080p, ≤ 80 MB).
- Hash with `Get-FileHash docs/demo.mp4 -Algorithm SHA256` and paste the hash into the lab report Appendix.
