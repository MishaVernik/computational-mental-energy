# DTDL Ontology — CME Cognitive Digital Twin

DTDL v3 (Digital Twin Definition Language, JSON-LD) ontology for the cognitive-state
digital twin used in the CME pipeline. The same models are uploaded to Azure Digital
Twins (West Central US) and serve as the formal data contract regardless of the runtime
backend.

## Models

| File | DTMI | Role |
|---|---|---|
| `User.json` | `dtmi:cme:User;1` | One twin per subject. Holds anonymized id, daily CME budget, current pFlow / CME rate, plus 9 clinical derived indices (engagement, cognitive load, relaxation, alpha asymmetry, flow minutes today, budget utilization, fatigue, current activity, current session). |
| `Headband.json` | `dtmi:cme:Headband;1` | Wearable EEG device (Muse Athena). 4 electrode children, derived `connectionState` (connected / disconnected / poorContact / simulated), rolling 60s `lastSignalQualityMean`, `dropoutCountLastHour`. |
| `Electrode.json` | `dtmi:cme:Electrode;1` | Single channel (TP9, AF7, AF8, TP10). Telemetry: delta/theta/alpha/beta/gamma in µV², plus numeric `quality` and categorical `contactQuality` (good/weak/none). |
| `Session.json` | `dtmi:cme:Session;1` | Contiguous recording. Activity tag, complexity c(t), inference mode, cumulative CME (Vn). On stop, adds `peakPFlow`, `flowMinutes`, `dataIntegrityScore`, `bestActivity`, `endedReason`. |
| `Window.json` | `dtmi:cme:Window;1` | 5-s window. pFlow (hybrid/quantum/classical), CME (Vn), flow decision, window class. |
| `Activity.json` | `dtmi:cme:Activity;1` | Catalogue entry for an annotated activity (coding, meditation, math, …). One shared twin per `ActionDefinition` row. |

## Relationships

```
User --[wears]--> Headband
User --[runs]--> Session
User --[practiced]--> Activity     // properties: totalCmeVn, totalMinutes, sessionCount, personalAvgPFlow, lastUsedAt
Headband --[hasElectrode]--> Electrode (x4)
Session --[contains]--> Window (x many)
Session --[hasActivity]--> Activity   // max multiplicity 1; replaced when active action changes
```

The `User --[practiced]--> Activity` relationship is the per-user usage layer: it
carries the personal aggregates so the shared `Activity` twin can stay a clean
catalogue entry. Counters are incremented once per touched activity per session-end.

## Twin instances created by the bootstrapper

| Twin id | Type | Notes |
|---|---|---|
| `user-default` | User | Local lab user; replace with anonymized id in production. |
| `headband-default` | Headband | Muse Athena. |
| `electrode-TP9`, `electrode-AF7`, `electrode-AF8`, `electrode-TP10` | Electrode | One per channel. |
| `activity-<slug>` | Activity | One per active `ActionDefinition` row (e.g. `activity-coding`, `activity-meditation`). Refreshed on API startup. |
| `session-<sessionId>` | Session | Created on `SessionStarted`. |

`Window` instances are optional and only created if per-window history is exported to
ADT (off by default to keep operation cost ≈ $0; see
[docs/digital_twin_platform.md](../digital_twin_platform.md) §Cost envelope).

## Uploading to Azure Digital Twins

```powershell
# First run (or new instance): just upload all models.
./scripts/Complete-Azure-Setup.ps1

# When DTDL schema changes (e.g. this enhancement batch): wipe and re-upload.
./scripts/Complete-Azure-Setup.ps1 -Reset
```

`-Reset` deletes every twin, relationship, and model in the target ADT instance, then
re-uploads in dependency order (`Activity` → `Window` → `Electrode` → `Headband` →
`Session` → `User`) so cross-references resolve. The CmeSim.Api bootstrapper then
re-creates the base twins on startup.

## Vendor neutrality

DTDL is a Microsoft-led specification, but the files are valid JSON-LD and the
information model (User → Headband → Electrode + Session → Window + Activity) can be
ported to Eclipse Vorto, FIWARE Smart Data Models, or W3C Web of Things Thing
Descriptions with mechanical translation.
