# EEG Data Format Specification

## CSV Format

### Structure

```csv
timestamp,session_id,window_id,alpha,beta,theta,delta,frontal_asym,parietal_asym,hrv,engagement,task_difficulty,label
```

### Column Descriptions

| Column | Type | Required | Range/Format | Description |
|--------|------|----------|--------------|-------------|
| `timestamp` | float | Yes | Unix time | Recording timestamp (seconds since epoch) |
| `session_id` | string | Yes | Any string | Unique session identifier (e.g., "session_001") |
| `window_id` | string | Yes | Any string | Unique window identifier (e.g., "w_001") |
| `alpha` | float | Yes | [-1, 1] | Alpha band power (8-13 Hz) - relaxation, flow |
| `beta` | float | Yes | [-1, 1] | Beta band power (13-30 Hz) - active thinking |
| `theta` | float | Yes | [-1, 1] | Theta band power (4-8 Hz) - deep focus, creativity |
| `delta` | float | Yes | [-1, 1] | Delta band power (0.5-4 Hz) - deep processing |
| `frontal_asym` | float | Yes | [-1, 1] | Frontal lobe asymmetry (left-right) |
| `parietal_asym` | float | Yes | [-1, 1] | Parietal lobe asymmetry (left-right) |
| `hrv` | float | Yes | [-1, 1] | Heart rate variability (normalized) |
| `engagement` | float | Yes | [-1, 1] | Overall cognitive engagement score |
| `task_difficulty` | float | No | [0, 1] | Task complexity (0=easy, 1=very hard) |
| `label` | string | No | Flow/No_Flow | Ground truth classification label |

## Feature Extraction Process

### From Raw EEG to Features

**Step 1: Raw Signal Acquisition**
```
64 channels × 500 Hz = 32,000 samples/second
Example: Channel Fz (frontal midline) = [12.3, 11.8, 13.1, ...]  μV
```

**Step 2: Preprocessing**
- Bandpass filter: 0.5-50 Hz
- Notch filter: 50/60 Hz (power line noise)
- Artifact removal: ICA for eye blinks, muscle
- Re-reference: Average reference

**Step 3: Windowing**
- Segment into 1-second windows (500 samples per channel)
- 50% overlap (sliding window)

**Step 4: Feature Extraction**

**Alpha Power**:
```python
# FFT on 1-second window
frequencies, psd = welch(eeg_signal, fs=500)
alpha_range = (8 <= frequencies) & (frequencies <= 13)
alpha_power = mean(psd[alpha_range])  # μV²/Hz

# Log and normalize
alpha_log = log10(alpha_power + epsilon)
alpha_norm = (alpha_log - baseline_mean) / baseline_std
alpha_clipped = clip(alpha_norm, -1, 1)
```

Repeat for Beta, Theta, Delta.

**Asymmetry**:
```python
# Frontal asymmetry (F4 - F3)
left_frontal = mean([F3, F7, FC5])
right_frontal = mean([F4, F8, FC6])
frontal_asym = (right_frontal - left_frontal) / (right_frontal + left_frontal)
# Normalize to [-1, 1]
```

**HRV**:
```python
# From ECG or PPG sensor
rr_intervals = detect_r_peaks(ecg_signal)
hrv_rmssd = sqrt(mean((diff(rr_intervals))**2))
hrv_norm = (hrv_rmssd - 30) / 50  # Normalize ~30-80ms range
```

**Engagement**:
```python
# Composite score
engagement = 0.3 * beta + 0.3 * theta - 0.2 * alpha - 0.2 * delta
```

## Example Data Interpretation

### High Flow State Example

```csv
0.72,-0.55,0.95,0.03,-0.38,0.81,0.02,-0.62,1.0,Flow
```

**Interpretation**:
- High Alpha (0.72): Relaxed but focused
- Low Beta (-0.55): Not anxious
- High Theta (0.95): Deep creative thinking
- Low Delta (0.03): Alert, not drowsy
- Negative Frontal Asym (-0.38): Approach motivation
- High Parietal Asym (0.81): Spatial processing
- Low HRV (0.02): Stable heart rate
- Negative Engagement (-0.62): Effortless flow
- Very Hard Task (1.0): Challenging but manageable

**Result**: Classic flow state pattern

### No Flow Example

```csv
0.15,0.42,-0.62,0.31,0.87,-0.19,0.51,0.12,0.3,No_Flow
```

**Interpretation**:
- Low Alpha (0.15): Not relaxed
- High Beta (0.42): Anxious, stressed
- Low Theta (-0.62): No deep focus
- High Delta (0.31): Drowsy, tired
- High Frontal Asym (0.87): Withdrawal/avoidance
- Negative Parietal Asym (-0.19): Poor spatial processing
- High HRV (0.51): Heart rate variability (stress)
- Low Engagement (0.12): Disengaged
- Easy Task (0.3): Bored

**Result**: Stress or boredom, not flow

## Using CSV Data in the Dashboard

### Option 1: Paste Single Row

1. Copy values from CSV (columns 4-11):
```
0.52,-0.31,0.78,0.11,-0.23,0.61,0.05,-0.42
```

2. Format as JSON array:
```json
[0.52, -0.31, 0.78, 0.11, -0.23, 0.61, 0.05, -0.42]
```

3. Paste into "EEG Features" field

4. Set Task Difficulty from column 12

### Option 2: Upload CSV (Future Feature)

Will allow batch processing:
- Upload entire CSV
- Process all rows
- Show aggregate results

## Validation Rules

### Feature Values

All features must be in **[-1, 1]** range:
- If outside range, data is corrupted or not normalized
- Should reject or clip

### Required Columns

Minimum required for inference:
- 8 feature columns (alpha → engagement)
- session_id, window_id

Optional but useful:
- task_difficulty (defaults to 0.5 if missing)
- label (for training/validation, not needed for inference)

## Generating Synthetic Data

For testing without real EEG:

```python
import numpy as np
import pandas as pd

# Generate synthetic "flow" data
def generate_flow_window():
    return {
        'alpha': np.random.uniform(0.4, 0.8),      # High alpha
        'beta': np.random.uniform(-0.6, -0.2),     # Low beta
        'theta': np.random.uniform(0.6, 1.0),      # High theta
        'delta': np.random.uniform(-0.1, 0.2),     # Low delta
        'frontal_asym': np.random.uniform(-0.5, 0.0),  # Approach
        'parietal_asym': np.random.uniform(0.4, 0.9),  # High
        'hrv': np.random.uniform(-0.2, 0.2),       # Stable
        'engagement': np.random.uniform(-0.7, -0.3), # Effortless
        'label': 'Flow'
    }

# Generate synthetic "no flow" data
def generate_no_flow_window():
    return {
        'alpha': np.random.uniform(-0.2, 0.3),     # Low alpha
        'beta': np.random.uniform(0.2, 0.6),       # High beta (stress)
        'theta': np.random.uniform(-0.8, -0.3),    # Low theta
        'delta': np.random.uniform(0.2, 0.5),      # High delta (drowsy)
        'frontal_asym': np.random.uniform(0.5, 1.0),   # Withdrawal
        'parietal_asym': np.random.uniform(-0.4, 0.1), # Low
        'hrv': np.random.uniform(0.3, 0.7),        # Variable (stress)
        'engagement': np.random.uniform(0.0, 0.4), # Low
        'label': 'No_Flow'
    }

# Create balanced dataset
data = []
for i in range(50):
    if i % 2 == 0:
        row = generate_flow_window()
    else:
        row = generate_no_flow_window()
    row['timestamp'] = 1732038456.123 + i
    row['session_id'] = f'session_{(i // 10) + 1:03d}'
    row['window_id'] = f'w_{(i % 10) + 1:03d}'
    row['task_difficulty'] = np.random.uniform(0.3, 0.9)
    data.append(row)

df = pd.DataFrame(data)
df.to_csv('eeg_sample_data.csv', index=False)
```

## Real EEG Data Sources

If you want to use actual EEG data:

1. **Public Datasets**:
   - [DEAP](http://www.eecs.qmul.ac.uk/mmv/datasets/deap/) - Emotion recognition
   - [Physionet EEG Motor Movement](https://physionet.org/content/eegmmidb/) - Motor imagery
   - [CHB-MIT](https://physionet.org/content/chbmit/) - Seizure detection

2. **Recording Your Own**:
   - Use OpenBCI, Muse, or Emotiv headsets
   - Record during flow-inducing tasks (gaming, coding, art)
   - Self-report flow state after each session
   - Extract features using MNE-Python or EEGLAB

3. **Preprocessing Tools**:
   - [MNE-Python](https://mne.tools/) - Python library for EEG analysis
   - [EEGLAB](https://sccn.ucsd.edu/eeglab/) - MATLAB toolbox
   - [Brainstorm](https://neuroimage.usc.edu/brainstorm/) - GUI-based

## Summary

**For Dissertation**:
- Use `eeg_sample_data.csv` as example dataset
- Shows realistic feature ranges for flow vs no-flow
- Can be loaded in dashboard for batch inference
- Demonstrates the data format for Petri net model inputs

**For Real Research**:
- Replace with actual labeled EEG recordings
- Use for training and validation
- Measure real accuracy (not synthetic fitness)

