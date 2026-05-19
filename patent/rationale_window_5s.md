# Why the default EEG analysis window is 5 seconds

## 1. Frequency resolution constraint

Spectral analysis via FFT/Welch yields a frequency resolution of:

    Δf = 1 / T

where T is the window duration in seconds.

| T (s) | Δf (Hz) | Can resolve δ (1–4 Hz)? |
|-------|---------|-------------------------|
| 1     | 1.0     | Only 3 bins – very coarse |
| 2     | 0.5     | 6 bins – marginal |
| 5     | 0.2     | 15 bins – sufficient |
| 10    | 0.1     | 30 bins – good, but too slow for real-time |

The δ-band (1–4 Hz) is the narrowest and lowest-frequency band used in CME.
At T = 1 s the entire δ-band collapses to just 3 spectral bins, making the
power estimate extremely noisy. At T = 5 s we get 15 bins inside [1, 4] Hz,
which is the practical minimum for a reliable Welch PSD estimate of δ-power.

## 2. Welch estimator variance

The Welch method splits the window into overlapping sub-segments of length
L (typically L = N, i.e. one segment at the Muse 256 Hz sample rate) and
averages their periodograms. The variance of the PSD estimate scales as:

    Var[Ŝ(f)] ∝ S²(f) / K

where K is the number of averaged segments. With 50 % overlap:

    K ≈ 2N / L − 1

| T (s) | N = 256·T | L = 256 | K (50% overlap) |
|-------|-----------|---------|-----------------|
| 1     | 256       | 256     | 1               |
| 2     | 512       | 256     | 3               |
| 5     | 1280      | 256     | 9               |

At T = 1 s there is only K = 1 segment – no averaging at all, the raw
periodogram has 100 % relative standard deviation. At T = 5 s we get K ≈ 9
averaged segments, reducing variance by roughly 3× (√9 = 3).

## 3. Cramér–Rao lower bound for δ-band power

For any unbiased estimator of band power P_δ, the minimum achievable
variance is bounded by the Cramér–Rao inequality. For a Gaussian EEG
signal with B = 3 Hz bandwidth (the δ-band) observed over T seconds,
the number of independent spectral degrees of freedom is:

    n = 2 · B · T

| T (s) | n = 2·3·T | Relative error floor ≈ 1/√n |
|-------|-----------|------------------------------|
| 1     | 6         | 41 %                         |
| 2     | 12        | 29 %                         |
| 5     | 30        | 18 %                         |
| 10    | 60        | 13 %                         |

At T = 1 s, even the theoretically best estimator cannot do better than
~41 % relative error on δ-power. At T = 5 s the floor drops to ~18 %,
which is acceptable for the downstream CME computation.

## 4. Error propagation into CME

CME_rate(t) = κ · E_band(t) · g(c, p_flow)

If E_band has relative error σ_E / E_band ≈ 18 % (at T = 5 s), this
propagates linearly into CME. At T = 1 s the ~41 % spectral noise would
dominate the CME signal, making the flow-modulation component g(c, p)
practically unobservable.

## 5. Real-time latency trade-off

| T (s) | Update rate | UX latency |
|-------|-------------|------------|
| 1     | 1 Hz        | Responsive but noisy |
| 5     | 0.2 Hz      | Acceptable for dashboard, reliable |
| 10    | 0.1 Hz      | Too sluggish for interactive use |

A 5-second window provides an update every 5 s (or more frequently with
sliding overlap), which is fast enough for real-time monitoring dashboards
while being spectrally reliable.

Additionally, QPU inference latency T_qpu (typically 0.2–2 s per window)
must fit within the window period. At T = 1 s, even moderate QPU latency
would create a processing bottleneck.

## 6. Sliding window option

The 5-second window does not require non-overlapping segmentation.
A sliding window with step < Δ (e.g., 1-second step with 5-second span)
can provide more frequent updates while maintaining the spectral quality
of the longer window:

    |-----5s-----|
         |-----5s-----|
              |-----5s-----|
    t=0  t=1  t=2  t=3  t=4  t=5  t=6 ...

Each output still uses 5 s of data (preserving frequency resolution and
variance), but new results arrive every 1 s. The CME formula remains
unchanged – Δ = 5 s is the integration window, not the update interval.

## 7. Summary

| Criterion | T = 1 s | T = 5 s | T = 10 s |
|-----------|---------|---------|----------|
| δ-band bins | 3 | 15 | 30 |
| Welch segments K | 1 | 9 | 19 |
| CR error floor (δ) | 41 % | 18 % | 13 % |
| CME noise | Dominates signal | Acceptable | Diminishing returns |
| UX update latency | Fast but noisy | Balanced | Too slow |
| QPU latency margin | Tight | Comfortable | Excessive |

**Δ = 5 s is the smallest window that simultaneously satisfies:**
- sufficient frequency resolution for all EEG bands including δ (1–4 Hz);
- enough Welch averaging to bring spectral variance below ~20 %;
- acceptable latency for real-time interactive use;
- headroom for QPU inference within the window period.
