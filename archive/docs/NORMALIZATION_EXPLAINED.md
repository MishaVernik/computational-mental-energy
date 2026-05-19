# Normalization Explained: How It Works & Unified Formula

## Current Normalization Methods

### Method 1: Min-Max Normalization (for EEG bands)

**Current Implementation**:
```csharp
// Step 1: Log transform (handles wide range)
log_alpha = log10(raw_alpha + 1)  // e.g., 15 μV²/Hz → 1.20

// Step 2: Min-max to [0, 1]
normalized_01 = log_alpha / 2.0  // Assuming log range [0, 2]
// Result: 1.20 / 2.0 = 0.6

// Step 3: Map to [-1, 1]
normalized = normalized_01 * 2 - 1  // 0.6 * 2 - 1 = 0.2
```

**Formula**:
```
normalized = (log_value / max_log_value) * 2 - 1
```

### Method 2: Ratio Normalization (for asymmetry)

**Current Implementation**:
```csharp
// Frontal asymmetry
frontal_asym = (alpha - beta) / (alpha + beta + 1)
// Result: Already in [-1, 1] range (approximately)
```

**Formula**:
```
normalized = (A - B) / (A + B + ε)
```
Where ε = 1 (prevents division by zero)

### Method 3: Z-Score Normalization (mentioned in docs)

**Formula**:
```
normalized = (value - mean) / std_dev
clipped = clip(normalized, -3, 3) / 3  // Scale to [-1, 1]
```

---

## Unified Normalization Formula with General Denominator

### Option 1: Unified Min-Max Formula

**General Formula**:
```
normalized_i = 2 × (raw_i - min_i) / (max_i - min_i) - 1
```

**Where**:
- `raw_i` = raw value of feature i
- `min_i` = minimum expected value for feature i
- `max_i` = maximum expected value for feature i
- Result: `normalized_i` ∈ [-1, 1]

**Example for Alpha**:
```
Raw alpha: 15 μV²/Hz
Expected range: [0.1, 100] μV²/Hz

normalized_alpha = 2 × (15 - 0.1) / (100 - 0.1) - 1
                 = 2 × 14.9 / 99.9 - 1
                 = 2 × 0.149 - 1
                 = 0.298 - 1
                 = -0.702
```

**For all features**:
```
normalized = [
    2 × (alpha - alpha_min) / (alpha_max - alpha_min) - 1,
    2 × (beta - beta_min) / (beta_max - beta_min) - 1,
    2 × (theta - theta_min) / (theta_max - theta_min) - 1,
    2 × (delta - delta_min) / (delta_max - delta_min) - 1,
    2 × (frontal_asym - asym_min) / (asym_max - asym_min) - 1,
    2 × (parietal_asym - asym_min) / (asym_max - asym_min) - 1,
    2 × (hrv - hrv_min) / (hrv_max - hrv_min) - 1,
    2 × (engagement - eng_min) / (eng_max - eng_min) - 1
]
```

**Common Denominator Approach**:
We can factor out the denominator structure:

```
normalized_i = 2 × (raw_i - min_i) / range_i - 1

Where: range_i = max_i - min_i
```

---

### Option 2: Unified Z-Score Formula

**General Formula**:
```
normalized_i = clip((raw_i - μ_i) / σ_i, -3, 3) / 3
```

**Where**:
- `μ_i` = mean of feature i (from training data)
- `σ_i` = standard deviation of feature i
- Result: `normalized_i` ∈ [-1, 1]

**For all features**:
```
normalized = [
    clip((alpha - μ_alpha) / σ_alpha, -3, 3) / 3,
    clip((beta - μ_beta) / σ_beta, -3, 3) / 3,
    clip((theta - μ_theta) / σ_theta, -3, 3) / 3,
    clip((delta - μ_delta) / σ_delta, -3, 3) / 3,
    clip((frontal_asym - μ_fasym) / σ_fasym, -3, 3) / 3,
    clip((parietal_asym - μ_pasym) / σ_pasym, -3, 3) / 3,
    clip((hrv - μ_hrv) / σ_hrv, -3, 3) / 3,
    clip((engagement - μ_eng) / σ_eng, -3, 3) / 3
]
```

**Common Denominator Structure**:
All features use the same formula structure:
```
normalized_i = clip(z_score_i, -3, 3) / 3
```

---

### Option 3: Unified Log-Scale Formula (Best for EEG)

**General Formula**:
```
normalized_i = 2 × log10(raw_i + ε) / log10(max_i + ε) - 1
```

**Where**:
- `ε` = small constant (e.g., 1) to handle zero values
- `max_i` = maximum expected value
- Result: `normalized_i` ∈ [-1, 1]

**For all features**:
```
normalized = [
    2 × log10(alpha + 1) / log10(alpha_max + 1) - 1,
    2 × log10(beta + 1) / log10(beta_max + 1) - 1,
    2 × log10(theta + 1) / log10(theta_max + 1) - 1,
    2 × log10(delta + 1) / log10(delta_max + 1) - 1,
    2 × log10(|frontal_asym| + 1) / log10(asym_max + 1) - 1,
    2 × log10(|parietal_asym| + 1) / log10(asym_max + 1) - 1,
    2 × log10(hrv + 1) / log10(hrv_max + 1) - 1,
    2 × log10(|engagement| + 1) / log10(eng_max + 1) - 1
]
```

**Common Denominator**: All use `log10(max_i + 1)` structure

---

## Recommended Unified Formula

### Single Formula for All Features

**Proposed Unified Formula**:
```
normalized_i = 2 × (log_transform(raw_i) - log_min_i) / (log_max_i - log_min_i) - 1
```

**Where**:
- `log_transform(x) = log10(x + ε)` for power values
- `log_transform(x) = x` for ratios (already normalized)
- `log_min_i` and `log_max_i` are feature-specific bounds

**Implementation**:
```csharp
public static double NormalizeFeature(double rawValue, double minValue, double maxValue, bool useLogTransform = true)
{
    double transformed;
    
    if (useLogTransform)
    {
        // For EEG power bands (wide range)
        transformed = Math.Log10(rawValue + 1);
        double logMin = Math.Log10(minValue + 1);
        double logMax = Math.Log10(maxValue + 1);
        return 2 * (transformed - logMin) / (logMax - logMin) - 1;
    }
    else
    {
        // For ratios (already in reasonable range)
        return 2 * (rawValue - minValue) / (maxValue - minValue) - 1;
    }
}
```

---

## Complete Normalization Table

| Feature | Raw Range | Transform | Normalized Range | Formula |
|---------|-----------|-----------|------------------|---------|
| **Alpha** | 0.1-100 μV²/Hz | log10(x+1) | [-1, 1] | `2 × (log10(α+1) - log10(0.1+1)) / (log10(100+1) - log10(0.1+1)) - 1` |
| **Beta** | 0.1-100 μV²/Hz | log10(x+1) | [-1, 1] | `2 × (log10(β+1) - log10(0.1+1)) / (log10(100+1) - log10(0.1+1)) - 1` |
| **Theta** | 0.1-100 μV²/Hz | log10(x+1) | [-1, 1] | `2 × (log10(θ+1) - log10(0.1+1)) / (log10(100+1) - log10(0.1+1)) - 1` |
| **Delta** | 0.1-100 μV²/Hz | log10(x+1) | [-1, 1] | `2 × (log10(δ+1) - log10(0.1+1)) / (log10(100+1) - log10(0.1+1)) - 1` |
| **Frontal Asym** | -1 to 1 | Direct | [-1, 1] | `(α - β) / (α + β + 1)` |
| **Parietal Asym** | -1 to 1 | Direct | [-1, 1] | `(θ - α) / (θ + α + 1)` |
| **HRV** | 30-80 ms | Linear | [-1, 1] | `2 × (hrv - 30) / (80 - 30) - 1` |
| **Engagement** | -1 to 1 | Direct | [-1, 1] | `(β - α) / (β + α + 1)` |

---

## General Denominator Formula

### Unified Structure

**All features can use this general form**:
```
normalized_i = 2 × (transformed_i - min_transformed_i) / (max_transformed_i - min_transformed_i) - 1
```

**Where**:
- `transformed_i = f(raw_i)` (log transform, identity, etc.)
- `min_transformed_i` = minimum of transformed feature i
- `max_transformed_i` = maximum of transformed feature i

**Common Denominator**: `(max_transformed_i - min_transformed_i)`

**For Energy Calculation**:
```
Energy = Σ |normalized_i|
       = Σ |2 × (transformed_i - min_i) / (max_i - min_i) - 1|
```

**We can factor out the denominator structure**:
```
Energy = Σ |2 × (transformed_i - min_i) / range_i - 1|
       = Σ |(2 × transformed_i - 2 × min_i) / range_i - 1|
       = Σ |(2 × transformed_i - 2 × min_i - range_i) / range_i|
       = Σ |2 × transformed_i - 2 × min_i - range_i| / range_i
```

**But this doesn't simplify nicely** because each feature has different `range_i`.

---

## Practical Implementation: Unified Normalizer

```csharp
public class UnifiedFeatureNormalizer
{
    // Feature-specific parameters
    private readonly Dictionary<string, FeatureParams> _params;
    
    public UnifiedFeatureNormalizer()
    {
        _params = new Dictionary<string, FeatureParams>
        {
            ["alpha"] = new FeatureParams { Min = 0.1, Max = 100, UseLog = true },
            ["beta"] = new FeatureParams { Min = 0.1, Max = 100, UseLog = true },
            ["theta"] = new FeatureParams { Min = 0.1, Max = 100, UseLog = true },
            ["delta"] = new FeatureParams { Min = 0.1, Max = 100, UseLog = true },
            ["frontal_asym"] = new FeatureParams { Min = -1, Max = 1, UseLog = false },
            ["parietal_asym"] = new FeatureParams { Min = -1, Max = 1, UseLog = false },
            ["hrv"] = new FeatureParams { Min = 30, Max = 80, UseLog = false },
            ["engagement"] = new FeatureParams { Min = -1, Max = 1, UseLog = false }
        };
    }
    
    public double Normalize(string featureName, double rawValue)
    {
        var p = _params[featureName];
        double transformed = p.UseLog ? Math.Log10(rawValue + 1) : rawValue;
        double minTransformed = p.UseLog ? Math.Log10(p.Min + 1) : p.Min;
        double maxTransformed = p.UseLog ? Math.Log10(p.Max + 1) : p.Max;
        
        // Unified formula
        return 2 * (transformed - minTransformed) / (maxTransformed - minTransformed) - 1;
    }
    
    private class FeatureParams
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public bool UseLog { get; set; }
    }
}
```

---

## Summary

### How Normalization Works

1. **Transform** raw values (log for wide ranges, identity for ratios)
2. **Scale** to [0, 1] using min-max: `(value - min) / (max - min)`
3. **Map** to [-1, 1]: `scaled * 2 - 1`

### Unified Formula

**Yes, we can use a general denominator!**

```
normalized_i = 2 × (transformed_i - min_i) / (max_i - min_i) - 1
```

**Common structure**: All features use `(max_i - min_i)` as denominator

**Benefits**:
- ✅ Consistent formula for all features
- ✅ Easy to understand and implement
- ✅ Maintainable (change ranges in one place)
- ✅ Mathematically sound

**Note**: Each feature still needs its own `min_i` and `max_i` because they measure different things, but the **formula structure** is unified!

