"""
Daily-activity CME experiment: synthetic EEG generation, VQC vs classical NN
inference, CME computation in Vernik (Vn), and comparison graphs.

Usage:
    python run_daily_experiment.py                  # Aer simulator only
    python run_daily_experiment.py --ibm-token TOKEN  # also run on IBM hardware
"""

from __future__ import annotations

import argparse
import csv
import time
from dataclasses import dataclass, field
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from scipy.optimize import minimize
from sklearn.neural_network import MLPClassifier
from sklearn.model_selection import train_test_split
from sklearn.metrics import accuracy_score, f1_score, roc_auc_score

from qiskit import QuantumCircuit
from qiskit.circuit import ParameterVector

RESULTS_DIR = Path(__file__).parent / "results"
RESULTS_DIR.mkdir(exist_ok=True)

SEED = 42
RNG = np.random.default_rng(SEED)
WINDOW_S = 5.0
KAPPA = 1.0
LAMBDA = np.array([0.5, 0.5, 0.5])
BAND_WEIGHTS = np.array([0.5, 1.0, 1.0, 0.3, 0.0])
NUM_CHANNELS = 4
NUM_BANDS = 5
SHOTS = 1024
MU_HYBRID = 0.6


@dataclass
class Activity:
    name: str
    duration_min: float
    complexity: float
    band_means: np.ndarray  # shape (5,) – delta, theta, alpha, beta, gamma
    band_stds: np.ndarray
    flow_prob_base: float


ACTIVITIES = [
    Activity("Morning routine", 30, 0.10,
             np.array([3.0, 2.5, 5.0, 1.5, 0.3]),
             np.array([0.5, 0.4, 0.8, 0.3, 0.1]), 0.10),
    Activity("Commute", 30, 0.15,
             np.array([2.5, 2.0, 6.0, 1.0, 0.2]),
             np.array([0.4, 0.3, 1.0, 0.2, 0.1]), 0.12),
    Activity("Deep coding 1", 120, 0.80,
             np.array([1.5, 4.5, 3.0, 6.0, 1.0]),
             np.array([0.3, 0.7, 0.5, 1.0, 0.2]), 0.72),
    Activity("Reading novel", 60, 0.25,
             np.array([2.0, 2.0, 5.5, 2.0, 0.3]),
             np.array([0.4, 0.3, 0.9, 0.4, 0.1]), 0.30),
    Activity("Lunch break", 60, 0.05,
             np.array([3.5, 2.0, 7.0, 0.8, 0.2]),
             np.array([0.6, 0.3, 1.2, 0.2, 0.1]), 0.05),
    Activity("Admin email", 60, 0.20,
             np.array([2.0, 2.5, 4.0, 2.5, 0.4]),
             np.array([0.4, 0.4, 0.7, 0.5, 0.1]), 0.18),
    Activity("Deep coding 2", 120, 0.80,
             np.array([1.5, 4.5, 3.0, 6.0, 1.0]),
             np.array([0.3, 0.7, 0.5, 1.0, 0.2]), 0.72),
    Activity("Evening relaxation", 90, 0.10,
             np.array([3.0, 2.0, 6.5, 1.0, 0.2]),
             np.array([0.5, 0.3, 1.0, 0.2, 0.1]), 0.08),
]


def generate_windows() -> tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray, list[str]]:
    """Generate synthetic EEG windows for the full day.

    Returns (full_features, reduced_features, labels, complexities, activity_names)
    where full_features is (N, 22), reduced_features is (N, 8), labels is (N,).
    """
    all_full, all_reduced, all_labels, all_c, all_names = [], [], [], [], []
    for act in ACTIVITIES:
        n_windows = int(act.duration_min * 60 / WINDOW_S)
        for _ in range(n_windows):
            ch_powers = np.zeros((NUM_CHANNELS, NUM_BANDS))
            for ch in range(NUM_CHANNELS):
                ch_powers[ch] = np.abs(
                    RNG.normal(act.band_means, act.band_stds)
                )
            full_vec = ch_powers.flatten()  # 20 values
            c_val = act.complexity + RNG.normal(0, 0.02)
            c_val = np.clip(c_val, 0, 1)
            q_val = 0.85 + RNG.normal(0, 0.05)
            q_val = np.clip(q_val, 0, 1)
            full_vec = np.concatenate([full_vec, [c_val, q_val]])  # 22

            mean_bands = ch_powers.mean(axis=0)  # 5 values
            frontal_asym = abs(ch_powers[1, 2] - ch_powers[2, 2])
            engagement = mean_bands[3] / max(mean_bands[1], 1e-6)
            reduced_vec = np.concatenate([mean_bands, [frontal_asym, engagement, c_val]])

            label = int(RNG.random() < act.flow_prob_base)

            all_full.append(full_vec)
            all_reduced.append(reduced_vec)
            all_labels.append(label)
            all_c.append(c_val)
            all_names.append(act.name)

    return (np.array(all_full), np.array(all_reduced),
            np.array(all_labels), np.array(all_c), all_names)


def build_vqc(num_qubits: int = 4, layers: int = 2) -> QuantumCircuit:
    """Replicates the VQC from the paper."""
    x = ParameterVector("x", 8)
    theta = ParameterVector("th", 2 * num_qubits * (layers + 1))
    qc = QuantumCircuit(num_qubits, num_qubits)
    idx = 0
    for _ in range(layers + 1):
        for q in range(num_qubits):
            qc.ry((x[q] + 1.0), q)
            qc.rz((x[q + 4] + 1.0), q)
        ring_pairs = [(0, 1), (1, 2), (2, 3), (3, 0)]
        for a, b in ring_pairs:
            qc.rzz((x[a] - x[b]) ** 2, a, b)
        for a, b in ring_pairs:
            qc.cx(a, b)
        for q in range(num_qubits):
            qc.ry(theta[idx], q)
            idx += 1
            qc.rz(theta[idx], q)
            idx += 1
        qc.barrier()
    qc.measure(range(num_qubits), range(num_qubits))
    return qc


def run_vqc_batch_aer(
    circuit: QuantumCircuit,
    data: np.ndarray,
    theta_vals: np.ndarray,
    shots: int = SHOTS,
) -> tuple[np.ndarray, float]:
    """Run VQC on Aer simulator for a batch. Returns (p_flow_array, total_seconds)."""
    from qiskit_aer import AerSimulator

    backend = AerSimulator()
    x_params = [p for p in circuit.parameters if p.name.startswith("x")]
    th_params = [p for p in circuit.parameters if p.name.startswith("th")]
    x_params_sorted = sorted(x_params, key=lambda p: p.index)
    th_params_sorted = sorted(th_params, key=lambda p: p.index)

    p_flows = np.zeros(len(data))
    t0 = time.perf_counter()
    for i, row in enumerate(data):
        bind_dict = {}
        for j, p in enumerate(x_params_sorted):
            bind_dict[p] = float(row[j])
        for j, p in enumerate(th_params_sorted):
            bind_dict[p] = float(theta_vals[j])
        bound = circuit.assign_parameters(bind_dict)
        result = backend.run(bound, shots=shots).result()
        counts = result.get_counts()
        flow_count = sum(v for k, v in counts.items() if k[-1] == "1")
        p_flows[i] = flow_count / shots
    elapsed = time.perf_counter() - t0
    return p_flows, elapsed


def run_vqc_batch_ibm(
    circuit: QuantumCircuit,
    data: np.ndarray,
    theta_vals: np.ndarray,
    token: str,
    shots: int = SHOTS,
) -> tuple[np.ndarray, float]:
    """Run VQC on IBM real hardware for a small batch."""
    from qiskit_ibm_runtime import QiskitRuntimeService, SamplerV2

    service = QiskitRuntimeService(channel="ibm_quantum", token=token)
    backend = service.least_busy(min_num_qubits=4, operational=True)
    print(f"  IBM backend selected: {backend.name}")
    sampler = SamplerV2(mode=backend)

    x_params = sorted(
        [p for p in circuit.parameters if p.name.startswith("x")],
        key=lambda p: p.index,
    )
    th_params = sorted(
        [p for p in circuit.parameters if p.name.startswith("th")],
        key=lambda p: p.index,
    )

    p_flows = np.zeros(len(data))
    t0 = time.perf_counter()
    for i, row in enumerate(data):
        bind_dict = {}
        for j, p in enumerate(x_params):
            bind_dict[p] = float(row[j])
        for j, p in enumerate(th_params):
            bind_dict[p] = float(theta_vals[j])
        bound = circuit.assign_parameters(bind_dict)
        job = sampler.run([bound], shots=shots)
        result = job.result()
        pub_result = result[0]
        counts = pub_result.data.meas.get_counts()
        flow_count = sum(v for k, v in counts.items() if k[-1] == "1")
        p_flows[i] = flow_count / shots
    elapsed = time.perf_counter() - t0
    return p_flows, elapsed


def train_vqc_cobyla(
    circuit: QuantumCircuit,
    X_train: np.ndarray,
    y_train: np.ndarray,
    n_params: int = 24,
    max_subset: int = 120,
    maxiter: int = 60,
) -> np.ndarray:
    """Train VQC theta parameters using COBYLA on a small subset."""
    from qiskit_aer import AerSimulator
    backend = AerSimulator()

    if len(X_train) > max_subset:
        idx = RNG.choice(len(X_train), max_subset, replace=False)
        X_sub, y_sub = X_train[idx], y_train[idx]
    else:
        X_sub, y_sub = X_train, y_train

    x_params = sorted(
        [p for p in circuit.parameters if p.name.startswith("x")],
        key=lambda p: p.index,
    )
    th_params = sorted(
        [p for p in circuit.parameters if p.name.startswith("th")],
        key=lambda p: p.index,
    )

    call_count = [0]

    def objective(theta_flat):
        call_count[0] += 1
        loss = 0.0
        for row, label in zip(X_sub, y_sub):
            bind_dict = {}
            for j, p in enumerate(x_params):
                bind_dict[p] = float(row[j])
            for j, p in enumerate(th_params):
                bind_dict[p] = float(theta_flat[j])
            bound = circuit.assign_parameters(bind_dict)
            result = backend.run(bound, shots=256).result()
            counts = result.get_counts()
            flow_count = sum(v for k, v in counts.items() if k[-1] == "1")
            p = flow_count / 256
            loss += (p - label) ** 2
        loss /= len(X_sub)
        if call_count[0] % 10 == 0:
            print(f"    COBYLA iter {call_count[0]}: loss={loss:.4f}")
        return loss

    theta0 = RNG.uniform(-np.pi, np.pi, n_params)
    print(f"  Training VQC on {len(X_sub)} windows, maxiter={maxiter} ...")
    res = minimize(objective, theta0, method="COBYLA",
                   options={"maxiter": maxiter, "rhobeg": 0.5})
    print(f"  Training done – final loss: {res.fun:.4f}, evals: {res.nfev}")
    return res.x


def train_classical_nn(X_train, y_train) -> MLPClassifier:
    clf = MLPClassifier(
        hidden_layer_sizes=(64, 32),
        activation="relu",
        max_iter=500,
        random_state=SEED,
    )
    clf.fit(X_train, y_train)
    return clf


def compute_cme(e_band: np.ndarray, p_flow: np.ndarray, c: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    g = LAMBDA[0] * c + LAMBDA[1] * p_flow + LAMBDA[2] * c * p_flow
    cme_rate = KAPPA * e_band * g
    cme = cme_rate * WINDOW_S
    return cme_rate, cme


def compute_e_band(full_features: np.ndarray) -> np.ndarray:
    """Compute E_band from full 22-dim features (first 20 = 4ch x 5bands)."""
    n = len(full_features)
    e_band = np.zeros(n)
    for i in range(n):
        ch_bands = full_features[i, :20].reshape(NUM_CHANNELS, NUM_BANDS)
        e_band[i] = np.sum(ch_bands * BAND_WEIGHTS[np.newaxis, :])
    return e_band


def plot_daily_cme_trace(cme_vals, activity_names, filename="daily_cme_trace.png"):
    unique_acts = []
    for n in activity_names:
        if n not in unique_acts:
            unique_acts.append(n)
    cmap = matplotlib.colormaps.get_cmap("tab10").resampled(len(unique_acts))
    color_map = {name: cmap(i) for i, name in enumerate(unique_acts)}

    fig, ax = plt.subplots(figsize=(14, 5))
    cumulative = np.cumsum(cme_vals)
    time_min = np.arange(len(cme_vals)) * WINDOW_S / 60.0

    prev_name = None
    seg_start = 0
    for i, name in enumerate(activity_names + [None]):
        if name != prev_name and prev_name is not None:
            seg = slice(seg_start, i)
            ax.plot(time_min[seg], cumulative[seg],
                    color=color_map[prev_name], linewidth=1.2, label=prev_name)
            seg_start = i
        prev_name = name

    handles, labels = ax.get_legend_handles_labels()
    seen = set()
    unique_handles = []
    for h, l in zip(handles, labels):
        if l not in seen:
            seen.add(l)
            unique_handles.append((h, l))
    ax.legend(*zip(*unique_handles), fontsize=8, loc="upper left")
    ax.set_xlabel("Time (minutes)")
    ax.set_ylabel("Cumulative CME (Vn)")
    ax.set_title("Daily CME Trace – Hybrid Mode")
    ax.grid(True, alpha=0.3)
    fig.tight_layout()
    fig.savefig(RESULTS_DIR / filename, dpi=200, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved {filename}")


def plot_pflow_comparison(pf_quantum, pf_classical, pf_hybrid, activity_names,
                          filename="quantum_vs_classical_pflow.png"):
    unique_acts = []
    for n in activity_names:
        if n not in unique_acts:
            unique_acts.append(n)

    q_means = [np.mean(pf_quantum[np.array(activity_names) == a]) for a in unique_acts]
    c_means = [np.mean(pf_classical[np.array(activity_names) == a]) for a in unique_acts]
    h_means = [np.mean(pf_hybrid[np.array(activity_names) == a]) for a in unique_acts]

    x = np.arange(len(unique_acts))
    w = 0.25
    fig, ax = plt.subplots(figsize=(12, 5))
    ax.bar(x - w, q_means, w, label="Quantum")
    ax.bar(x, c_means, w, label="Classical")
    ax.bar(x + w, h_means, w, label="Hybrid")
    ax.set_xticks(x)
    ax.set_xticklabels(unique_acts, rotation=25, ha="right", fontsize=8)
    ax.set_ylabel("Mean $p_{flow}$")
    ax.set_title("Flow Probability by Activity and Inference Mode")
    ax.legend()
    ax.grid(True, alpha=0.3, axis="y")
    fig.tight_layout()
    fig.savefig(RESULTS_DIR / filename, dpi=200, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved {filename}")


def plot_vn_per_activity(cme_vals, activity_names, filename="vn_per_activity.png"):
    unique_acts = []
    for n in activity_names:
        if n not in unique_acts:
            unique_acts.append(n)

    totals = [np.sum(cme_vals[np.array(activity_names) == a]) for a in unique_acts]

    fig, ax = plt.subplots(figsize=(10, 5))
    bars = ax.bar(unique_acts, totals, color=plt.cm.tab10(np.linspace(0, 1, len(unique_acts))))
    for bar, val in zip(bars, totals):
        ax.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 5,
                f"{val:.0f}", ha="center", va="bottom", fontsize=8)
    ax.set_ylabel("Total CME (Vn)")
    ax.set_title(f"CME Consumption per Activity – Daily Total: {sum(totals):.0f} Vn")
    ax.set_xticklabels(unique_acts, rotation=25, ha="right", fontsize=8)
    ax.grid(True, alpha=0.3, axis="y")
    fig.tight_layout()
    fig.savefig(RESULTS_DIR / filename, dpi=200, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved {filename}")


def plot_accuracy_comparison(metrics: dict, filename="accuracy_comparison.png"):
    modes = list(metrics.keys())
    acc_vals = [metrics[m]["accuracy"] for m in modes]
    f1_vals = [metrics[m]["f1"] for m in modes]

    x = np.arange(len(modes))
    w = 0.3
    fig, ax = plt.subplots(figsize=(7, 5))
    ax.bar(x - w / 2, acc_vals, w, label="Accuracy")
    ax.bar(x + w / 2, f1_vals, w, label="F1 Score")
    ax.set_xticks(x)
    ax.set_xticklabels(modes)
    ax.set_ylabel("Score")
    ax.set_title("Classification Performance: Quantum vs Classical vs Hybrid")
    ax.legend()
    ax.set_ylim(0, 1.05)
    ax.grid(True, alpha=0.3, axis="y")
    fig.tight_layout()
    fig.savefig(RESULTS_DIR / filename, dpi=200, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved {filename}")


def plot_latency_comparison(latencies: dict, filename="latency_comparison.png"):
    fig, ax = plt.subplots(figsize=(7, 5))
    modes = list(latencies.keys())
    vals = [latencies[m] for m in modes]
    bars = ax.bar(modes, vals, color=["#4c72b0", "#dd8452", "#55a868"])
    for bar, val in zip(bars, vals):
        ax.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 0.1,
                f"{val:.1f}", ha="center", va="bottom", fontsize=9)
    ax.set_ylabel("Median latency per window (ms)")
    ax.set_title("Inference Latency Comparison")
    ax.grid(True, alpha=0.3, axis="y")
    fig.tight_layout()
    fig.savefig(RESULTS_DIR / filename, dpi=200, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved {filename}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ibm-token", type=str, default=None)
    args = parser.parse_args()

    print("=== Generating synthetic EEG windows ===")
    full_feat, reduced_feat, labels, complexities, act_names = generate_windows()
    n_total = len(labels)
    print(f"  Total windows: {n_total}")
    print(f"  Total simulated time: {n_total * WINDOW_S / 3600:.1f} hours")
    print(f"  Flow-positive windows: {labels.sum()} ({labels.mean() * 100:.1f}%)")

    e_band = compute_e_band(full_feat)

    print("\n=== Training classical NN ===")
    X_train, X_test, y_train, y_test, idx_train, idx_test = train_test_split(
        full_feat, labels, np.arange(n_total), test_size=0.3, random_state=SEED, stratify=labels
    )
    clf = train_classical_nn(X_train, y_train)
    nn_probs_all = clf.predict_proba(full_feat)[:, 1]
    nn_preds_test = clf.predict(X_test)
    nn_probs_test = clf.predict_proba(X_test)[:, 1]
    t0 = time.perf_counter()
    _ = clf.predict_proba(full_feat)
    nn_total_time = time.perf_counter() - t0
    print(f"  Classical NN – Accuracy: {accuracy_score(y_test, nn_preds_test):.4f}, "
          f"F1: {f1_score(y_test, nn_preds_test):.4f}")

    print("\n=== Training VQC on Aer simulator ===")
    circuit = build_vqc()
    reduced_train = reduced_feat[idx_train]
    theta_vals = train_vqc_cobyla(circuit, reduced_train, y_train)

    print("\n=== Running VQC inference on Aer simulator ===")
    vqc_probs_all, vqc_total_time = run_vqc_batch_aer(circuit, reduced_feat, theta_vals)
    vqc_preds_test = (vqc_probs_all[idx_test] >= 0.5).astype(int)
    vqc_probs_test = vqc_probs_all[idx_test]
    print(f"  VQC (Aer) – Accuracy: {accuracy_score(y_test, vqc_preds_test):.4f}, "
          f"F1: {f1_score(y_test, vqc_preds_test):.4f}")

    hybrid_probs_all = MU_HYBRID * vqc_probs_all + (1 - MU_HYBRID) * nn_probs_all
    hybrid_preds_test = (hybrid_probs_all[idx_test] >= 0.5).astype(int)
    hybrid_probs_test = hybrid_probs_all[idx_test]
    print(f"  Hybrid (mu={MU_HYBRID}) – Accuracy: {accuracy_score(y_test, hybrid_preds_test):.4f}, "
          f"F1: {f1_score(y_test, hybrid_preds_test):.4f}")

    metrics = {
        "Quantum": {
            "accuracy": accuracy_score(y_test, vqc_preds_test),
            "f1": f1_score(y_test, vqc_preds_test),
            "auroc": roc_auc_score(y_test, vqc_probs_test) if len(np.unique(y_test)) > 1 else 0.5,
        },
        "Classical": {
            "accuracy": accuracy_score(y_test, nn_preds_test),
            "f1": f1_score(y_test, nn_preds_test),
            "auroc": roc_auc_score(y_test, nn_probs_test) if len(np.unique(y_test)) > 1 else 0.5,
        },
        "Hybrid": {
            "accuracy": accuracy_score(y_test, hybrid_preds_test),
            "f1": f1_score(y_test, hybrid_preds_test),
            "auroc": roc_auc_score(y_test, hybrid_probs_test) if len(np.unique(y_test)) > 1 else 0.5,
        },
    }

    latency_per_window = {
        "Quantum": (vqc_total_time / n_total) * 1000,
        "Classical": (nn_total_time / n_total) * 1000,
        "Hybrid": ((vqc_total_time + nn_total_time) / n_total) * 1000,
    }

    print("\n=== Computing CME (hybrid mode) ===")
    _, cme_hybrid = compute_cme(e_band, hybrid_probs_all, complexities)
    _, cme_quantum = compute_cme(e_band, vqc_probs_all, complexities)
    _, cme_classical = compute_cme(e_band, nn_probs_all, complexities)

    act_names_arr = np.array(act_names)
    unique_acts = []
    for n in act_names:
        if n not in unique_acts:
            unique_acts.append(n)

    print(f"\n  {'Activity':<22} {'Windows':>8} {'Vn (Hybrid)':>12} {'Vn/s rate':>10}")
    print("  " + "-" * 56)
    for a in unique_acts:
        mask = act_names_arr == a
        total_vn = cme_hybrid[mask].sum()
        rate = cme_hybrid[mask].mean() / WINDOW_S
        print(f"  {a:<22} {mask.sum():>8} {total_vn:>12.1f} {rate:>10.3f}")
    daily_total = cme_hybrid.sum()
    print(f"  {'DAILY TOTAL':<22} {n_total:>8} {daily_total:>12.1f}")

    print("\n=== Generating plots ===")
    plot_daily_cme_trace(cme_hybrid, act_names)
    plot_pflow_comparison(vqc_probs_all, nn_probs_all, hybrid_probs_all, act_names)
    plot_vn_per_activity(cme_hybrid, act_names)
    plot_accuracy_comparison(metrics)
    plot_latency_comparison(latency_per_window)

    print("\n=== Writing summary CSV ===")
    csv_path = RESULTS_DIR / "summary_table.csv"
    with open(csv_path, "w", newline="") as f:
        w = csv.writer(f)
        w.writerow(["Section", "Key", "Value"])
        w.writerow(["Data", "Total windows", n_total])
        w.writerow(["Data", "Total hours", f"{n_total * WINDOW_S / 3600:.1f}"])
        w.writerow(["Data", "Activities", len(unique_acts)])
        w.writerow(["Data", "Flow-positive pct", f"{labels.mean() * 100:.1f}%"])
        for mode, m in metrics.items():
            for k, v in m.items():
                w.writerow([mode, k, f"{v:.4f}"])
        for mode, v in latency_per_window.items():
            w.writerow([mode, "latency_ms_per_window", f"{v:.2f}"])
        for a in unique_acts:
            mask = act_names_arr == a
            w.writerow(["CME", f"{a} total Vn (hybrid)", f"{cme_hybrid[mask].sum():.1f}"])
            w.writerow(["CME", f"{a} Vn/s rate (hybrid)", f"{cme_hybrid[mask].mean() / WINDOW_S:.3f}"])
        w.writerow(["CME", "Daily total Vn (hybrid)", f"{daily_total:.1f}"])
        w.writerow(["CME", "Daily total Vn (quantum)", f"{cme_quantum.sum():.1f}"])
        w.writerow(["CME", "Daily total Vn (classical)", f"{cme_classical.sum():.1f}"])

        if metrics["Hybrid"]["accuracy"] > 0 and metrics["Classical"]["accuracy"] > 0:
            imp_acc = ((metrics["Hybrid"]["accuracy"] - metrics["Classical"]["accuracy"])
                       / metrics["Classical"]["accuracy"] * 100)
            w.writerow(["Improvement", "Hybrid vs Classical accuracy %", f"{imp_acc:+.1f}%"])
        if metrics["Hybrid"]["f1"] > 0 and metrics["Classical"]["f1"] > 0:
            imp_f1 = ((metrics["Hybrid"]["f1"] - metrics["Classical"]["f1"])
                      / metrics["Classical"]["f1"] * 100)
            w.writerow(["Improvement", "Hybrid vs Classical F1 %", f"{imp_f1:+.1f}%"])

        var_q = np.var(vqc_probs_all)
        var_h = np.var(hybrid_probs_all)
        if var_q > 0:
            var_red = (1 - var_h / var_q) * 100
            w.writerow(["Improvement", "Hybrid variance reduction vs quantum %", f"{var_red:.1f}%"])

    print(f"  Saved {csv_path.name}")

    if args.ibm_token:
        print("\n=== Running VQC on IBM hardware (subset of 12 windows per activity = 96 total) ===")
        ibm_indices = []
        for a in unique_acts:
            act_idx = np.where(act_names_arr == a)[0]
            ibm_indices.extend(act_idx[:12].tolist())
        ibm_indices = np.array(ibm_indices)
        ibm_data = reduced_feat[ibm_indices]
        ibm_labels = labels[ibm_indices]

        ibm_probs, ibm_time = run_vqc_batch_ibm(
            circuit, ibm_data, theta_vals, args.ibm_token
        )
        ibm_preds = (ibm_probs >= 0.5).astype(int)
        print(f"  IBM VQC – Accuracy: {accuracy_score(ibm_labels, ibm_preds):.4f}")
        print(f"  IBM VQC – Total time: {ibm_time:.1f}s for {len(ibm_data)} windows")
        print(f"  IBM VQC – Latency per window: {ibm_time / len(ibm_data) * 1000:.0f} ms")

        with open(csv_path, "a", newline="") as f:
            w = csv.writer(f)
            w.writerow(["IBM", "windows_run", len(ibm_data)])
            w.writerow(["IBM", "accuracy", f"{accuracy_score(ibm_labels, ibm_preds):.4f}"])
            w.writerow(["IBM", "total_time_s", f"{ibm_time:.1f}"])
            w.writerow(["IBM", "latency_ms_per_window", f"{ibm_time / len(ibm_data) * 1000:.0f}"])

    print("\n=== Done ===")


if __name__ == "__main__":
    main()
