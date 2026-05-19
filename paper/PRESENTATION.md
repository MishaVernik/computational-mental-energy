---
marp: true
theme: default
paginate: true
math: katex
size: 16:9
header: 'ICSSEA 2026'
footer: 'Vernik, Oleshchenko, Hu — Hybrid Quantum-Classical CME from EEG'
style: |
  section { font-size: 24px; }
  section.lead h1 { font-size: 44px; line-height: 1.2; }
  section.lead h2 { font-size: 28px; color: #555; font-weight: 400; }
  h1 { font-size: 32px; }
  h2 { font-size: 26px; color: #333; }
  pre, code { font-size: 0.85em; }
  table { font-size: 0.82em; }
  blockquote { border-left: 4px solid #888; padding-left: 12px; color: #444; }
  .cols { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
  .small { font-size: 0.85em; color: #444; }
  .pill { display: inline-block; padding: 2px 10px; border-radius: 12px; background: #eee; font-size: 0.8em; margin-right: 6px; }
  .box { background: #f4f6fa; border-left: 4px solid #3a6ea5; padding: 10px 14px; margin: 8px 0; border-radius: 4px; }
  .num { color: #3a6ea5; font-weight: 600; }
---

<!-- _class: lead -->

# Hybrid Quantum-Classical Framework for Computational Mental Energy from Multichannel EEG Streams

## A standardized cognitive indicator (CME, Vernik unit) with hybrid quantum-classical inference, validated on real IBM Marrakesh hardware

**Mykhailo Vernik**, Liubov Oleshchenko, Zhengbing Hu
Igor Sikorsky Kyiv Polytechnic Institute · Hubei University of Technology

ICSSEA 2026 · Article No. 2614

---

# Why this matters

<div class="cols">

**The opportunity**
- Wearable EEG is now portable, real-time, low-friction
- Adaptive HCI, learning, neurofeedback, workplace cognition all need *operational* cognitive metrics
- Quantum ML is moving from offline benchmarks toward streaming use

**Three persistent gaps**
1. Pipelines fragment feature extraction and decisions; no unified output unit
2. Quantum ML for EEG is evaluated offline, not in production-grade streams
3. Quantum-resource cost is rarely modelled together with prediction quality

</div>

> **Concrete pain.** A meditation app says "your focus today: 73". 73 of what? On Tuesday's session? On someone else's brain? *No common unit means no comparability.*

---

# Постановка проблеми

<div class="cols">

**П'ять невирішених системних питань**
1. **Немає єдиного EEG-індикатора** з явною фізичною одиницею і session-level правилами агрегації. Існуючі індекси (β/θ, α/θ) безрозмірні і **не порівнювані** між сесіями та користувачами.
2. **QML для EEG оцінювалось лише офлайн** [Olvera 2024, Hernandez-Arango 2024], а не в production-grade потокових пайплайнах.
3. **Жодна система не оптимізує спільно** якість квантової моделі і ресурсну вартість $(S, D, T_{\text{qpu}})$ в одному циклі.

**Чого бракує практиці**
4. Немає архітектури, що **підтримує quantum / classical / hybrid режими** під одним формалізмом без зміни API.
5. Немає моделі **активність-залежних швидкостей** когнітивного споживання та довгострокової динаміки виснаження.

</div>

> Таким чином, проблема не в окремому алгоритмі, а в **системній інтеграції**: метрика + інференс + ресурсна оптимізація + потокова архітектура під одним дахом.

---

# Мета дослідження

<div class="box">

**Мета.** Розробити та експериментально валідувати **метод** обчислення показника обчислювальної ментальної енергії (CME) на основі гібридного квантово-класичного інференсу і ресурсо-чутливої метаевристичної оптимізації, який:

(а) дає стандартизований показник у явній одиниці $1\,\mathrm{Vn} \equiv 1\,\mu V^2 \cdot s$;
(б) працює у потоковому режимі з end-to-end-латентністю **≤2 с** на 5-секундному вікні;
(в) підтримує quantum / classical / hybrid режими **без зміни математичної суті індикатора**;
(г) валідується на реальних EEG-даних і реальному QPU-обладнанні.

</div>

**Що буде покращено (вимірювані результати):**
- Порівнюваність когнітивних метрик через явну одиницю Vn.
- Стабільність прогнозу: <span class="num">−22.2 %</span> дисперсії $p_{\text{flow}}$ у гібриді.
- Якість класифікації: AUROC <span class="num">0.800 → 0.967</span> (+16.7 п.п.).
- Керованість квантової вартості через спільну оптимізацію $(\Theta, S, D)$.

---

# Об'єкт, предмет, завдання дослідження

<div class="cols">

**Об'єкт**
Процес оцінювання когнітивного стану користувача за багатоканальним EEG у потоковому режимі.

**Предмет**
Методи й моделі гібридного квантово-класичного інференсу та ресурсо-чутливої метаевристичної оптимізації для обчислення показника CME.

**П'ять завдань**
1. Формалізувати CME з одиницею Vn та правилами window/session агрегації.
2. Спроєктувати 4-кубітний VQC з data re-uploading, придатний для NISQ ($|\Theta|=24$).
3. Розробити механізм гібридного злиття $\mu$ із збереженням CME-формалізму.
4. Реалізувати метаевристичну оптимізацію $(\Theta, S, D)$ за цільовою функцією якість+вартість (GA / PSO / ACO / SA).
5. Експериментально валідувати на реальних EEG (Muse Athena, 8 активностей, 288 вікон) і реальному QPU (IBM Marrakesh).

</div>

---

# Contributions

1. **Computational Mental Energy (CME)** — a window- and session-level cognitive indicator with an explicit unit, the **Vernik** ($1\,\mathrm{Vn} \equiv 1\,\mu V^2{\cdot}s$).
2. **Hybrid quantum-classical inference** — a 4-qubit data-re-uploading VQC fused with a classical MLP via a single mixing weight $\mu$.
3. **Resource-aware metaheuristic optimization** — jointly tunes model parameters $\Theta$, shots $S$, depth $D$ against quality + cost.
4. **Deployable streaming architecture** — wearable EEG → edge → server, with quantum-only / classical-only / hybrid backends behind one CME formalism.

<span class="pill">EEG</span><span class="pill">Quantum ML</span><span class="pill">Streaming</span><span class="pill">Real QPU validation</span>

---

# Computational Mental Energy (CME)

**Idea.** Combine *what the EEG shows* (spectral energy), *what the user is doing* (task complexity $c$), and *how engaged they are* (flow probability $p_{\text{flow}}$) into one comparable number per window.

**Aggregated band power** per window:
$$E_{\text{band}}(t) = \sum_{ch \in \mathcal{CH}} \left( w_\delta P_\delta + w_\theta P_\theta + w_\alpha P_\alpha + w_\beta P_\beta + w_\gamma P_\gamma \right)$$

**Modulation** by complexity and flow:
$$g(c, p) = \lambda_1 c + \lambda_2 p + \lambda_3 c p$$

**CME rate and window CME:**
$$\mathrm{CME}_{\text{rate}}(t) = \kappa \cdot E_{\text{band}}(t) \cdot g(c(t), p_{\text{flow}}(t)), \quad \mathrm{CME}(t) = \mathrm{CME}_{\text{rate}}(t) \cdot \Delta$$

> Three knobs (energy, complexity, engagement) → one number per window.

---

# CME — worked example (one 5-s window of *reading technical*)

<div class="cols">

**Inputs**
- Aggregated band power: $E_{\text{band}} = \mathtt{4.9903}\,\mu V^2$
- Task complexity: $c = 0.62$ (reading technical)
- VQC over 1024 shots: $p_{\text{flow}} = 0.623$
- Default weights: $\lambda_1 = \lambda_2 = \lambda_3 = 0.5$, $\kappa = 1$

**Modulation**
$$g(c, p) = 0.5 \cdot 0.62 + 0.5 \cdot 0.623 + 0.5 \cdot 0.62 \cdot 0.623$$
$$= 0.310 + 0.312 + 0.193 = \mathbf{0.815}$$

</div>

<div class="box">

**Compose**
$$\mathrm{CME}_{\text{rate}} = 1.0 \cdot 4.99 \cdot 0.815 = \mathbf{\,4.07\,Vn/s}$$
$$\mathrm{CME}(\Delta = 5\,s) = 4.07 \cdot 5 = \mathbf{\,20.33\,Vn}$$

</div>

> Same chain runs every 5 s in production: spectral features → flow probability → modulated rate → window CME → session aggregate.

---

# Why a new unit (Vernik)?

<div class="cols">

**Definition**
- $1\,\mathrm{Vn} \equiv 1\,\mu V^2 \cdot s$
- Time-integral of squared EEG amplitude over a window $\Delta$
- Pure signal-energy unit (no impedance assumption)

**Why a named symbol?**
- Cognitive expenditure is reported across sessions, devices, dashboards
- A single named magnitude is operationally cleaner — same logic as **kWh** naming a derived joule magnitude
- *Not a new physics claim*; an analytics convention

</div>

**Concrete intuition** — at the rates measured in our pilot:

- 5 s of resting ≈ <span class="num">186 Vn</span> (37.1 Vn/s × 5 s)
- 5 s of coding ≈ <span class="num">1,700 Vn</span> (339.9 Vn/s × 5 s) — ~9× higher
- A 9.5-h working day ≈ <span class="num">7.6 M Vn</span> total *(single-subject illustration)*

---

# Гібридна потокова шестирівнева архітектура

<div class="cols">

**Шість функціональних рівнів**
1. **Acquisition** — wearable EEG (Muse Athena, 4 канали)
2. **Ingestion** — windowing, quality filter, normalization
3. **Features** — Welch PSD per band; full $\mathbb{R}^{22}$ + reduced $\mathbb{R}^{8}$
4. **Inference** — quantum / classical / hybrid backend
5. **CME engine** — $\mathrm{CME}_{\text{rate}}(t)$, $\mathrm{CME}(t)$, session aggregates
6. **Persistence + Web API** (async, неблокувальний)

**Closed-loop optimizer (паралельний контур)**
- Background метаевристика (GA / PSO / ACO / SA)
- Тюнить $(\Theta, S, D)$ за квалітет vs вартість
- *Відрізняє нашу систему від static offline QML*

</div>

> Прикметники названі точно: **гібридна** (Q + Cl), **потокова** (windowed real-time), **шестирівнева** (6 функціональних шарів). Backend decoupled від CME-формалізму — будь-який бекенд можна замінити в runtime без зміни логіки обчислення енергії.

---

# A window's journey — ~1.59 s end-to-end

| Step | Median latency | Where |
|---|---|---|
| EEG window arrival ($\Delta = 5\,s$) | $t = 0$ | edge node |
| Ingest, quality check, band-pass filter | ~30 ms | server |
| Welch PSD × 4 channels × 5 bands | ~80 ms | server |
| Feature transforms ($\mathbb{R}^{22}$, $\mathbb{R}^{8}$) | ~10 ms | server |
| **Quantum inference (1024 shots)** | **~1456 ms** | Aer / QPU |
| Classical NN forward pass | ~5 ms | server |
| Fusion + CME engine | < 1 ms | server |
| Async persistence + API response | ~10 ms | server |
| **End-to-end (reported)** | **~1589 ms** | — |

> Quantum dominates latency. The metaheuristic optimizer trades shots/depth for speed when budgets are tight; classical fallback engages if QPU is unavailable.

---

# 4-qubit Variational Quantum Classifier

**Per layer $\ell \in \{0, \dots, L\}$:**

1. **Dual-axis re-uploading** — $R_y((x_i+1)\pi)$, $R_z((x_{i+4}+1)\pi)$
2. **Feature interaction** — ring-wise $R_{ZZ}$ between adjacent qubits
3. **Entanglement** — CNOT ring $q_0 \to q_1 \to q_2 \to q_3 \to q_0$
4. **Trainable** — $R_y(\theta_i^{(\ell)})$, $R_z(\phi_i^{(\ell)})$

**Compactness:** $N_q = 4$, $L = 2 \Rightarrow |\Theta| = 2 N_q (L+1) = \mathbf{24}$ trainable parameters.

**Readout:** $p_{\text{flow}}(t) = \Pr[b_0 = 1]$ on $q_0$.

> Re-uploading at every layer increases the expressive power of an otherwise shallow circuit — important when only 4 qubits are available.

---

# VQC encoding — a worked example

**Reduced feature vector** $x \in \mathbb{R}^{8}$ (band averages + asymmetry + engagement + complexity), normalized to $[-1, 1]$:
$$x = [\,0.30,\, 0.50,\, 0.20,\, 0.40,\, 0.10,\, 0.60,\, 0.70,\, 0.50\,]$$

| Qubit | $R_y$ angle | $R_z$ angle | Encodes |
|---|---|---|---|
| $q_0$ | $(0.30 + 1)\pi = 1.30\pi$ | $(0.10 + 1)\pi = 1.10\pi$ | $\bar P_\delta$ + frontal asymmetry $_\alpha$ |
| $q_1$ | $(0.50 + 1)\pi = 1.50\pi$ | $(0.60 + 1)\pi = 1.60\pi$ | $\bar P_\theta$ + engagement $\beta/\theta$ |
| $q_2$ | $(0.20 + 1)\pi = 1.20\pi$ | $(0.70 + 1)\pi = 1.70\pi$ | $\bar P_\alpha$ + task complexity $c(t)$ |
| $q_3$ | $(0.40 + 1)\pi = 1.40\pi$ | $(0.50 + 1)\pi = 1.50\pi$ | $\bar P_\beta$ + (reserved) |

**Per layer:** $R_{ZZ}$ ring → CNOT ring → trainable $R_y(\theta)$, $R_z(\phi)$.
**Measurement:** 1024 shots on $q_0$ → count$(b_0 = 1) = 638$ → $p_{\text{flow}} = 0.623$.

---

# Hybrid fusion

$$p_{\text{flow}}^{\text{hybrid}}(t) = \mu \cdot p_{\text{flow}}^{\text{Q}}(t) + (1 - \mu) \cdot p_{\text{flow}}^{\text{NN}}(t), \quad \mu \in [0, 1]$$

<div class="cols">

**Three modes, one CME core**
- $\mu = 1$ — quantum-only
- $\mu = 0$ — classical-only
- $\mu = 0.6$ — recommended production

**Branches in parallel**
- Quantum: 4-qubit VQC on $\mathbb{R}^{8}$ reduced features
- Classical: MLP (22 → 64 → 32 → 1) on full $\mathbb{R}^{22}$

</div>

> Fusion happens *before* the CME engine, so the energy formula never changes. $\mu$ is a *trust knob*: it can be tuned from data or set by policy.

---

# Hybrid fusion — a worked example

**Same window, two estimators**
- Quantum: $p_{\text{flow}}^{Q} = 0.85$ — high confidence, but per-window variance $\sigma_Q^2 = 0.0140$
- Classical: $p_{\text{flow}}^{NN} = 0.42$ — less peaked, lower variance $\sigma_{NN}^2 \approx 0.005$

**Apply $\mu = 0.6$:**
$$p_{\text{flow}}^{\text{hybrid}} = 0.6 \cdot 0.85 + 0.4 \cdot 0.42 = 0.510 + 0.168 = \mathbf{0.678}$$

**Variance after fusion** (treating branches as approximately independent):
$$\sigma^2_{\text{hybrid}} \approx \mu^2 \sigma_Q^2 + (1 - \mu)^2 \sigma_{NN}^2$$

<div class="box">

In our pilot this drops the empirical $p_{\text{flow}}$ variance from <span class="num">0.0140</span> → <span class="num">0.0083</span> — a **−40.9 %** reduction, so flow predictions stop oscillating between back-to-back windows and the cumulative CME trace becomes smooth enough for live dashboards.

</div>

---

# Чому саме гібрид, а не щось одне

<div class="cols">

**Гібрид — це не компроміс, це інженерний вибір з 5 причин**

1. **Комплементарні представлення.** MLP бачить повний $\mathbb{R}^{22}$, VQC — стиснутий $\mathbb{R}^{8}$. Дві гілки дають слабко корельовані помилки → класична умова виграшу ансамблю.
2. **Зменшення дисперсії.** $\sigma^2_{p_{\text{flow}}}$: <span class="num">0.0144 → 0.0112</span> (−22.2%); у ранній версії −40.9%.
3. **Робастність.** При недоступності QPU $\mu \to 0$ → graceful degradation на класичну гілку **без зміни CME-формалізму та API**.

**Числа на пілотних 288 вікнах**

| Режим | Acc | F1 | AUROC |
|---|:---:|:---:|:---:|
| Quantum-only (24 параметри) | 0.792 | 0.412 | 0.800 |
| Classical-only (MLP, generated labels) | 1.000 | 1.000 | 1.000 |
| **Hybrid ($\mu = 0.6$)** | **0.938** | **0.700** | **0.967** |

</div>

> Q-only → Hybrid: AUROC **+16.7 п.п.**, F1 **+28.8 п.п.** Класична гілка на 100% бо сама генерує labels — це upper-bound, а не справжня точність. Коректне порівняння — Q-only vs Hybrid.

**4. NISQ-сумісність.** 24-параметровий VQC навмисно малий — максимум для одного суб'єкта без overfitting і для виконання на Heron r2 (depth 61 → 219 після transpile). Гібрид компенсує обмежену ємність через $p^{NN}_{\text{flow}}$.
**5. Безшовний перехід** між режимами — один CME-формалізм, $\mu \in \{0, 0.6, 1\}$ задається конфігом.

---

# Experimental setup

<div class="cols">

**Single-subject pilot**
- Muse Athena headband (TP9, AF7, AF8, TP10)
- 8 cognitive activities (rest → coding/math)
- 288 five-second windows, 24 min recording
- Train/eval split with held-out evaluation subset
- Weak labels heuristically refined (FSS / FKS to follow)

**Backends**
- Aer simulator (1024 shots/window)
- **IBM Marrakesh** — 156-qubit Heron r2 (real QPU)
- 100-window stratified sample submitted as one batch job

</div>

<span class="small">Reported numbers are point estimates; no inter-subject confidence intervals are claimed.</span>

---

# Експериментальна установка — деталі та одиниці

<div class="cols">

**Симулятор**
- **Qiskit Aer** (open-source, IBM)
- shot-based statevector, без шумової моделі
- $S = 1024$ shots/вікно
- ~1 с на 100 схем (CPU)

**Реальне обладнання**
- **IBM Marrakesh** — 156-кубітний QPU
- Архітектура **Heron r2**, heavy-hex topology
- Доступ: IBM Quantum Free Tier (10 хв QPU/28 днів)
- Depth ideal → transpiled: **61 → 219** (×3.6, краще за Eagle ×4.4)

</div>

**Одиниці на всіх графіках**

| Графік | X | Y | Розмірність |
|---|---|---|---|
| Accuracy / F1 / AUROC | режим | значення [0, 1] | безрозмірна |
| CME-rate vs activity | $c(t) \in [0, 1]$ | $\mathrm{CME}_{\text{rate}}$, **Vn/s** $= \mu V^2$ | сигнальна потужність |
| Sim vs real QPU scatter | $p_{\text{flow}}$ Aer [0, 1] | $p_{\text{flow}}$ Marrakesh [0, 1] | безрозмірна |
| Latency table | — | **мс** (median / P95 / P99) | час |
| Transpilation depth | конфігурація | gate count | штук |

> $1\,\mathrm{Vn} \equiv 1\,\mu V^2 \cdot s$ — сигнальна енергія (Парсеваль). Vn/s = $\mu V^2$ — миттєва потужність. Перетворення у Дж є необов'язковою апроксимацією.

---

# Results — flow-state classification

| Mode | Accuracy | F1 | AUROC |
|---|---|---|---|
| Quantum-only VQC (4 qubits, 24 params) | 0.500 | 0.153 | 0.548 |
| **Hybrid ($\mu = 0.6$)** | **0.882** | **0.553** | **0.914** |

- Hybrid reduces $p_{\text{flow}}$ variance **−40.9 %** vs quantum-only (0.0140 → 0.0083).
- F1 / accuracy gap reflects class imbalance in the small evaluation set.
- 24 trainable parameters is *intentional* — small enough to train on a single subject without overfitting and to fit on a 4-qubit chip.

> **Single-subject pilot.** Cross-subject validation is the first item of future work; recent wearable-EEG flow studies provide LOSO benchmarks to compare against [Beiramvand et al., *Proc. EMBC 2025*: 93 % within-subject, 82 % LOSO over 29 subjects].

---

# Results — CME per activity

<div class="cols">

| Activity | $c(t)$ | $p_{\text{flow}}$ | CME rate (Vn/s) |
|---|---|---|---|
| Coding | 0.70 | 0.456 | **339.9** |
| Math / Problem solving | 0.90 | 0.442 | 316.7 |
| Debugging | 0.80 | 0.469 | 277.0 |
| Reading (technical) | 0.60 | 0.484 | 190.5 |
| Reading (general) | 0.35 | 0.472 | 149.3 |
| Email | 0.30 | 0.415 | 132.5 |
| Browsing | 0.20 | 0.484 | 112.5 |
| Resting | 0.05 | 0.551 | **37.1** |

**Take-aways**
- ~9× rate ratio between coding and rest
- Super-linear with $c(t)$ in the fitted curve
- 9.5-h day extrapolates to ≈ **7.6 M Vn**
- *Single-subject illustration* — not a population budget

</div>

<div class="box">

**Use-case example.** A 4-h coding block at 339.9 Vn/s burns ≈ <span class="num">4.9 M Vn</span> — about 64 % of the daily total. A scheduler can recommend interleaving low-rate work (browsing, email ≈ 120 Vn/s) to flatten the cumulative curve.

</div>

---

# Results — IBM Marrakesh validation

<div class="cols">

**Hardware run**
- 100 stratified EEG windows (≈12-13 per activity)
- 1 batch job, 55.5 s on real QPU
- Transpiled depth: 61 → 219 (×3.6 routing overhead)
- Heron r2 routing is already an improvement over Eagle (×4.4)

**Simulator vs real hardware**
- Pearson **r = 0.869**, MAE = 0.045
- $p_{\text{flow}}$ std: sim 0.103 → QPU 0.068
- Hybrid accuracy: sim 91 % → QPU 96 %
- Per-activity sim-vs-QPU disagreement < 0.034 (worst: Email, Resting)

</div>

> The 5-point hybrid-accuracy gap is *consistent with noise-induced smoothing*, but is not claimed as a reproducible hardware advantage without repeated runs and CIs. Heron-vs-Eagle benchmarking [Mayo et al., 2026] supports targeting Heron-class hardware for current variational workloads.

---

# Limitations (stated up front)

- **Sample size.** $N = 1$ subject, 288 windows; reported AUROC / accuracy / $r$ are point estimates without inter-subject confidence intervals.
- **Labels.** Weak labels from a classical NN, refined heuristically; not yet validated against FSS / FKS on this dataset.
- **Hardware sample.** Single 100-window batch on one QPU; results are indicative across providers and sessions.
- **SI mapping.** $\mathrm{CME} \to J$ assumes a simple electrode impedance — an approximation, not a physical claim.
- **Sleep / restoration model (§ 6.4).** Conceptual extension only; *not validated experimentally* in this paper.

---

# Future work

- Multi-subject empirical validation, FSS / FKS calibration, transfer learning baselines
- Longitudinal CME balance & restoration model (multi-day, sleep tracking)
- Error mitigation on Heron — Dynamical Decoupling, ZNE, PEC
- Online policy learning for $S$, $D$, and $\mu$ under latency budgets
- Cross-device generalization (Muse 2, Muse S, Emotiv, Neurosity)
- Recent work to build on: hybrid quantum-classical EEG frameworks for clinical signals [Padmaja et al., *Sci. Rep.* 2026]

---

# Що робить квантова частина — і чого НЕ робить

<div class="cols">

**НЕ робить** (прозоро)
- **Не прискорює** інференс: 1456 мс QPU vs 5 мс класичний MLP (×290 повільніше).
- **Не зменшує простір пошуку** (Grover дає лише $O(\sqrt{N})$).
- **Не замінює** класичну оптимізацію: GA/PSO/ACO/SA працюють на CPU.
- **Не дає** експоненціального speedup для VQC-класифікації (відкрите питання).

**Робить** (підтверджено експериментом)
- **Інше представлення ознак** у $\mathcal{H} = \mathbb{C}^{16}$ (4 кубіти).
- **Експресивність re-uploading**: $L+1$ повторне кодування подвоює фур'є-спектр [Schuld 2021].
- **Комплементарний прогноз** → AUROC +16.7 п.п. у гібриді.
- **Регуляризація шумом NISQ**: std $p_{\text{flow}}$ 0.119 → 0.087 на Marrakesh.

</div>

> Архітектурно: метаевристика (класичний CPU) → викликає QPU **як оракул-обчислювач** функції $p_{\text{flow}}(\mathbf{x}; \Theta)$. Це не «квантовий пошук», це параметрична підгонка з квантовим feature map.

---

# Що ми вимірюємо — і чого НЕ вимірюємо

<div class="cols">

**Що CME є**
- **Операційний індикатор** = signal-energy ($\mu V^2 \cdot s$, Парсеваль) × cognitive modulator ($c$, $p_{\text{flow}}$).
- Формально: $\mathrm{CME}(t) = \kappa \cdot E_{\text{band}}(t) \cdot g(c, p_{\text{flow}}) \cdot \Delta$.
- **Проксі** до ментальної активності — як HRV є проксі для активності автономної НС.

**Чому «ментальна» (5 фактів)**
1. Джерело — мозок (EEG, post-synaptic potentials, ICA-фільтрація артефактів).
2. Модулятори когнітивні: $c(t)$, $p_{\text{flow}}$.
3. Емпірична дискримінативність: 9.15× між Coding і Resting.
4. Кореляти flow [Katahira 2018, Cherep 2024, Pope 1995].
5. Заплановано валідацію проти FSS / FKS.

</div>

**Чого CME явно НЕ є** (межі чесно зафіксовані у [patent/opys_vynakhodu.md](../patent/opys_vynakhodu.md) п. 6.8.3):

| Не є | Що для цього потрібно |
|---|---|
| глюкозний метаболізм нейронів | fMRI / fPET-CT |
| теплова потужність кори | fNIRS / тепловізор |
| ATP-споживання | пряма біохімія, ex vivo |

> Стандартна аналогія: CME : ментальна активність ≈ HRV : автономна НС. Проксі-метрика з явною сигнальною компонентою.

---

# Що означає **МЕТА**евристика?

<div class="cols">

**Етимологія і визначення**
- грец. **μετά** — «над, на вищому рівні»
- Glover (1986): метаевристика — **алгоритмічний каркас вищого рівня**, що визначає стратегію застосування внутрішніх (проблемно-залежних) евристик.
- На відміну від звичайної евристики, метаевристика **проблемно-незалежна**.

**Чотири властивості**
1. Проблемна незалежність
2. Стохастичність
3. Ітеративне покращення
4. Баланс exploration / exploitation

</div>

**Чому в нашій задачі — саме метаевристика:**

$$J(\mathbf{u}) = \mathcal{L}_{\text{val}}(\mathbf{u}) + \xi_S \cdot S + \xi_D \cdot D + \xi_T \cdot T_{\text{qpu}}(\mathbf{u})$$

- **Багатокритеріальна** (квалітет vs ресурси) → потрібен Pareto-aware пошук
- **Негладка** (квантовий шум при кінцевому $S$) → градієнт не визначений
- **Шумна** (стохастичні shots) → потрібне усереднення по поколінню
- **Дискретно-неперервна** ($\Theta$ continuous, $S, D$ цілі)

> 4 алгоритми (GA / PSO / ACO / SA) — навмисне порівняння 4 парадигм пошуку: еволюційна / роєва / стигмергія / термодинамічна.

---

# Сценарії застосування

| Категорія | Сценарій | Бюджет latency | Як CME допомагає |
|---|---|:---:|---|
| **Адаптивні HCI / e-learning** | LMS змінює складність матеріалу за поточним $p_{\text{flow}}$ | ≤2 с | $c(t)$ + $p_{\text{flow}}$ → авто-навантаження |
| **Closed-loop neurofeedback** | Тренування уваги / медитації з візуальним feedback | ≤500 мс | CME-index [0,100] на дашборді |
| **Корпоративний моніторинг вигорання** | Анонімізовані team-dashboards, alert при overload | хв/год | session $\overline{CME}$, FlowShare → burnout markers |
| **Особиста продуктивність / quantified-self** | Daily energy budget (CMEflow продукт) | секунди + добова агрегація | $\sum_t \mathrm{CME}(t)$ → денний бюджет |
| **Дослідницькі платформи** | Великі EEG-набори, batch-аналіз | хв (batch) | відкрите API + reproducible pipeline |
| **BCI-реабілітація** | Моніторинг навантаження пацієнтів під час тренувань | ≤2 с | $q(t)$ + adaptive threshold |

> Цільова **низька end-to-end-латентність ≤2 с** (не «квантовий speedup») потрібна для real-time feedback. Ринок Muse-користувачів: **200K–500K** worldwide; brain-sensing headbands: **$281M** (Interaxon).

---

# Термінологія — точна ієрархія

| Термін | Що означає | Що в нас |
|---|---|---|
| **Метод** | конкретний алгоритм/спосіб | **CME-формалізм + VQC + гібридне злиття + метаевристика** ← захищається |
| **Підхід** | загальний напрям | гібридний квантово-класичний інференс |
| **Спосіб** | патентний синонім методу | формулювання в [opys_vynakhodu.md](../patent/opys_vynakhodu.md) рядок 5 |
| **Система** | сукупність компонентів | програмно-апаратна реалізація: Muse → стрімер → API → QPU → DB |
| **Архітектура** | структурна схема | гібридна потокова **шестирівнева** |
| **Фреймворк** | програмний каркас | Qiskit, FastAPI — використовуються; не наш внесок |
| **Методологія** | вчення про систему методів | **не наш рівень**; ми не пропонуємо нову методологію |

> У україномовному захисті коректно: «**Гібридний квантово-класичний метод** оцінювання CME». Слово «комплексна» зайве (дублює «гібридна» + «багаторівнева»).

---

# Take-aways

1. **CME + Vernik** give EEG-derived cognitive expenditure a single named magnitude — comparable across sessions, devices, subjects.
2. **Hybrid fusion** at $\mu = 0.6$ cuts flow-probability variance by **40.9 %** and reaches **0.914 AUROC** on a single-subject pilot.
3. **Real QPU run** on IBM Marrakesh agrees with the simulator at **r = 0.869**, MAE = 0.045 — feasibility on Heron-class hardware *today*.
4. **Open questions** are explicit: multi-subject validation, longitudinal restoration, error mitigation, and policy learning for resource control.

> A reproducible streaming pipeline + a measurable cognitive unit + a working real-QPU path — all behind one CME formalism.

---

<!-- _class: lead -->

# Thank you — questions?

**Mykhailo Vernik**
mykhailo.vernik@pzks.fam.kpi.ua
ORCID: 0009-0008-6156-1051

Liubov Oleshchenko · Zhengbing Hu

ICSSEA 2026 — Article No. 2614
