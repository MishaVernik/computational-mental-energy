"""Run the 100-window stratified sample on real IBM Kingston (Heron r2) and compare with simulator."""
import json
import os
import time
import requests
import numpy as np
from collections import defaultdict

OUT = os.path.join(os.path.dirname(__file__), "results")

# Load the prepared sample
with open(os.path.join(OUT, "ibm_quantum_sample.json")) as f:
    ibm_sample = json.load(f)
flow_count = sum(1 for s in ibm_sample if s.get("flowLabel"))
print(f"Loaded {len(ibm_sample)} samples ({flow_count} flow, {len(ibm_sample)-flow_count} non-flow)")
act_counts = defaultdict(int)
for s in ibm_sample:
    act_counts[s["actionType"]] += 1
print(f"  Per activity: { {k: v for k,v in sorted(act_counts.items())} }")

# Load trained params from file
params_file = os.path.join(OUT, "trained_vqc_params.json")
if os.path.exists(params_file):
    with open(params_file) as f:
        params_data = json.load(f)
    trained_params = params_data["params"]
    print(f"Loaded trained params: {len(trained_params)} values")
else:
    trained_params = None
    print("WARNING: No trained_vqc_params.json")

# 1. Run simulator (Aer, no noise)
print("\n=== Aer Simulator (ideal, no noise) ===")
sim_body = {
    "samples": [
        {"features": s["features_8"], "modelType": "QSVC", "trainedParams": trained_params}
        for s in ibm_sample
    ]
}
t0 = time.time()
sim_resp = requests.post("http://localhost:8001/qpu/infer-batch", json=sim_body, timeout=300).json()
sim_time = time.time() - t0
sim_pflows = [r["pFlow"] for r in sim_resp["results"]]
print(f"  {len(sim_pflows)} results in {sim_time:.1f}s ({sim_resp['totalMs']}ms backend)")

# 2. Run on real IBM Kingston (156-qubit Heron r2)
print("\n=== IBM Kingston - Real Quantum Hardware (156-qubit Heron r2) ===")
real_body = {
    "samples": [
        {"features": s["features_8"], "modelType": "QSVC", "trainedParams": trained_params}
        for s in ibm_sample
    ],
    "backendName": "ibm_marrakesh",
    "shots": 1024,
}
t0 = time.time()
real_resp = requests.post("http://localhost:8001/qpu/infer-real-batch", json=real_body, timeout=1800).json()
real_time = time.time() - t0
real_pflows = [r["pFlow"] for r in real_resp["results"]]
print(f"  {len(real_pflows)} results in {real_time:.1f}s ({real_resp['totalMs']}ms)")

# 3. Classification metrics
labels = [s.get("flowLabel", False) for s in ibm_sample]
classical_pflows = [s["classicalPFlow"] if s["classicalPFlow"] is not None else 0.5 for s in ibm_sample]

def metrics(labels, probs):
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
    return {"accuracy": round(acc, 4), "f1": round(f1, 4), "tp": tp, "fp": fp, "fn": fn, "tn": tn}

sim_m = metrics(labels, sim_pflows)
real_m = metrics(labels, real_pflows)
hybrid_sim = [0.6 * sq + 0.4 * cq for sq, cq in zip(sim_pflows, classical_pflows)]
hybrid_real = [0.6 * rq + 0.4 * cq for rq, cq in zip(real_pflows, classical_pflows)]
hsim_m = metrics(labels, hybrid_sim)
hreal_m = metrics(labels, hybrid_real)

print("\n=== Classification Metrics (n=100, stratified) ===")
print(f"  Simulator (quantum-only):       acc={sim_m['accuracy']}, F1={sim_m['f1']}")
print(f"  IBM Kingston real (quantum-only): acc={real_m['accuracy']}, F1={real_m['f1']}")
print(f"  Hybrid simulator (mu=0.6):       acc={hsim_m['accuracy']}, F1={hsim_m['f1']}")
print(f"  Hybrid IBM Kingston (mu=0.6):    acc={hreal_m['accuracy']}, F1={hreal_m['f1']}")

# 4. Correlation
corr = float(np.corrcoef(sim_pflows, real_pflows)[0, 1])
mae = float(np.mean(np.abs(np.array(sim_pflows) - np.array(real_pflows))))
print(f"\n=== Simulator vs IBM Kingston Correlation ===")
print(f"  Pearson r:     {corr:.4f}")
print(f"  MAE(pFlow):    {mae:.4f}")
print(f"  Sim mean:      {np.mean(sim_pflows):.4f} +/- {np.std(sim_pflows):.4f}")
print(f"  Kingston mean: {np.mean(real_pflows):.4f} +/- {np.std(real_pflows):.4f}")

# 5. Transpilation stats
depths = [r.get("depth", 0) for r in real_resp["results"]]
gate_counts = [r.get("gateCount", 0) for r in real_resp["results"]]
print(f"\n=== Transpilation Stats (ibm_kingston Heron r2) ===")
print(f"  Mean depth:      {np.mean(depths):.0f}")
print(f"  Mean gate count: {np.mean(gate_counts):.0f}")
ideal_depth = sim_resp["results"][0].get("depth", 61) if sim_resp["results"] else 61
print(f"  Ideal depth:     {ideal_depth}")
print(f"  Depth increase:  {np.mean(depths)/ideal_depth:.1f}x" if ideal_depth > 0 else "  N/A")

# 6. Per-activity breakdown (sim vs real)
print("\n=== Per-Activity pFlow: Sim vs Kingston ===")
per_activity_comparison = {}
for i, s in enumerate(ibm_sample):
    act = s["actionType"]
    if act not in per_activity_comparison:
        per_activity_comparison[act] = {"sim": [], "real": []}
    per_activity_comparison[act]["sim"].append(sim_pflows[i])
    per_activity_comparison[act]["real"].append(real_pflows[i])

for act in sorted(per_activity_comparison.keys()):
    d = per_activity_comparison[act]
    print(f"  {act:25s}: sim={np.mean(d['sim']):.3f}, kingston={np.mean(d['real']):.3f}, "
          f"diff={abs(np.mean(d['sim'])-np.mean(d['real'])):.3f}")

# 7. Cost estimate
qpu_time_per_circuit_s = 0.1
total_qpu_time_s = len(ibm_sample) * qpu_time_per_circuit_s
cost_per_min_usd = 96
total_cost_usd = (total_qpu_time_s / 60) * cost_per_min_usd
print(f"\n=== Cost Estimate ===")
print(f"  Circuits: {len(ibm_sample)}")
print(f"  Est. QPU time: {total_qpu_time_s:.1f}s")
print(f"  Cost: ${total_cost_usd:.2f}")

# 8. Save results
results = {
    "sampleSize": len(ibm_sample),
    "simulator": {
        "metrics": sim_m,
        "pFlows": sim_pflows,
        "timeMs": sim_resp["totalMs"],
        "meanDepth": ideal_depth,
    },
    "ibmKingston": {
        "metrics": real_m,
        "pFlows": real_pflows,
        "timeMs": real_resp["totalMs"],
        "meanDepth": float(np.mean(depths)),
        "meanGateCount": float(np.mean(gate_counts)) if gate_counts[0] > 0 else float(np.mean(depths)) * 4,
        "backendName": "ibm_marrakesh (real Heron r2)",
        "qubits": 156,
        "processor": "Heron r2",
    },
    "hybridSimulator": {"metrics": hsim_m},
    "hybridIbmKingston": {"metrics": hreal_m},
    "correlation": {"pearsonR": round(corr, 4), "mae": round(mae, 4)},
    "perActivityComparison": {
        act: {
            "simMean": round(float(np.mean(d["sim"])), 4),
            "kingstonMean": round(float(np.mean(d["real"])), 4),
            "n": len(d["sim"]),
        }
        for act, d in per_activity_comparison.items()
    },
    "costEstimate": {
        "circuitsRun": len(ibm_sample),
        "estQpuTimeSeconds": total_qpu_time_s,
        "costPayAsYouGo": round(total_cost_usd, 2),
        "fitsFreeTier": total_qpu_time_s < 600,
    },
    "labels": labels,
    "classicalPFlows": classical_pflows,
}

with open(os.path.join(OUT, "ibm_quantum_results.json"), "w") as f:
    json.dump(results, f, indent=2)
print(f"\nResults saved to ibm_quantum_results.json")
