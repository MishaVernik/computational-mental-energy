"""Generate all paper figures from real measurement data and IBM QPU results.

All figures are saved WITHOUT titles (titles go in paper captions).
"""
import os
import json
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

OUT = os.path.join(os.path.dirname(__file__), "results")
os.makedirs(OUT, exist_ok=True)

ANALYSIS_FILE = os.path.join(OUT, "real_data_analysis.json")
IBM_RESULTS_FILE = os.path.join(OUT, "ibm_quantum_results.json")
IBM_SAMPLE_FILE = os.path.join(OUT, "ibm_quantum_sample.json")

with open(ANALYSIS_FILE) as f:
    analysis = json.load(f)

COLORS = [
    "#4e79a7", "#59a14f", "#e15759", "#f28e2b",
    "#76b7b2", "#edc948", "#b07aa1", "#9c755f",
]

plt.rcParams.update({
    "font.size": 11, "axes.titlesize": 13, "axes.labelsize": 12,
    "figure.dpi": 300, "savefig.dpi": 300, "savefig.bbox": "tight",
    "font.family": "sans-serif",
})

activities = analysis["perActivity"]
act_names = [a["name"] for a in activities]
act_rates = [a["cmeRateVnPerS"] for a in activities]
act_totals = [a["totalCmeVn"] for a in activities]
act_pflows = [a["meanPFlow"] for a in activities]
act_complexities = [a["complexity"] for a in activities]
act_daily = [a["extrapolatedDailyVn"] for a in activities]
act_daily_min = [a["dailyMinutes"] for a in activities]

# ═══════════════════════════════════════════════════════════════
# Fig 6: Cumulative CME trace across the 24-min recording
# ═══════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(12, 5))
time_min = 0
cum_vn = 0
for i, a in enumerate(activities):
    n_windows = a["windows"]
    rate = a["cmeRateVnPerS"]
    dur_min = n_windows * 5 / 60
    x = np.linspace(time_min, time_min + dur_min, n_windows + 1)
    y = cum_vn + np.arange(n_windows + 1) * rate * 5
    ax.fill_between(x, y, alpha=0.25, color=COLORS[i % len(COLORS)])
    ax.plot(x, y, color=COLORS[i % len(COLORS)], linewidth=2, label=a["name"])
    cum_vn += a["totalCmeVn"]
    time_min += dur_min

ax.set_xlabel("Recording time (minutes)")
ax.set_ylabel("Cumulative CME (Vn)")
ax.yaxis.set_major_formatter(ticker.FuncFormatter(lambda x, _: f"{x/1000:.0f}K"))
ax.legend(loc="upper left", fontsize=8, ncol=2)
ax.grid(alpha=0.3)
fig.savefig(os.path.join(OUT, "daily_cme_trace.png"))
plt.close()
print("  OK daily_cme_trace.png")

# ═══════════════════════════════════════════════════════════════
# Fig 7: CME per activity bar chart
# ═══════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(10, 5))
bars = ax.barh(act_names[::-1], act_totals[::-1],
               color=COLORS[:len(act_names)][::-1], edgecolor="white")
for bar, v in zip(bars, act_totals[::-1]):
    ax.text(bar.get_width() + 500, bar.get_y() + bar.get_height() / 2,
            f"{v/1000:.1f}K Vn", va="center", fontsize=10)
ax.set_xlabel("Total CME (Vn) per 3-min recording")
ax.xaxis.set_major_formatter(ticker.FuncFormatter(lambda x, _: f"{x/1000:.0f}K"))
ax.grid(axis="x", alpha=0.3)
fig.savefig(os.path.join(OUT, "vn_per_activity.png"))
plt.close()
print("  OK vn_per_activity.png")

# ═══════════════════════════════════════════════════════════════
# Fig 8: Mean pFlow per activity (from live quantum inference)
# ═══════════════════════════════════════════════════════════════
sim_pflow_data = analysis.get("perActivitySimPFlow", {})
fig, ax = plt.subplots(figsize=(10, 5))
x = np.arange(len(act_names))
w = 0.35
live_pflows = act_pflows
sim_pflows_list = [sim_pflow_data.get(n, {}).get("mean", 0) for n in act_names]

ax.bar(x - w/2, live_pflows, w, label="Live quantum (Aer)", color="#e15759", edgecolor="white")
ax.bar(x + w/2, sim_pflows_list, w, label="Batch re-inference (Aer)", color="#4e79a7", edgecolor="white")
ax.set_xticks(x)
ax.set_xticklabels(act_names, rotation=35, ha="right", fontsize=9)
ax.set_ylabel("Mean p_flow")
ax.legend()
ax.grid(axis="y", alpha=0.3)
fig.savefig(os.path.join(OUT, "quantum_vs_classical_pflow.png"))
plt.close()
print("  OK quantum_vs_classical_pflow.png")

# ═══════════════════════════════════════════════════════════════
# Fig 9: Accuracy / F1 / AUROC comparison
# ═══════════════════════════════════════════════════════════════
met = analysis["metrics"]
modes = ["Quantum", "Classical", "Hybrid"]
acc_vals = [met["quantum"]["accuracy"], met["classical"]["accuracy"], met["hybrid"]["accuracy"]]
f1_vals = [met["quantum"]["f1"], met["classical"]["f1"], met["hybrid"]["f1"]]
auroc_vals = [met["quantum"]["auroc"], met["classical"]["auroc"], met["hybrid"]["auroc"]]

fig, ax = plt.subplots(figsize=(8, 5))
x = np.arange(len(modes))
w = 0.22
ax.bar(x - w, acc_vals, w, label="Accuracy", color="#4e79a7")
ax.bar(x, f1_vals, w, label="F1 Score", color="#e15759")
ax.bar(x + w, auroc_vals, w, label="AUROC", color="#59a14f")
ax.set_xticks(x)
ax.set_xticklabels(modes)
ax.set_ylabel("Score")
ax.set_ylim(0, 1.1)
ax.legend()
ax.grid(axis="y", alpha=0.3)
for i, (a, f, r) in enumerate(zip(acc_vals, f1_vals, auroc_vals)):
    ax.text(i - w, a + 0.02, f"{a:.3f}", ha="center", fontsize=8)
    ax.text(i, f + 0.02, f"{f:.3f}", ha="center", fontsize=8)
    ax.text(i + w, r + 0.02, f"{r:.3f}", ha="center", fontsize=8)
fig.savefig(os.path.join(OUT, "accuracy_comparison.png"))
plt.close()
print("  OK accuracy_comparison.png")

# ═══════════════════════════════════════════════════════════════
# Fig 10: Latency comparison
# ═══════════════════════════════════════════════════════════════
lat = analysis["latency"]
fig, ax = plt.subplots(figsize=(7, 4))
lat_labels = ["Mean", "Median", "p95"]
lat_vals = [lat["meanMs"], lat["medianMs"], lat["p95Ms"]]
bars = ax.bar(lat_labels, lat_vals, color=["#4e79a7", "#59a14f", "#e15759"],
              edgecolor="white", width=0.5)
for bar, val in zip(bars, lat_vals):
    ax.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 5,
            f"{val:.0f} ms", ha="center", fontsize=10)
ax.set_ylabel("Latency (ms per 5s window)")
ax.set_ylim(0, max(max(lat_vals), 1) * 1.3)
ax.grid(axis="y", alpha=0.3)
fig.savefig(os.path.join(OUT, "latency_comparison.png"))
plt.close()
print("  OK latency_comparison.png")

# ═══════════════════════════════════════════════════════════════
# Fig 15: CME rate vs task complexity (scatter + fit)
# ═══════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(8, 5))
for i, a in enumerate(activities):
    ax.scatter(a["complexity"], a["cmeRateVnPerS"], s=120,
               color=COLORS[i % len(COLORS)], zorder=5, edgecolors="white", linewidths=0.8)
    ax.annotate(a["name"], (a["complexity"], a["cmeRateVnPerS"]),
                textcoords="offset points", xytext=(8, 4), fontsize=8, color="#666")

z = np.polyfit(act_complexities, act_rates, 2)
xs = np.linspace(0, 1, 100)
ax.plot(xs, np.polyval(z, xs), color="#e15759", linewidth=2, alpha=0.7, linestyle="--")
ax.set_xlabel("Task complexity c(t)")
ax.set_ylabel("CME rate (Vn/s)")
ax.grid(alpha=0.3)
fig.savefig(os.path.join(OUT, "cme_rate_by_activity.png"))
plt.close()
print("  OK cme_rate_by_activity.png")

# ═══════════════════════════════════════════════════════════════
# Fig 16: Extrapolated daily CME budget (stacked bar)
# ═══════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(10, 5))
bars = ax.barh(act_names[::-1], [d/1000 for d in act_daily[::-1]],
               color=COLORS[:len(act_names)][::-1], edgecolor="white")
for bar, v in zip(bars, [d/1000 for d in act_daily[::-1]]):
    label = f"{v:.0f}K" if v >= 1 else f"{v*1000:.0f}"
    ax.text(bar.get_width() + 20, bar.get_y() + bar.get_height() / 2,
            f"{label} Vn/day", va="center", fontsize=9)
ax.set_xlabel("Extrapolated daily CME (K Vn)")
ax.grid(axis="x", alpha=0.3)
fig.savefig(os.path.join(OUT, "extrapolated_daily_budget.png"))
plt.close()
print("  OK extrapolated_daily_budget.png")

# ═══════════════════════════════════════════════════════════════
# Figures 11-14: IBM Kingston comparison (if results exist)
# ═══════════════════════════════════════════════════════════════
if os.path.exists(IBM_RESULTS_FILE):
    with open(IBM_RESULTS_FILE) as f:
        ibm = json.load(f)
    with open(IBM_SAMPLE_FILE) as f:
        ibm_sample = json.load(f)

    sim_pflows = ibm["simulator"]["pFlows"]
    backend_key = "ibmKingston" if "ibmKingston" in ibm else "ibmKyiv"
    real_pflows = ibm[backend_key]["pFlows"]
    labels = ibm.get("labels", [s.get("flowLabel", False) for s in ibm_sample])
    classical_pflows = ibm.get("classicalPFlows",
        [s.get("classicalPFlow", 0.5) or 0.5 for s in ibm_sample])
    proc_label = ibm[backend_key].get("processor", "Heron r2")
    n_qubits = ibm[backend_key].get("qubits", 156)

    # ── Fig 11: Scatter sim vs real ───────────────────────────
    pearson_r = ibm["correlation"]["pearsonR"]
    mae_val = ibm["correlation"]["mae"]

    fig, ax = plt.subplots(figsize=(7, 7))
    flow_idx = [i for i, l in enumerate(labels) if l]
    nflow_idx = [i for i, l in enumerate(labels) if not l]
    ax.scatter([sim_pflows[i] for i in flow_idx],
               [real_pflows[i] for i in flow_idx],
               c="#e15759", alpha=0.7, s=50, label="Flow", edgecolors="white", linewidths=0.5)
    ax.scatter([sim_pflows[i] for i in nflow_idx],
               [real_pflows[i] for i in nflow_idx],
               c="#4e79a7", alpha=0.7, s=50, label="Non-flow", edgecolors="white", linewidths=0.5)
    lo = min(min(sim_pflows), min(real_pflows)) - 0.03
    hi = max(max(sim_pflows), max(real_pflows)) + 0.03
    ax.plot([lo, hi], [lo, hi], "k--", alpha=0.4, linewidth=1, label="y = x")
    z = np.polyfit(sim_pflows, real_pflows, 1)
    xs = np.linspace(lo, hi, 100)
    ax.plot(xs, np.polyval(z, xs), color="#59a14f", linewidth=2,
            label=f"Fit: y={z[0]:.2f}x+{z[1]:.2f}")
    ax.set_xlabel("Aer Simulator pFlow")
    ax.set_ylabel(f"IBM Kingston ({proc_label}) pFlow")
    ax.text(0.05, 0.95, f"r = {pearson_r:.4f}\nMAE = {mae_val:.4f}",
            transform=ax.transAxes, fontsize=11, va="top",
            bbox=dict(boxstyle="round,pad=0.3", facecolor="white", alpha=0.8))
    ax.legend(loc="lower right", fontsize=9)
    ax.set_xlim(lo, hi)
    ax.set_ylim(lo, hi)
    ax.set_aspect("equal")
    ax.grid(alpha=0.3)
    fig.savefig(os.path.join(OUT, "sim_vs_kingston_scatter.png"))
    plt.close()
    print("  OK sim_vs_kingston_scatter.png")

    # ── Fig 12: pFlow overlay (3 modes) ──────────────────────
    fig, ax = plt.subplots(figsize=(14, 5))
    wids = np.arange(len(sim_pflows))
    ax.plot(wids, sim_pflows, color="#4e79a7", alpha=0.8, linewidth=1.2,
            label="Aer Simulator")
    ax.plot(wids, real_pflows, color="#e15759", alpha=0.8, linewidth=1.2,
            label=f"IBM Kingston ({proc_label})")
    ax.plot(wids, classical_pflows, color="#59a14f", alpha=0.6, linewidth=1.0,
            linestyle="--", label="Classical NN")
    ax.axhline(0.5, color="gray", linestyle=":", alpha=0.4, label="Decision boundary")
    for i, l in enumerate(labels):
        if l:
            ax.axvspan(i - 0.4, i + 0.4, alpha=0.07, color="#e15759")
    ax.set_xlabel("Window index (n=100, stratified across 8 activities)")
    ax.set_ylabel("pFlow")
    ax.legend(loc="upper right", fontsize=9)
    ax.set_xlim(-1, len(sim_pflows))
    ax.set_ylim(-0.02, 1.02)
    ax.grid(alpha=0.3)
    fig.savefig(os.path.join(OUT, "pflow_overlay_3modes.png"))
    plt.close()
    print("  OK pflow_overlay_3modes.png")

    # ── Fig 13: Accuracy/F1 bar chart (4 conditions) ─────────
    sim_m = ibm["simulator"]["metrics"]
    real_m = ibm[backend_key]["metrics"]
    hsim_m = ibm["hybridSimulator"]["metrics"]
    hreal_key = "hybridIbmKingston" if "hybridIbmKingston" in ibm else "hybridIbmKyiv"
    hreal_m = ibm[hreal_key]["metrics"]

    mode_names = ["Quantum\n(Aer)", "Quantum\n(Kingston)", "Hybrid\n(Aer)", "Hybrid\n(Kingston)"]
    acc_v = [sim_m["accuracy"], real_m["accuracy"], hsim_m["accuracy"], hreal_m["accuracy"]]
    f1_v = [sim_m["f1"], real_m["f1"], hsim_m["f1"], hreal_m["f1"]]

    fig, ax = plt.subplots(figsize=(10, 5))
    x = np.arange(len(mode_names))
    w = 0.3
    b1 = ax.bar(x - w / 2, acc_v, w, label="Accuracy", color="#4e79a7", edgecolor="white")
    b2 = ax.bar(x + w / 2, f1_v, w, label="F1 Score", color="#e15759", edgecolor="white")
    for bars in [b1, b2]:
        for bar in bars:
            ax.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 0.01,
                    f"{bar.get_height():.2f}", ha="center", fontsize=9)
    ax.set_xticks(x)
    ax.set_xticklabels(mode_names, fontsize=10)
    ax.set_ylabel("Score")
    ax.set_ylim(0, 1.1)
    ax.legend()
    ax.grid(axis="y", alpha=0.3)
    fig.savefig(os.path.join(OUT, "accuracy_sim_vs_kingston.png"))
    plt.close()
    print("  OK accuracy_sim_vs_kingston.png")

    # ── Fig 14: Transpilation comparison ──────────────────────
    ideal_depth = ibm["simulator"].get("meanDepth", 61)
    real_depth = ibm[backend_key]["meanDepth"]
    ideal_gates = ideal_depth * 4
    real_gates = ibm[backend_key].get("meanGateCount", real_depth * 4)

    fig, axes = plt.subplots(1, 2, figsize=(10, 5))
    ax1, ax2 = axes

    bars1 = ax1.bar(["Aer (ideal)", f"Kingston\n({proc_label})"],
                     [ideal_depth, real_depth],
                     color=["#4e79a7", "#e15759"], edgecolor="white", width=0.5)
    for b in bars1:
        ax1.text(b.get_x() + b.get_width() / 2, b.get_height() + 3,
                 f"{b.get_height():.0f}", ha="center", fontsize=11, fontweight="bold")
    ax1.set_ylabel("Circuit Depth")
    ax1.grid(axis="y", alpha=0.3)

    bars2 = ax2.bar(["Aer (ideal)", f"Kingston\n({proc_label})"],
                     [ideal_gates, real_gates],
                     color=["#4e79a7", "#e15759"], edgecolor="white", width=0.5)
    for b in bars2:
        ax2.text(b.get_x() + b.get_width() / 2, b.get_height() + 5,
                 f"{b.get_height():.0f}", ha="center", fontsize=11, fontweight="bold")
    ax2.set_ylabel("Estimated Gate Count")
    ax2.grid(axis="y", alpha=0.3)

    fig.tight_layout()
    fig.savefig(os.path.join(OUT, "transpilation_comparison.png"))
    plt.close()
    print("  OK transpilation_comparison.png")

else:
    print("  SKIP Figs 11-14: ibm_quantum_results.json not found (run run_ibm_quantum_batch.py first)")

print(f"\nAll figures generated in: {OUT}")
