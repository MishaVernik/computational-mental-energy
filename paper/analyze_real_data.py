"""Analyze real EEG data from the 8-activity measurement protocol.

Pulls 288 windows (8 activities x 36 windows x 5s) via the local API,
computes per-activity stats, runs Aer simulator batch inference,
and prepares a 100-window stratified sample for IBM Kingston QPU.
"""
import json
import os
import requests
import numpy as np
from collections import defaultdict

API = "http://localhost:5000/api"
QPU = "http://localhost:8001"
OUT = os.path.join(os.path.dirname(__file__), "results")
os.makedirs(OUT, exist_ok=True)

SESSION_ID = "9f0851fe-e620-4218-8e12-de4065fe84d3"

ACTIVITY_LABELS = {
    0.05: "Resting (Eyes Closed)",
    0.20: "Browsing",
    0.30: "Email",
    0.35: "Reading (General)",
    0.60: "Reading (Technical)",
    0.70: "Coding",
    0.80: "Debugging",
    0.90: "Math / Problem Solving",
}

DAILY_MINUTES = {
    0.05: 60, 0.20: 30, 0.30: 60, 0.35: 60,
    0.60: 60, 0.70: 120, 0.80: 120, 0.90: 60,
}

# ── 1. Pull windows from API ──────────────────────────────────
print("=== Pulling windows from API ===")
all_windows = requests.get(f"{API}/dataset/windows?labeled=false&limit=600", timeout=60).json()
windows_raw = [w for w in all_windows
               if w.get("actionSpikeId") and w.get("sessionId") == SESSION_ID]
print(f"  Total from API: {len(all_windows)}, tagged in session: {len(windows_raw)}")

# ── 2. Parse into structured data ─────────────────────────────
windows = []
for w in windows_raw:
    feats = w.get("features", [])
    if len(feats) < 20:
        continue
    avg_delta = np.mean(feats[0:20:5])
    avg_theta = np.mean(feats[1:20:5])
    avg_alpha = np.mean(feats[2:20:5])
    avg_beta  = np.mean(feats[3:20:5])
    avg_gamma = np.mean(feats[4:20:5])
    frontal_asym = feats[12] - feats[7]   # alpha AF8 - alpha AF7
    engagement = avg_beta / avg_theta if avg_theta > 0 else 0.5
    difficulty = w.get("taskDifficulty", 0.5)

    features_8_raw = [float(x) for x in [avg_delta, avg_theta, avg_alpha, avg_beta,
                                          avg_gamma, frontal_asym, engagement, difficulty]]
    max_abs = max(abs(x) for x in features_8_raw) or 1.0
    features_8 = [x / max_abs for x in features_8_raw]
    windows.append({
        "windowId": w["windowId"],
        "timestamp": w["timestamp"],
        "spikeId": w["actionSpikeId"],
        "features_8": features_8,
        "flowLabel": w.get("flowLabel"),
        "classicalPFlow": w.get("flowProbability"),
        "difficulty": difficulty,
        "activityName": ACTIVITY_LABELS.get(round(difficulty, 2), f"c={difficulty}"),
    })

print(f"  Processed {len(windows)} windows with 8-dim features")

# ── 3. Per-activity summary from live CME data ────────────────
activity_data = defaultdict(list)
for w in windows:
    activity_data[w["activityName"]].append(w)

# Fetch spike stats from API for ground-truth CME values
spike_ids = list(set(w["spikeId"] for w in windows))
spike_stats = {}
for sid in spike_ids:
    resp = requests.get(f"http://localhost:5000/api/sessions/spike-stats/{sid}", timeout=15).json()
    difficulty = next(w["difficulty"] for w in windows if w["spikeId"] == sid)
    act_name = ACTIVITY_LABELS.get(round(difficulty, 2), f"c={difficulty}")
    spike_stats[act_name] = {
        "spikeId": sid,
        "windowCount": resp["windowCount"],
        "meanPFlow": resp["meanPFlow"],
        "meanCmeRate": resp["meanCmeRate"],
        "totalCmeVn": resp["totalCmeVn"],
    }

print(f"\n=== Per-Activity Summary (from live CME data) ===")
activity_order = sorted(spike_stats.keys(), key=lambda n: next(
    (d for d, nm in ACTIVITY_LABELS.items() if nm == n), 0))
activity_summary = []
for name in activity_order:
    s = spike_stats[name]
    diff = next((d for d, nm in ACTIVITY_LABELS.items() if nm == name), 0)
    daily_min = DAILY_MINUTES.get(round(diff, 2), 60)
    extrap = s["meanCmeRate"] * daily_min * 60
    activity_summary.append({
        "name": name,
        "complexity": diff,
        "windows": s["windowCount"],
        "meanPFlow": round(s["meanPFlow"], 4),
        "cmeRateVnPerS": round(s["meanCmeRate"], 2),
        "totalCmeVn": round(s["totalCmeVn"], 2),
        "dailyMinutes": daily_min,
        "extrapolatedDailyVn": round(extrap, 0),
    })
    print(f"  {name:25s}: {s['windowCount']:3d} win, "
          f"pFlow={s['meanPFlow']:.3f}, rate={s['meanCmeRate']:.2f} Vn/s, "
          f"total={s['totalCmeVn']:.0f} Vn, daily={extrap/1000:.1f}K")

total_cme = sum(s["totalCmeVn"] for s in spike_stats.values())
total_daily = sum(a["extrapolatedDailyVn"] for a in activity_summary)
rates = [s["meanCmeRate"] for s in spike_stats.values()]
rate_ratio = max(rates) / min(rates) if min(rates) > 0 else 0
print(f"\n  Total CME: {total_cme:.0f} Vn")
print(f"  Extrapolated daily: {total_daily/1000:.0f}K Vn")
print(f"  Rate ratio (max/min): {rate_ratio:.2f}x")

# Load trained VQC params
print("\n=== Loading trained VQC parameters ===")
trained_params = None
params_file = os.path.join(OUT, "trained_vqc_params.json")
if os.path.exists(params_file):
    with open(params_file) as f:
        params_data = json.load(f)
    trained_params = params_data["params"]
    print(f"  Loaded {len(trained_params)} params, fitness={params_data['fitness']:.4f}")
else:
    print("  WARNING: No trained_vqc_params.json found")

# ── 5. Run all windows through Aer simulator ──────────────────
print(f"\n=== Running Aer simulator on all {len(windows)} windows ===")
batch_body = {
    "samples": [
        {"features": w["features_8"], "modelType": "QSVC", "trainedParams": trained_params}
        for w in windows
    ]
}
batch_resp = requests.post(f"{QPU}/qpu/infer-batch", json=batch_body, timeout=600).json()
sim_results = batch_resp["results"]
print(f"  {len(sim_results)} results in {batch_resp['totalMs']}ms")

for i, w in enumerate(windows):
    w["simPFlow"] = sim_results[i]["pFlow"]
    w["simShotsUsed"] = sim_results[i]["shotsUsed"]
    w["simDepth"] = sim_results[i]["depth"]
    w["simLatencyMs"] = sim_results[i]["qpuLatencyMs"]

# ── 6. Compute classification metrics ─────────────────────────
def compute_metrics(labels, probs):
    preds = [p >= 0.5 for p in probs]
    tp = sum(1 for l, p in zip(labels, preds) if l and p)
    fp = sum(1 for l, p in zip(labels, preds) if not l and p)
    fn = sum(1 for l, p in zip(labels, preds) if l and not p)
    tn = sum(1 for l, p in zip(labels, preds) if not l and not p)
    n = len(labels)
    acc = (tp + tn) / n if n else 0
    prec = tp / (tp + fp) if (tp + fp) > 0 else 0
    rec = tp / (tp + fn) if (tp + fn) > 0 else 0
    f1 = 2 * prec * rec / (prec + rec) if (prec + rec) > 0 else 0

    sorted_pairs = sorted(zip(probs, labels), key=lambda x: -x[0])
    tp_sum, fp_sum = 0, 0
    total_pos = sum(1 for l in labels if l)
    total_neg = n - total_pos
    pts = [(0, 0)]
    for prob, lab in sorted_pairs:
        if lab:
            tp_sum += 1
        else:
            fp_sum += 1
        tpr = tp_sum / total_pos if total_pos > 0 else 0
        fpr = fp_sum / total_neg if total_neg > 0 else 0
        pts.append((fpr, tpr))
    auroc = sum((x2 - x1) * (y1 + y2) / 2 for (x1, y1), (x2, y2) in zip(pts, pts[1:]))

    return {
        "accuracy": round(acc, 4), "f1": round(f1, 4), "auroc": round(auroc, 4),
        "tp": tp, "fp": fp, "fn": fn, "tn": tn,
    }

has_labels = [w for w in windows if w["flowLabel"] is not None]
if has_labels:
    labels = [w["flowLabel"] for w in has_labels]
    q_probs = [w["simPFlow"] for w in has_labels]
    c_probs = [w["classicalPFlow"] if w["classicalPFlow"] is not None else 0.5 for w in has_labels]
    h_probs = [0.6 * q + 0.4 * c for q, c in zip(q_probs, c_probs)]

    q_metrics = compute_metrics(labels, q_probs)
    c_metrics = compute_metrics(labels, c_probs)
    h_metrics = compute_metrics(labels, h_probs)

    print(f"\n=== Classification Metrics (n={len(has_labels)} labeled) ===")
    print(f"  Quantum:   acc={q_metrics['accuracy']}, F1={q_metrics['f1']}, AUROC={q_metrics['auroc']}")
    print(f"  Classical: acc={c_metrics['accuracy']}, F1={c_metrics['f1']}, AUROC={c_metrics['auroc']}")
    print(f"  Hybrid:    acc={h_metrics['accuracy']}, F1={h_metrics['f1']}, AUROC={h_metrics['auroc']}")

    q_var = float(np.var(q_probs))
    h_var = float(np.var(h_probs))
    var_reduction = (q_var - h_var) / q_var * 100 if q_var > 0 else 0
    print(f"  pFlow var: quantum={q_var:.4f}, hybrid={h_var:.4f}, reduction={var_reduction:.1f}%")
else:
    q_metrics = c_metrics = h_metrics = {"accuracy": 0, "f1": 0, "auroc": 0}
    q_var = h_var = var_reduction = 0
    print("\n  No flow labels; metrics unavailable")

# ── 7. Per-activity sim pFlow ─────────────────────────────────
print("\n=== Per-Activity Simulator pFlow ===")
per_act_sim = {}
for name in activity_order:
    act_wins = activity_data[name]
    pflows = [w["simPFlow"] for w in act_wins]
    per_act_sim[name] = {
        "mean": round(float(np.mean(pflows)), 4),
        "std": round(float(np.std(pflows)), 4),
        "windows": len(pflows),
    }
    print(f"  {name:25s}: simPFlow={np.mean(pflows):.3f} +/- {np.std(pflows):.3f}")

# ── 8. Prepare 100-window stratified sample for IBM Kingston ──
print(f"\n=== Preparing IBM Kingston sample ===")
np.random.seed(42)

ibm_sample = []
n_per_activity = 100 // len(activity_data)  # 12 per activity, 4 remainder
remainder = 100 - n_per_activity * len(activity_data)

for name in activity_order:
    act_wins = activity_data[name]
    n = n_per_activity + (1 if remainder > 0 else 0)
    if remainder > 0:
        remainder -= 1
    chosen_idx = np.random.choice(len(act_wins), size=min(n, len(act_wins)), replace=False)
    for idx in chosen_idx:
        w = act_wins[idx]
        ibm_sample.append({
            "windowId": w["windowId"],
            "actionType": name,
            "features_8": w["features_8"],
            "classicalPFlow": w["classicalPFlow"],
            "flowLabel": w["flowLabel"],
            "simPFlow": w["simPFlow"],
            "difficulty": w["difficulty"],
        })

np.random.shuffle(ibm_sample)
act_counts = defaultdict(int)
for s in ibm_sample:
    act_counts[s["actionType"]] += 1
print(f"  IBM sample: {len(ibm_sample)} windows")
for act, cnt in sorted(act_counts.items()):
    print(f"    {act}: {cnt}")

with open(os.path.join(OUT, "ibm_quantum_sample.json"), "w") as f:
    json.dump(ibm_sample, f, indent=2)
print(f"  Saved to ibm_quantum_sample.json")

# ── 9. Latency stats ──────────────────────────────────────────
latencies = [w["simLatencyMs"] for w in windows]

# ── 10. Save all results ──────────────────────────────────────
all_sim_pflows = [w["simPFlow"] for w in windows]
results = {
    "totalWindows": len(windows),
    "activities": len(activity_data),
    "sessionId": SESSION_ID,
    "perActivity": activity_summary,
    "perActivitySimPFlow": per_act_sim,
    "metrics": {
        "quantum": q_metrics,
        "classical": c_metrics,
        "hybrid": h_metrics,
        "labeledWindows": len(has_labels),
        "varianceReduction": round(var_reduction, 1),
    },
    "pFlowStats": {
        "simMean": round(float(np.mean(all_sim_pflows)), 4),
        "simStd": round(float(np.std(all_sim_pflows)), 4),
        "simVar": round(float(np.var(all_sim_pflows)), 6),
    },
    "latency": {
        "meanMs": round(float(np.mean(latencies)), 1),
        "medianMs": round(float(np.median(latencies)), 1),
        "p95Ms": round(float(np.percentile(latencies, 95)), 1),
    },
    "trainedModel": {
        "algorithm": "genetic",
        "fitness": params_data.get("fitness") if trained_params else None,
        "paramCount": len(trained_params) if trained_params else 0,
    },
    "totals": {
        "totalCmeVn": round(total_cme, 0),
        "extrapolatedDailyVn": round(total_daily, 0),
        "rateRatio": round(rate_ratio, 2),
        "maxRate": round(max(rates), 2),
        "minRate": round(min(rates), 2),
    },
    "ibmSampleSize": len(ibm_sample),
    "measurementProtocol": {
        "activitiesRecorded": 8,
        "windowsPerActivity": 36,
        "windowDuration_s": 5,
        "totalRecording_min": 24,
    },
}

with open(os.path.join(OUT, "real_data_analysis.json"), "w") as f:
    json.dump(results, f, indent=2)
print(f"\nSaved analysis to real_data_analysis.json")
print("\nDone. Next: run run_ibm_quantum_batch.py for real Kingston QPU execution.")
