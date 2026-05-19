# Review Response and Edit Guide - Article No. 2614

This document maps every reviewer concern from the ICSSEA 2026 peer review of *"Hybrid Quantum-Classical Framework for Computational Mental Energy from Multichannel EEG Streams"* to specific, line-anchored edits in [paper/paper.txt](paper/paper.txt).

Each entry uses the format:

```
Line N (or quoted anchor)
line: <current text from paper.txt>
new:  <replacement text>
why:  <reviewer criterion this addresses>
```

When applied, every change should be marked in a single highlight color (e.g., yellow) per the editor's resubmission instruction.

---

## Summary

The reviewer's verdict is **"Should be revised and resubmitted"** with moderate revision. The strengths (system integration, novelty of CME / Vernik, real-QPU validation) are kept. The edits below address every reviewer criterion that scored "minor revision" or worse:

- **Originality, Significance, Conclusions** - hedge overstated claims of novelty, "deployment readiness", and generalizability.
- **Abstract** - trim density, drop the "confirming deployment readiness" framing, note the single-subject sample.
- **Results, Data representation, Graphs/figures** - attach explicit "single subject, 288 windows, no significance tests" caveats next to all headline numbers; reword the QPU-beats-simulator claim.
- **Results 4 / Conclusions 3** - reframe the daily-budget and sleep-debt sections as conceptual extensions, not validated findings.
- **Presentation 2-3** - rewrite the longest run-on sentences and fix grammar.
- **Presentation 4** - justify the new terminology (CME, Vernik) where it is first introduced.
- **References, Author info, Declarations** - add 2025-2026 references (highlight in yellow), insert author photo placeholders per the latest template, and add the missing `Informed Consent Statement` declaration.

---

## Section 1 - Hedge overstated claims

Reviewer criteria addressed: Originality (1), Significance (2), Conclusions (2)(3), Results (3).

### 1.1 Abstract - drop "confirming deployment readiness"

Lines 32-37

```
line: Validation on the IBM Marrakesh 156-qubit Heron r2 real quantum processor demonstrates a Pearson correlation (r = 0.869) between ideal simulator and real hardware pFlow values (MAE = 0.045), with hybrid accuracy improving from 91% to 96% on real QPU, confirming deployment readiness on current quantum hardware.
new:  On the IBM Marrakesh 156-qubit Heron r2 processor, simulator-vs-hardware pFlow agreement reaches r = 0.869 (MAE = 0.045) on a 100-window stratified sample, indicating feasibility - rather than fully established deployment readiness - on currently available quantum hardware.
why:  Abstract (3) and Conclusions (2): "confirming deployment readiness" overstates a single 100-window run.
```

### 1.2 Introduction - soften purpose statement

Lines 67-73

```
line: Specifically, the paper aims to show that the hybrid quantum-classical inference mode reduces flow-probability prediction variance by over 40% compared to quantum-only inference, the 4-qubit VQC achieves a pFlow correlation of r = 0.869 between ideal simulator and real quantum hardware, demonstrating deployment readiness, and the CME framework captures a 9.15-fold difference in cognitive consumption rate between high-demand and low-demand activities.
new:  Specifically, the paper aims to show, on a single-subject pilot, that the hybrid quantum-classical mode reduces flow-probability prediction variance by over 40% relative to quantum-only inference; that the 4-qubit VQC reproduces simulator predictions on real hardware at r = 0.869, indicating noise robustness; and that the CME framework captures an approximately 9-fold difference in consumption rate between high-demand and low-demand activities.
why:  Originality (1) and Conclusions (2): replaces "demonstrating deployment readiness" with "indicating noise robustness" and adds the single-subject scope.
```

### 1.3 Results 6.1 - hybrid result is single-subject

Lines 357-359

```
line: In contrast, the hybrid configuration (μ = 0.6) significantly improves performance, achieving 88.2% accuracy and 0.914 AUROC by integrating complementary signals from the quantum and classical branches. This result demonstrates the effectiveness of the proposed fusion strategy.
new:  In contrast, the hybrid configuration (μ = 0.6) improves performance on the 288-window single-subject evaluation, reaching 88.2% accuracy and 0.914 AUROC by integrating complementary signals from the quantum and classical branches. With no inter-subject confidence intervals available, this should be read as a single-subject indication of fusion effectiveness rather than a general result.
why:  Results (3) and Data representation (2): drops "significantly" (no significance test reported) and adds the sample-size caveat.
```

### 1.4 Results 6.2 - QPU > simulator is not a validated advantage

Lines 484-487

```
line: Notably, the hybrid accuracy on real QPU (96.0%) exceeds the simulator hybrid accuracy (91.0%), demonstrating that the noise-induced smoothing of the quantum branch can improve classification when combined with the classical branch.
new:  On the 100-window stratified sample, the hybrid accuracy on real QPU (96.0%) was higher than on the simulator (91.0%); this 5-point gap is consistent with noise-induced smoothing of the quantum branch but is not claimed as a significant hardware advantage without repeated runs and confidence intervals.
why:  Graphs/figures (2) and Results (3): reviewer flagged the "hardware advantage" interpretation as not rigorously validated.
```

### 1.5 Conclusion - replace "ready for deployment" with "feasibility"

Lines 654-656

```
line: The demonstrated 40.9% reduction in prediction variance through hybrid fusion, combined with hardware noise resilience (r = 0.869 on real IBM Marrakesh QPU), confirms that the system is ready for deployment on currently available quantum hardware.
new:  The 40.9% reduction in prediction variance through hybrid fusion, combined with simulator-vs-hardware agreement at r = 0.869 on the IBM Marrakesh QPU, indicates that the system is feasible on currently available quantum hardware; broader deployment readiness will require multi-subject and multi-session validation.
why:  Conclusions (2)(3): generalizability and real-world readiness overstated.
```

### 1.6 Conclusion - hedge transpilation/accuracy framing

Lines 651-652

```
line: yet hybrid classification accuracy improves from 91.0% (simulator) to 96.0% (real QPU) due to noise-induced regularisation.
new:  on the 100-window stratified sample, hybrid classification accuracy was 91.0% (simulator) and 96.0% (real QPU), a small gap consistent with noise-induced regularisation but not yet established as a reproducible hardware effect.
why:  Conclusions (2): mirrors edit 1.4 in the conclusion.
```

---

## Section 2 - Trim and clarify the abstract

Reviewer criteria addressed: Abstract (1)(2)(3), Presentation (2).

### 2.1 Abstract - trim experimental block; integrate sample-size note

Lines 28-32

```
line: Experimental evaluation on real EEG data from 8 cognitive activities recorded with a Muse Athena headband via MindMonitor (288 five-second windows, 24 minutes of continuous recording) demonstrates that the hybrid mode (μ = 0.6) achieves 0.914 AUROC for flow-state detection, while the standalone 4-qubit VQC reaches 0.548 AUROC with only 24 trainable parameters.
new:  A single-subject pilot evaluation (8 cognitive activities, Muse Athena headband, 288 five-second windows, 24 minutes of recording) shows that the hybrid mode (μ = 0.6) reaches 0.914 AUROC for flow-state detection versus 0.548 AUROC for the standalone 4-qubit VQC (24 trainable parameters).
why:  Abstract (1)(2): density / verbosity; explicitly labels the pilot as single-subject.
```

### 2.2 Abstract - reframe daily extrapolation as illustrative

Lines 35-37

```
line: The experiment reveals a ~9x difference in CME consumption rate between high-demand activities (coding, 339.9 Vn/s) and low-demand activities (resting, 37.1 Vn/s), with an extrapolated daily total of approximately 7,618,000 Vn across a 9.5-hour working day.
new:  Across the eight activities, the CME rate differs by ~9x between coding (339.9 Vn/s) and resting (37.1 Vn/s); the corresponding 9.5-hour day extrapolates to roughly 7.6 million Vn and is presented as a single-subject illustration rather than a population estimate.
why:  Abstract (3) and Results (4): hedges the daily-budget number.
```

### 2.3 Abstract - tone down the closing application sentence

Lines 37-40

```
line: The proposed software architecture contributes a reproducible pipeline for EEG-based cognitive-state analytics, introduces resource-aware optimization for quantum-assisted inference, and provides implementation-ready interfaces for scalable deployment in adaptive human-computer systems and workplace cognitive monitoring.
new:  The proposed software architecture contributes a reproducible pipeline for EEG-based cognitive-state analytics, introduces resource-aware optimization for quantum-assisted inference, and provides interfaces intended to support adaptive human-computer systems and workplace cognitive monitoring once multi-subject validation is completed.
why:  Abstract (3) and Significance (2): "scalable deployment" overstated for an initial single-subject contribution.
```

---

## Section 3 - Add empirical-rigor caveats

Reviewer criteria addressed: Results (3)(4), Data representation (1)(2), Graphs/figures (2).

### 3.1 Section 6.1 - sample size and class imbalance note

Lines 360-362

```
line: The observed discrepancy between accuracy and F1-score is attributed to class imbalance in the evaluation dataset. Additionally, the hybrid approach reduces pFlow prediction variance by 40.9% compared to the quantum-only model (from 0.0140 to 0.0083), indicating improved prediction stability.
new:  The discrepancy between accuracy and F1-score is attributable to class imbalance in the small (288-window, single-subject) evaluation set; reported metrics should therefore be interpreted as point estimates without inter-subject confidence intervals. Within this set, the hybrid approach reduces pFlow prediction variance by 40.9% relative to the quantum-only model (from 0.0140 to 0.0083), indicating improved within-subject stability.
why:  Data representation (2): adds the missing sample-size and CI caveat.
```

### 3.2 Fig. 6 - drop "confirms" for the super-linear claim

Lines 437-439

```
line: The super-linear relationship confirms that high-complexity tasks produce disproportionately higher energy expenditure, driven by both elevated spectral power and the modulation function g(c, p).
new:  The fitted relationship is consistent with high-complexity tasks producing disproportionately higher energy expenditure, driven by both elevated spectral power and the modulation function g(c, p); given the single-subject sample, the super-linear shape is indicative rather than statistically established.
why:  Graphs/figures (2): super-linear interpretation flagged as not rigorously validated.
```

### 3.3 Section 6.3 - hedge the 9x ratio and super-linear discussion

Lines 571-574

```
line: The ~9x rate ratio between Coding (339.9 Vn/s) and Resting (37.1 Vn/s) demonstrates that CME captures meaningful variation in cognitive demand. Notably, the relationship between c(t) and CME rate is super-linear (Fig. 6): activities above c = 0.60 produce rates 2-9 times higher than those below c = 0.35, driven by both higher spectral energy during demanding tasks and the multiplicative effect of the modulation function g(c, p).
new:  The ~9x rate ratio between Coding (339.9 Vn/s) and Resting (37.1 Vn/s) suggests that CME captures meaningful within-subject variation in cognitive demand. The c(t)-vs-rate relationship is super-linear in the fitted curve of Fig. 6: activities above c = 0.60 produce rates 2-9 times higher than those below c = 0.35, driven by both higher spectral energy during demanding tasks and the multiplicative effect of g(c, p). Both observations are based on a single subject and require replication for statistical confirmation.
why:  Graphs/figures (2) and Data representation (2).
```

### 3.4 Daily extrapolation - frame as illustrative

Lines 428-432

```
line: As shown in Table 5, the results reveal a ~9x difference in CME rate between the most demanding activity (Coding at 339.9 Vn/s) and the least demanding (Resting at 37.1 Vn/s). The three highest-rate activities - Coding (339.9), Math/Problem Solving (316.7), and Debugging (277.0 Vn/s) - share task complexities above 0.70 and together account for 60.0% of the recorded CME. The extrapolated daily total of approximately 7,618,000 Vn provides a concrete reference point for budgeting cognitive resources across a working day.
new:  As shown in Table 5, the results indicate a ~9x difference in CME rate between the most demanding activity (Coding, 339.9 Vn/s) and the least demanding (Resting, 37.1 Vn/s). The three highest-rate activities - Coding (339.9), Math/Problem Solving (316.7), and Debugging (277.0 Vn/s) - share task complexities above 0.70 and together account for 60.0% of the recorded CME. The extrapolated daily total of approximately 7,618,000 Vn is presented as an illustrative single-subject reference, not as a population-level cognitive budget; multi-subject validation is required before such estimates can be generalized.
why:  Results (4): "daily cognitive budget" flagged as speculative.
```

### 3.5 Section 6.2 - soften "high fidelity" / "robust"

Lines 482-484

```
line: The key finding is a Pearson correlation of r = 0.869 between simulator and real hardware pFlow values, with a mean absolute error of 0.045. This high fidelity indicates that the 4-qubit VQC circuit is robust to the noise levels present in current superconducting quantum hardware.
new:  On the 100-window stratified sample, simulator and real-hardware pFlow values agree at Pearson r = 0.869 with a mean absolute error of 0.045. This level of agreement suggests that the 4-qubit VQC circuit tolerates the noise levels of current superconducting quantum hardware; broader robustness claims require evaluation across multiple QPU sessions and devices.
why:  Results (3): avoids overclaiming "high fidelity" and "robust" from a single batch.
```

### 3.6 Discussion 6.5 - expand limitations into an explicit list

Lines 630-638

```
line: Several limitations should be noted, the present paper emphasizes system formalization and worked computational examples, large-scale comparative experimental evidence across subjects and task domains should be expanded in future submissions. Weak labeling strategies can introduce bias if not periodically corrected with stronger psychometric supervision such as FSS or FKS. The SI conversion of CME is approximate because it relies on simplified electrode impedance assumptions that do not fully account for volume conduction effects. QPU-specific performance may vary significantly across quantum hardware providers and queue conditions, meaning that latency figures reported here are indicative rather than definitive. Finally, generalization across devices, populations, and task domains requires broader multicenter validation that is beyond the scope of this initial architectural contribution.
new:  Several limitations should be noted explicitly. (i) Sample size: the empirical evaluation uses a single subject, eight activities, and 288 five-second windows; reported AUROC, accuracy, and r values are therefore point estimates without inter-subject confidence intervals or significance tests. (ii) Labels: weak labels generated by the classical network were refined heuristically but were not fully validated against psychometric scales (FSS, FKS) on the present dataset. (iii) Hardware sample: the IBM Marrakesh validation uses 100 windows from one batch job; QPU-specific performance can vary across providers, devices, and queue conditions, so the latency and accuracy figures should be read as indicative. (iv) SI conversion: the CME-to-joule mapping is approximate because EEG voltage reaches the electrode through a volume conductor that is not modelled as a simple resistor. (v) Generalization: extension across devices, populations, and task domains requires broader multi-centre validation, which is beyond the scope of this initial architectural contribution.
why:  Data representation (2), Results (3)(4), Conclusions (3): consolidates the reviewer's empirical-rigor concerns into one explicit list.
```

---

## Section 4 - Reframe speculative CME extensions

Reviewer criteria addressed: Results (4), Conclusions (3), Significance (2).

### 4.1 Section 6.4 - insert a leading caveat

Insertion point: immediately after the heading "6.4 Mental Energy Restoration and Sleep Deprivation" (between line 582 and 583)

```
line: 6.4 Mental Energy Restoration and Sleep Deprivation
new:  6.4 Mental Energy Restoration and Sleep Deprivation
      The model presented in this subsection is a conceptual extension of the CME framework intended to motivate future longitudinal work. The depletion-restoration dynamics, the daily cognitive budget B_d, and any "cognitive debt" interpretation are not validated experimentally in this paper; no sleep-deprivation data were collected.
why:  Results (4): explicitly labels the section as speculative as flagged by the reviewer.
```

### 4.2 Section 6.4 - soften "consistent with sleep debt literature" passage

Lines 615-619

```
line: Whether the effective baseline becomes "negative" depends on the model chosen for B_d. In the simplest additive model above, prolonged deprivation drives B_d below zero, which can be interpreted as a cognitive debt that must be repaid through extended recovery sleep before the subject returns to normal performance. This is consistent with the sleep debt literature, which shows that recovery from chronic sleep restriction requires multiple nights of adequate sleep rather than a single compensatory episode. The CME balance model thus provides a quantitative, EEG-grounded framework for tracking not only moment-to-moment cognitive expenditure but also the longer-term dynamics of depletion and recovery across multi-day work schedules.
new:  Whether the effective baseline becomes "negative" depends on the model chosen for B_d. In the simplest additive model above, prolonged deprivation would drive B_d below zero, an outcome that could be interpreted as a "cognitive debt" requiring extended recovery sleep. This proposal is qualitatively compatible with the sleep-debt literature, which reports that recovery from chronic sleep restriction tends to require multiple nights of adequate sleep rather than a single compensatory episode. Within the present paper the CME balance model should therefore be regarded as a quantitative hypothesis that is EEG-grounded but not yet empirically tested; longitudinal multi-day studies are required before any clinical or operational interpretation.
why:  Results (4) and Conclusions (3): hedges "consistent with" and "thus provides".
```

### 4.3 Conclusion - tighten the future-work bullet that references this model

Line 661

```
line: longitudinal validation of the CME balance and restoration model across diverse populations and sleep conditions.
new:  longitudinal validation of the proposed CME balance and restoration model - which is presented in this paper as a hypothesis only - across diverse populations and sleep conditions.
why:  Conclusions (3): keeps the future-work item but reminds the reader the model is still hypothetical.
```

---

## Section 5 - Grammar and verbosity passes

Reviewer criteria addressed: Presentation (2)(3).

### 5.1 Introduction - split the "three persistent issues" sentence

Lines 47-50

```
line: However, practical EEG state estimation still faces three persistent issues: fragmented pipelines that separate feature extraction and decision layers without unified operational output, limited integration of quantum machine learning methods in production-like real-time settings [10, 11], and insufficient treatment of computational resource constraints in quantum-enabled inference [12, 13].
new:  Practical EEG state estimation, however, still faces three persistent issues. First, pipelines fragment feature extraction and decision layers and lack a unified operational output. Second, quantum machine learning is rarely integrated into production-grade real-time settings [10, 11]. Third, computational resource constraints in quantum-enabled inference are typically not modelled explicitly [12, 13].
why:  Presentation (2)(3): single 4-line sentence broken into four short sentences.
```

### 5.2 Methodology - fix grammar in operational-loop description

Lines 226-228

```
line: Raw data, features, and predictions persisted asynchronously to avoid blocking the real-time path. Concurrently, a background metaheuristic optimization loop updates the parameter tuple (Θ, S, D) to minimize the combined quality-cost objective.
new:  Raw data, features, and predictions are persisted asynchronously to avoid blocking the real-time path. Concurrently, a background metaheuristic optimization loop updates the parameter tuple (Θ, S, D) to minimize the combined quality-cost objective.
why:  Presentation (3): missing "are" - grammatical error.
```

### 5.3 Experimental setup - split the long "validation setup" sentence

Lines 270-279

```
line: The validation setup follows an engineering protocol, meaning the sessions are split into train, validation, and test partitions with subject- and time-based separation whenever possible. Weak labels from heuristic rules are used to bootstrap initial models, which are subsequently refined via model-assisted or protocol-assisted labeling. Flow-state predictions are validated against psychometric post-session scales such as the Flow State Scale (FSS) and the Flow-Kurzskala (FKS), and correlation with session-level CME statistics is reported. Given the stochastic nature of quantum circuit evaluation, confidence intervals across repeated runs are reported.
new:  The validation setup follows an engineering protocol. Sessions are split into train, validation, and test partitions with subject- and time-based separation whenever possible. Weak labels from heuristic rules bootstrap the initial models, which are then refined via model-assisted or protocol-assisted labelling. Where available, flow-state predictions are validated against psychometric post-session scales (FSS, FKS), and correlation with session-level CME statistics is reported. Given the stochastic nature of quantum circuit evaluation, confidence intervals across repeated runs are reported when multiple runs are available.
why:  Presentation (2)(3) and Data representation (2): clarifies that CIs are reported only when multiple runs exist - matches the actual single-subject experiment.
```

### 5.4 Results 6.2 - split the dense ablations / failure-modes paragraph

Lines 510-516

```
line: The architecture enables controlled ablations along several axes. Feature subsets can be varied by toggling the γ band on or off depending on hardware bandwidth. Circuit depth D and shot count S can be swept to trace prediction-quality versus latency trade-off curves. The modulation function form g(c, p) can be replaced with alternative formulations (e.g., multiplicative-only or learned nonlinear mappings), and the hybrid mixing parameter μ can be adjusted between 0 and 1 to explore the classical-quantum trust boundary. In deployment-oriented studies, sensitivity should be reported jointly for prediction quality and latency-cost curves, since resource settings can dominate practical behavior.
new:  The architecture enables controlled ablations along several axes:
      - feature subsets, by toggling the γ band on or off depending on hardware bandwidth;
      - circuit depth D and shot count S, to trace prediction-quality vs. latency trade-off curves;
      - modulation function form g(c, p), replaceable with multiplicative-only or learned nonlinear mappings;
      - hybrid mixing parameter μ ∈ [0, 1], to explore the classical-quantum trust boundary.
      In deployment-oriented studies, sensitivity should be reported jointly for prediction quality and latency-cost curves, because resource settings can dominate practical behaviour.
why:  Presentation (2): converts a dense paragraph into a readable enumeration.
```

---

## Section 6 - Terminology justification

Reviewer criteria addressed: Presentation (4), Significance (2), Originality (4).

### 6.1 Vernik unit - add "why a named unit" justification

Insert immediately after line 151 ("The notation follows SI typographic conventions.")

```
line: The notation follows SI typographic conventions.
new:  The notation follows SI typographic conventions. The Vernik symbol is introduced - rather than reusing μV²·s directly - so that session- and day-level cognitive expenditure can be cited as a single named magnitude in deployed analytics dashboards, mirroring how the kilowatt-hour names a derived energy magnitude even though it reduces to SI joules. The choice is therefore one of operational convenience and reproducible reporting, not of new physics.
why:  Presentation (4) and Originality (4): reviewer asked for clearer justification of newly introduced terms.
```

### 6.2 CME - tie operational definition to flow construct on first use

Lines 51-53

```
line: This paper addresses these issues with a unified framework for computing a standardized indicator named Computational Mental Energy (CME). CME captures EEG spectral activity modulated by task complexity and the probability of a target cognitive state known as flow [1], where flow probability is estimated by a variational quantum circuit based on the data re-uploading architecture [5].
new:  This paper addresses these issues with a unified framework for computing a standardized indicator named Computational Mental Energy (CME). CME is defined as an operational, EEG-derived quantity that combines aggregated spectral energy, task complexity, and an estimate of the probability of being in flow in the sense of Csikszentmihalyi [1]; the goal is not to claim a new psychological construct but to provide a measurable, reproducible analogue suitable for streaming analytics. Flow probability is estimated by a variational quantum circuit based on the data re-uploading architecture [5].
why:  Presentation (4) and Significance (2): grounds the CME concept in the existing flow construct so the term is positioned as operational, not metaphysical.
```

---

## Section 7 - Additions (not replacements)

### 7.1 Authors' Profiles - add photo placeholders per the latest template

Lines 741-764

For each of the three author bios, prepend a `[PHOTO]` placeholder on its own line and standardize the field order: photo, name, degree, affiliation, ORCID, bio, research interests. Example for Mykhailo Vernik:

```
line: Mykhailo Vernik, PhD student at the Computer Systems Software Department, Faculty of Program Systems and Applied Mathematics, National Technical University of Ukraine "Igor Sikorsky Kyiv Polytechnic Institute", Ukraine. ...
new:  [PHOTO]
      Mykhailo Vernik, PhD student
      Affiliation: Computer Systems Software Department, Faculty of Program Systems and Applied Mathematics, National Technical University of Ukraine "Igor Sikorsky Kyiv Polytechnic Institute", Ukraine.
      ORCID: 0009-0008-6156-1051
      Bio: Entrepreneur, founder of the startup Sellsgram and MADJO company, winner of the hackathon at the Haiqu bootcamp (2024). Tech Lead in the machine learning department at JustAnswer. Speaker at Google Developer Groups; author and organizer of the workshop "Juggling It All: Two Jobs, a Startup, and Quantum Neural Networks", dedicated to building and using quantum neural networks for classification problems.
      Research interests: software engineering methods, neural networks, artificial intelligence, big data analytics, quantum computing and their applications.
why:  Editor instruction: "use the latest paper template and instructions in the attachment to add all author information and photo".
```

Apply the same restructuring to L. Oleshchenko (lines 751-757) and Z. Hu (lines 759-764). Insert the actual photo files when the template is in hand.

### 7.2 References - remove the Ukrainian TODO line

Line 734

```
line: відформатувати як у шаблоні, якщо є – додати пару джерел 2025-2026 і виділити їх жовтим
new:  (delete this line)
why:  Editorial: a Ukrainian-language TODO must not appear in the final manuscript; its action is satisfied by 7.3.
```

### 7.3 References - add three verified 2025-2026 entries (highlight in yellow)

Insertion point: after line 733 ("V. Havlicek et al., ... 2019."), before line 734.

All three citations below were verified via PubMed / arXiv / Scientific Reports / IEEE EMBC. They are highlighted yellow in the resubmitted manuscript per the editor's instruction. Each is placed where it most directly supports the paper's argument.

```
line: (no existing line - this is an insertion)
new:  [16] M. Beiramvand, R. Koivula, and T. Lipping, "Development of an EEG-Based Method for Detecting Flow State Using a Wearable Headband in a Game Environment," in Proc. 47th Annual International Conference of the IEEE Engineering in Medicine and Biology Society (EMBC), 2025. (PMID: 41335901)
      [17] B. Padmaja, B. Maram et al., "Hybrid quantum-classical framework for electroencephalogram-driven neurological processing in epileptic seizure taxonomy," Scientific Reports, vol. 16, art. 5305, 2026. doi:10.1038/s41598-026-36121-0.
      [18] N. Mayo, T. Mor, and Y. Weinstein, "Benchmarking quantum computers via protocols: comparing IBM's Heron vs IBM's Eagle," arXiv:2603.04377, 2026.
why:  References (2)(3) and Editor instruction (Ukrainian TODO at line 734 asked for 2025-2026 sources highlighted in yellow).
```

**Why each paper is cited and where it slots into the manuscript:**

- **[16] Beiramvand et al., 2025 (EMBC).** Detects flow state from a *consumer-grade wearable EEG headband* during Tetris gameplay using DWT + entropy features and Random Forest, reaching 93% within-subject and 82% LOSO accuracy on 29 subjects. This is a very direct comparator for the present paper's wearable-EEG flow detection on a Muse Athena headband; it also provides cross-subject (LOSO) numbers that the present paper currently lacks, which strengthens the limitations / future-work argument.

  *Insertion in the body:* extend the Related Work passage at lines 80-82, immediately after the existing reference to M. Cherep et al. [7]:

  ```
  line: ...as shown by M. Cherep et al. [7], who estimated flow states during video game play.
  new:  ...as shown by M. Cherep et al. [7], who estimated flow states during video game play. More recently, M. Beiramvand et al. [16] reported a wavelet-entropy method for flow detection on a consumer-grade EEG headband, reaching 93% within-subject and 82% leave-one-subject-out accuracy across 29 participants - results that motivate the cross-subject validation flagged as future work in Section 7.
  ```

- **[17] Padmaja, Maram et al., 2026 (Scientific Reports).** A *hybrid quantum-classical neural framework* (HQCNF) applied to EEG, using continuous wavelet transform scalograms and quantum-inspired layers to classify epileptic seizure types. Sits naturally next to the existing references to C. Olvera et al. [10] and P. Hernandez-Arango et al. [11], and is the closest 2026 analogue to this paper's hybrid quantum-classical EEG architecture - useful for the "the hybrid QML for EEG line of work is now actively published" framing.

  *Insertion in the body:* extend the Related Work passage at lines 85-87, immediately after the existing reference to P. Hernandez-Arango et al. [11]:

  ```
  line: ...and P. Hernandez-Arango et al. [11] demonstrated QEEGNet for EEG encoding tasks.
  new:  ...and P. Hernandez-Arango et al. [11] demonstrated QEEGNet for EEG encoding tasks. B. Padmaja et al. [17] further extended the hybrid quantum-classical line to clinical EEG with a wavelet-scalogram pipeline for epileptic seizure taxonomy, indicating that hybrid quantum-classical EEG architectures are now an active line of work in 2026.
  ```

- **[18] Mayo, Mor, Weinstein, 2026 (arXiv).** A protocol-level *benchmarking* study that compares IBM's Eagle and Heron generations and reports substantial performance improvements in Heron - directly relevant to the present paper's hardware validation on the Heron-r2 Marrakesh QPU.

  *Insertion in the body:* extend Section 6.2 IBM Quantum Hardware Validation at lines 446-447, immediately after the introduction of the Heron r2 hardware:

  ```
  line: To validate the framework's behaviour under real quantum hardware conditions, the VQC was executed on the IBM Marrakesh processor - a 156-qubit Heron r2 superconducting quantum computer accessed via the IBM Quantum Platform.
  new:  To validate the framework's behaviour under real quantum hardware conditions, the VQC was executed on the IBM Marrakesh processor - a 156-qubit Heron r2 superconducting quantum computer accessed via the IBM Quantum Platform. The Heron generation has been independently benchmarked against the older Eagle architecture and shown to provide substantial protocol-level performance gains [18], which motivates targeting Heron-class hardware for current variational workloads.
  ```

### 7.4 Declarations - add the missing Informed Consent Statement

Insertion point: immediately after line 686 ("...standard research practices.") and before "Acknowledgements" on line 687.

```
line: (no existing line - this is an insertion)
new:  Informed Consent Statement
      Not applicable. The EEG dataset used in this study was self-recorded by one of the authors as part of system validation; no external human subjects participated, and no personally identifying biometric data were collected from third parties.
why:  Editor instruction: "please add all the statements and declarations; if there is no corresponding part, write 'None' or 'Not applicable'." The current manuscript has Author Contribution, Conflict of Interest, Funding, Data Availability, Ethical, Acknowledgements, and Generative AI declarations - but no Informed Consent Statement, which the latest template typically requires.
```

If the latest template requires any further declarations not listed above (e.g., `Use of Human or Animal Subjects`, `Code Availability`, `Reproducibility Statement`), add them in the same block with `Not applicable` or a one-line description as required.

---

## Section 8 - Highlight color reminder

Per the editor's final instruction:

> "Upon submitting a corrected manuscript, all aforementioned modifications should be highlighted in a single color to aid in the verification of the alterations."

Action for the author:

1. Pick one highlight colour (yellow recommended - matches the existing TODO note on line 734).
2. Apply it to every change made from Sections 1-7 above, including inserted sentences and the new references [16]-[18].
3. Prepare a short cover letter / point-by-point response that maps the reviewer's bullets to Sections 1-7 of this document. Each section heading here can serve as one item in the response letter.

---

## Coverage map (reviewer criteria → edit IDs)

| Reviewer criterion | Score in review | Addressed by edits |
|---|---|---|
| Originality (1) | minor revision | 1.2 |
| Significance (2) | minor revision | 1.5, 2.3, 4.1, 6.2 |
| Presentation (2)(3) | minor revision (verbose, grammar) | 5.1, 5.2, 5.3, 5.4 |
| Presentation (4) | minor revision (terminology) | 6.1, 6.2 |
| Abstract (1)(2)(3) | minor revision (overload, readiness) | 1.1, 2.1, 2.2, 2.3 |
| Results (3) | minor revision (overinterpretation) | 1.3, 1.4, 3.1, 3.5 |
| Results (4) | minor revision (speculative) | 3.4, 4.1, 4.2 |
| Conclusions (2)(3) | minor revision (overstatements) | 1.5, 1.6, 4.3 |
| Data representation (2) | minor revision (small data, no CIs) | 3.1, 3.3, 3.6, 5.3 |
| Graphs/figures (2) | minor revision (super-linear, hardware advantage) | 1.4, 3.2, 3.3 |
| References (2)(3) | minor revision (latest works) | 7.2, 7.3 |
| Editor: latest template, photos, declarations | required | 7.1, 7.4 |
| Editor: highlight color, response letter | required | Section 8 |

Total: 27 edit entries across 7 active sections, plus the highlight-colour reminder.
