"""Train VQC parameters using genetic algorithm against Aer simulator."""
import json, os, requests, numpy as np
from collections import defaultdict

API = "http://localhost:5000/api"
QPU = "http://localhost:8001"
OUT = os.path.join(os.path.dirname(__file__), "results")
SESSION_ID = "9f0851fe-e620-4218-8e12-de4065fe84d3"
NUM_PARAMS = 24; POP_SIZE = 20; GENERATIONS = 30; MUTATION_RATE = 0.15; ELITE_FRAC = 0.2

print("=== VQC Training Script ===")
all_windows = requests.get(f"{API}/dataset/windows?labeled=false&limit=600", timeout=60).json()
windows = [w for w in all_windows if w.get("actionSpikeId") and w.get("sessionId") == SESSION_ID]
print(f"  Total windows: {len(windows)}")

data = []
for w in windows:
    feats = w.get("features", [])
    if len(feats) < 20 or w.get("flowLabel") is None: continue
    avg_d = np.mean(feats[0:20:5]); avg_t = np.mean(feats[1:20:5]); avg_a = np.mean(feats[2:20:5])
    avg_b = np.mean(feats[3:20:5]); avg_g = np.mean(feats[4:20:5])
    fa = feats[12] - feats[7]; eng = avg_b / avg_t if avg_t > 0 else 0.5; diff = w.get("taskDifficulty", 0.5)
    f8 = [float(x) for x in [avg_d, avg_t, avg_a, avg_b, avg_g, fa, eng, diff]]
    mx = max(abs(x) for x in f8) or 1.0
    data.append({"features": [x/mx for x in f8], "label": w["flowLabel"]})

flow = [d for d in data if d["label"]]; nonflow = [d for d in data if not d["label"]]
print(f"  Labeled: {len(data)} ({len(flow)} flow, {len(nonflow)} non-flow)")
np.random.seed(42)
n_each = min(len(flow), len(nonflow), 50)
tf = [flow[i] for i in np.random.choice(len(flow), n_each, replace=False)]
tnf = [nonflow[i] for i in np.random.choice(len(nonflow), n_each, replace=False)]
train_data = tf + tnf; np.random.shuffle(train_data)
print(f"  Training set: {len(train_data)} ({n_each} flow + {n_each} non-flow)")

def evaluate(params):
    body = {"samples": [{"features": d["features"], "modelType": "QSVC", "trainedParams": list(params)} for d in train_data]}
    try:
        resp = requests.post(f"{QPU}/qpu/infer-batch", json=body, timeout=120).json()
        results = resp["results"]
        correct = sum(1 for d, r in zip(train_data, results) if (r["pFlow"] >= 0.5) == d["label"])
        acc = correct / len(train_data)
        fp = [r["pFlow"] for d, r in zip(train_data, results) if d["label"]]
        nfp = [r["pFlow"] for d, r in zip(train_data, results) if not d["label"]]
        sep = np.mean(fp) - np.mean(nfp) if fp and nfp else 0
        return acc + 0.1 * max(0, sep)
    except Exception as e:
        print(f"  Eval error: {e}"); return 0.0

print(f"\n--- GA: {POP_SIZE} pop, {GENERATIONS} gens ---")
population = [np.random.uniform(-np.pi, np.pi, NUM_PARAMS) for _ in range(POP_SIZE)]
best_params = None; best_fitness = 0.0

for gen in range(GENERATIONS):
    fitnesses = [evaluate(ind) for ind in population]
    ranked = sorted(zip(fitnesses, population), key=lambda x: -x[0])
    if ranked[0][0] > best_fitness:
        best_fitness = ranked[0][0]; best_params = ranked[0][1].copy()
    print(f"  Gen {gen+1:2d}/{GENERATIONS}: best={ranked[0][0]:.4f} mean={np.mean(fitnesses):.4f} overall={best_fitness:.4f}")
    n_elite = max(2, int(POP_SIZE * ELITE_FRAC))
    new_pop = [ind.copy() for _, ind in ranked[:n_elite]]
    while len(new_pop) < POP_SIZE:
        p1 = ranked[np.random.randint(0, n_elite)][1]; p2 = ranked[np.random.randint(0, len(ranked))][1]
        cp = np.random.randint(1, NUM_PARAMS)
        child = np.concatenate([p1[:cp], p2[cp:]])
        mask = np.random.random(NUM_PARAMS) < MUTATION_RATE
        child[mask] += np.random.normal(0, 0.3, mask.sum())
        new_pop.append(child)
    population = new_pop[:POP_SIZE]

print(f"\n=== Training Complete: fitness={best_fitness:.4f} ===")

# Evaluate on full dataset
body = {"samples": [{"features": d["features"], "modelType": "QSVC", "trainedParams": best_params.tolist()} for d in data]}
resp = requests.post(f"{QPU}/qpu/infer-batch", json=body, timeout=120).json()
results = resp["results"]
correct = sum(1 for d, r in zip(data, results) if (r["pFlow"] >= 0.5) == d["label"])
full_acc = correct / len(data)
fp = [r["pFlow"] for d, r in zip(data, results) if d["label"]]
nfp = [r["pFlow"] for d, r in zip(data, results) if not d["label"]]
print(f"  Full acc: {full_acc:.4f}, Flow pFlow: {np.mean(fp):.4f}, Non-flow pFlow: {np.mean(nfp):.4f}")

params_file = os.path.join(OUT, "trained_vqc_params.json")
with open(params_file, "w") as f:
    json.dump({"params": best_params.tolist(), "fitness": best_fitness, "fullDatasetAccuracy": full_acc,
               "flowMeanPFlow": float(np.mean(fp)), "nonflowMeanPFlow": float(np.mean(nfp)),
               "trainSize": len(train_data), "totalLabeled": len(data), "generations": GENERATIONS}, f, indent=2)
print(f"  Saved to {params_file}")
