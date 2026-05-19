# Muse Athena EEG Bridge

Python service that connects to a Muse Athena headband via Bluetooth LE,
extracts spectral band powers per electrode using FFT, and streams windowed
data to the CmeSim.Api SignalR hub for real-time CME computation.

## Setup

```bash
cd muse-bridge
pip install -r requirements.txt
```

## Usage

### With real Muse Athena device
```bash
python bridge.py
```

### With simulated data (for testing)
```bash
python bridge.py --simulate
```

### Custom options
```bash
python bridge.py --hub-url http://192.168.1.10:5000/eeg-stream --difficulty 0.7
```

## MindMonitor Not Receiving Data?

1. **Windows Firewall** – Run as Administrator:
   ```powershell
   .\allow-osc-firewall.ps1
   ```
   Or manually add an inbound rule: UDP port 7002, Allow.

2. **MindMonitor settings** – OSC Stream Brainwaves must be set to **"All Values"** (raw EEG). Default "Average" sends different OSC paths.

3. **IP address** – Use the IP printed when the bridge starts (e.g. `192.168.0.101`). Both phone and PC must be on the same WiFi.

4. **Port** – Must match: MindMonitor port = 7002 (or whatever `--osc-port` is).

## Configuration

Edit `config.yaml` to set device address, hub URL, window size, OSC port (default 7002), etc.
