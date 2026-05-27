"""
Muse Athena EEG Bridge
======================
Receives EEG data from MindMonitor (via OSC) or simulated source,
and streams windowed band powers to CmeSim.Api SignalR hub
for real-time CME computation.

Usage:
    python bridge.py --osc                      # MindMonitor OSC (default port 5000)
    python bridge.py --osc --osc-port 5000      # MindMonitor OSC on custom port
    python bridge.py --simulate                  # Simulated data for testing
    python bridge.py --osc --hub-url http://192.168.1.10:5000/eeg-stream
"""

import asyncio
import argparse
import logging
import threading
import time
from datetime import datetime, timezone
from collections import deque
from typing import Optional

import numpy as np
import yaml

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s [%(levelname)s] %(name)s: %(message)s'
)
logger = logging.getLogger('muse-bridge')

# ─── Configuration ────────────────────────────────────────────────

def load_config(path: str = 'config.yaml') -> dict:
    try:
        with open(path, 'r') as f:
            return yaml.safe_load(f)
    except FileNotFoundError:
        logger.warning(f"Config file {path} not found, using defaults")
        return {}

ELECTRODE_NAMES = ['TP9', 'AF7', 'AF8', 'TP10']


def _log_local_ips(osc_port: int):
    """Print this machine's local IPs so user knows what to put in MindMonitor."""
    import socket
    try:
        # Get hostname and resolve to IPs
        hostname = socket.gethostname()
        # Connect to external address to discover our local IP (doesn't send data)
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        try:
            s.connect(("8.8.8.8", 80))
            local_ip = s.getsockname()[0]
            logger.info(f"  Use this IP in MindMonitor: {local_ip} (port {osc_port})")
        except Exception:
            pass
        finally:
            s.close()
    except Exception as e:
        logger.warning(f"  Could not detect local IP: {e}")
BAND_NAMES = ['delta', 'theta', 'alpha', 'beta', 'gamma']

BANDS = {
    'delta': (1, 4),
    'theta': (4, 8),
    'alpha': (8, 13),
    'beta':  (13, 30),
    'gamma': (30, 45),
}

# ─── Physiological Reference Table ────────────────────────────────
# Per-electrode band power thresholds (linear μV², integrated Welch PSD,
# consumer dry-electrode EEG). Sources: Katahira 2018, Raufi & Longo 2022,
# Pope 1995, standard artifact literature.
#   clean:    upper bound of normal cortical activity
#   artifact: soft cap – values above this are likely blink/EMG but still usable
#   reject:   hard cap – values above this indicate bad contact or saturation
EEG_LIMITS = {
    'delta': {'clean': 30,  'artifact': 100, 'reject': 500},
    'theta': {'clean': 20,  'artifact': 50,  'reject': 200},
    'alpha': {'clean': 50,  'artifact': 100, 'reject': 500},
    'beta':  {'clean': 15,  'artifact': 50,  'reject': 200},
    'gamma': {'clean': 5,   'artifact': 20,  'reject': 100},
}
EEG_LIMITS_BELS = {
    'delta': {'clean': 1.48, 'artifact': 2.0, 'reject': 2.7},
    'theta': {'clean': 1.30, 'artifact': 1.7, 'reject': 2.3},
    'alpha': {'clean': 1.70, 'artifact': 2.0, 'reject': 2.7},
    'beta':  {'clean': 1.18, 'artifact': 1.7, 'reject': 2.3},
    'gamma': {'clean': 0.70, 'artifact': 1.3, 'reject': 2.0},
}
TOTAL_POWER_REJECT = 1000  # per-channel total linear μV²


# ═══════════════════════════════════════════════════════════════════
# MindMonitor OSC Source
# ═══════════════════════════════════════════════════════════════════

class MindMonitorOSC:
    """
    Receives EEG data from MindMonitor app via OSC protocol.

    Primary path: MindMonitor's pre-computed absolute band powers (Bels)
    sent on /muse/elements/*_absolute at 10 Hz, with built-in blink/jaw
    clench rejection. Converted to linear μV² via 10^bels.

    Fallback path: raw EEG on /muse/eeg → Welch PSD (used only when
    MindMonitor doesn't send absolute band powers).

    Also uses MindMonitor contact quality signals:
      /muse/elements/horseshoe  – per-channel 1=Good 2=OK 3=Bad
      /muse/elements/is_good    – per-channel binary 0/1
      /muse/elements/touching_forehead – headband on/off
    """

    def __init__(self, osc_port: int = 7002, window_seconds: float = 1.0, sample_rate: int = 256):
        self.osc_port = osc_port
        self.window_seconds = window_seconds
        self.sample_rate = sample_rate
        self._connected = False
        self._receiving = False
        self._last_receive = 0.0
        self._lock = threading.Lock()
        self._server = None
        self._server_thread = None

        # Raw EEG sample buffers (fallback path)
        buf_size = sample_rate * 10
        self._raw_buffers = [deque(maxlen=buf_size) for _ in range(4)]

        # MindMonitor absolute band power buffers (Bels, primary path)
        # {band_name: [deque_ch0, deque_ch1, deque_ch2, deque_ch3]}
        # MindMonitor sends ~10 Hz; buffer holds enough for window_seconds averaging
        buf_band = max(20, int(window_seconds * 12))
        self._band_buffers = {
            b: [deque(maxlen=buf_band) for _ in range(4)] for b in BAND_NAMES
        }
        self._band_receiving = False

        # Contact quality from MindMonitor
        self._horseshoe = [1, 1, 1, 1]   # 1=Good, 2=OK, 3=Bad
        self._is_good = [1, 1, 1, 1]     # binary per-channel
        self._touching = False
        self._battery: Optional[float] = None  # [0,1] from /muse/batt

    async def connect(self):
        from pythonosc import dispatcher, osc_server

        disp = dispatcher.Dispatcher()

        disp.map("/muse/eeg", self._on_eeg)

        disp.map("/muse/elements/delta_absolute", self._on_band_power, "delta")
        disp.map("/muse/elements/theta_absolute", self._on_band_power, "theta")
        disp.map("/muse/elements/alpha_absolute", self._on_band_power, "alpha")
        disp.map("/muse/elements/beta_absolute", self._on_band_power, "beta")
        disp.map("/muse/elements/gamma_absolute", self._on_band_power, "gamma")

        disp.map("/muse/elements/horseshoe", self._on_horseshoe)
        disp.map("/muse/elements/is_good", self._on_is_good)
        disp.map("/muse/elements/touching_forehead", self._on_touching)
        disp.map("/muse/batt", self._on_battery)

        self._server = osc_server.ThreadingOSCUDPServer(
            ("0.0.0.0", self.osc_port), disp, bind_and_activate=False
        )
        self._server.allow_reuse_address = True
        self._server.server_bind()
        self._server.server_activate()

        self._server_thread = threading.Thread(target=self._server.serve_forever, daemon=True)
        self._server_thread.start()

        self._connected = True
        logger.info(f"OSC server listening on 0.0.0.0:{self.osc_port}")
        _log_local_ips(self.osc_port)
        logger.info("Waiting for MindMonitor data...")
        logger.info("  Configure MindMonitor: OSC Target IP → one of the IPs above")
        logger.info(f"  Configure MindMonitor: OSC Port → {self.osc_port}")
        logger.info("  MindMonitor Settings: OSC Stream Brainwaves → All Values")

    # ── OSC handlers ──────────────────────────────────────────────

    def _on_eeg(self, address, *args):
        """Buffer raw EEG samples (fallback path)."""
        values = []
        for a in args:
            if isinstance(a, (list, tuple)):
                values.extend(a)
            elif isinstance(a, (int, float)):
                values.append(float(a))

        with self._lock:
            for ch in range(min(4, len(values))):
                self._raw_buffers[ch].append(values[ch])
            self._last_receive = time.time()
            if not self._receiving:
                self._receiving = True
                logger.info(f"Receiving raw EEG from MindMonitor ({len(values)} values/packet)")

    def _on_band_power(self, address, *args):
        """Buffer MindMonitor absolute band powers (Bels, 4 floats per message)."""
        band_name = args[0] if args else None
        values = []
        for a in args[1:] if band_name else args:
            if isinstance(a, (int, float)):
                values.append(float(a))
            elif isinstance(a, (list, tuple)):
                values.extend(float(v) for v in a)

        if not band_name or band_name not in BAND_NAMES or len(values) < 4:
            return

        with self._lock:
            for ch in range(4):
                self._band_buffers[band_name][ch].append(values[ch])
            self._last_receive = time.time()
            if not self._band_receiving:
                self._band_receiving = True
                logger.info(f"Receiving MindMonitor absolute band powers (Bels)")

    def _on_horseshoe(self, address, *args):
        """Per-channel contact quality: 1=Good, 2=OK, 3=Bad."""
        values = []
        for a in args:
            if isinstance(a, (int, float)):
                values.append(int(a))
            elif isinstance(a, (list, tuple)):
                values.extend(int(v) for v in a)
        with self._lock:
            for i in range(min(4, len(values))):
                self._horseshoe[i] = values[i]

    def _on_is_good(self, address, *args):
        """Strict per-channel binary quality: 0 or 1."""
        values = []
        for a in args:
            if isinstance(a, (int, float)):
                values.append(int(a))
            elif isinstance(a, (list, tuple)):
                values.extend(int(v) for v in a)
        with self._lock:
            for i in range(min(4, len(values))):
                self._is_good[i] = values[i]

    def _on_touching(self, address, *args):
        """Headband on/off forehead."""
        val = args[0] if args else 0
        if isinstance(val, (list, tuple)):
            val = val[0] if val else 0
        with self._lock:
            self._touching = bool(int(val))

    def _on_battery(self, address, *args):
        """MindMonitor /muse/batt: per-doc 4 ints (charge%/4200/?/?). Normalize to [0,1]."""
        raw = []
        for a in args:
            if isinstance(a, (int, float)):
                raw.append(float(a))
            elif isinstance(a, (list, tuple)):
                raw.extend(float(v) for v in a)
        if not raw:
            return
        v = raw[0]
        # Heuristic: 0..1 already normalized, 0..100 percent, 0..10000 (Muse millivolts*100), else millivolts
        if v <= 1.0:
            level = max(0.0, min(1.0, v))
        elif v <= 100.0:
            level = v / 100.0
        elif v <= 10000.0:
            level = max(0.0, min(1.0, v / 10000.0))
        else:
            level = max(0.0, min(1.0, v / 4200.0))
        with self._lock:
            self._battery = level

    # ── Per-channel quality assessment ────────────────────────────

    @staticmethod
    def _compute_channel_quality(bands: dict, horseshoe_val: int) -> float:
        """
        Returns 0.0 (reject), 0.5 (artifact but usable), or 1.0 (clean).
        Combines Muse hardware contact status with band power plausibility.
        """
        if horseshoe_val >= 3:
            return 0.0
        total = sum(bands.values())
        if total > TOTAL_POWER_REJECT:
            return 0.0
        for band_name in BAND_NAMES:
            if bands.get(band_name, 0) > EEG_LIMITS[band_name]['reject']:
                return 0.0
        has_artifact = any(
            bands.get(b, 0) > EEG_LIMITS[b]['artifact'] for b in BAND_NAMES
        )
        if has_artifact:
            return 0.5
        return 1.0

    # ── Window extraction ─────────────────────────────────────────

    def _get_window_from_band_buffers(self) -> Optional[dict]:
        """Primary path: average buffered MindMonitor Bels → convert to linear μV²."""
        channels = {}
        for ch_idx, electrode in enumerate(ELECTRODE_NAMES):
            bands = {}
            has_data = True
            for band_name in BAND_NAMES:
                buf = self._band_buffers[band_name][ch_idx]
                if len(buf) == 0:
                    has_data = False
                    break
                avg_bels = np.mean(list(buf))
                bands[band_name] = float(10.0 ** avg_bels)
            if not has_data:
                return None
            channels[electrode] = bands
        return channels

    def _get_window_from_raw(self, window_seconds: float) -> Optional[dict]:
        """Fallback path: Welch PSD from raw EEG samples."""
        from scipy.signal import welch as welch_psd
        n_samples = int(window_seconds * self.sample_rate)
        channels = {}
        for ch_idx, electrode in enumerate(ELECTRODE_NAMES):
            buf = self._raw_buffers[ch_idx]
            if len(buf) < n_samples // 2:
                channels[electrode] = {b: 0.0 for b in BAND_NAMES}
                continue
            samples = np.array(list(buf)[-n_samples:])
            nperseg = min(len(samples), self.sample_rate)
            if nperseg < 16:
                channels[electrode] = {b: 0.0 for b in BAND_NAMES}
                continue
            freqs, psd = welch_psd(samples, fs=self.sample_rate,
                                   nperseg=nperseg, noverlap=nperseg // 2)
            bands = {}
            for band_name, (fmin, fmax) in BANDS.items():
                mask = (freqs >= fmin) & (freqs < fmax)
                bands[band_name] = float(np.trapz(psd[mask], freqs[mask])) if mask.any() else 0.0
            channels[electrode] = bands
        return channels

    def get_window(self, window_seconds: float = 1.0) -> Optional[tuple]:
        """
        Returns (channels, channel_quality, overall_quality, touching, source_mode)
        or None if no data is available.
        """
        with self._lock:
            if not self._receiving and not self._band_receiving:
                return None

            if time.time() - self._last_receive > 3.0:
                self._receiving = False
                self._band_receiving = False
                logger.warning("MindMonitor data stream interrupted")
                return None

            if self._band_receiving:
                channels = self._get_window_from_band_buffers()
                source_mode = "mindmonitor"
                if channels is None:
                    channels = self._get_window_from_raw(window_seconds)
                    source_mode = "welch"
            else:
                channels = self._get_window_from_raw(window_seconds)
                source_mode = "welch"

            if channels is None:
                return None

            horseshoe = list(self._horseshoe)
            touching = self._touching
            battery = self._battery

        ch_quality = {}
        for ch_idx, electrode in enumerate(ELECTRODE_NAMES):
            ch_quality[electrode] = self._compute_channel_quality(
                channels[electrode], horseshoe[ch_idx]
            )

        overall_quality = np.mean(list(ch_quality.values()))

        bad = [e for e, q in ch_quality.items() if q == 0.0]
        if bad:
            logger.debug(f"Bad contact: {bad} (horseshoe={horseshoe})")

        return channels, ch_quality, overall_quality, touching, source_mode, battery

    @property
    def is_connected(self):
        return self._connected

    @property
    def is_receiving(self):
        return self._receiving or self._band_receiving

    async def disconnect(self):
        if self._server:
            self._server.shutdown()
        self._connected = False


# ═══════════════════════════════════════════════════════════════════
# Simulated Source (for testing)
# ═══════════════════════════════════════════════════════════════════

class SimulatedMuse:
    """Generates realistic simulated EEG band powers for testing."""

    def __init__(self):
        self._t = 0
        self._connected = True
        self._receiving = True

    async def connect(self):
        logger.info("Using SIMULATED Muse data (no real device)")
        self._connected = True

    def get_window(self, window_seconds: float = 1.0):
        self._t += window_seconds
        channels = {}
        for name in ELECTRODE_NAMES:
            channels[name] = {
                'delta': 0.8 + 0.4 * np.random.random(),
                'theta': 0.2 + 0.15 * np.random.random(),
                'alpha': 0.3 + 0.2 * np.random.random() + 0.1 * np.sin(self._t * 0.3),
                'beta':  0.1 + 0.05 * np.random.random(),
                'gamma': 0.04 + 0.03 * np.random.random(),
            }
        ch_quality = {name: 1.0 for name in ELECTRODE_NAMES}
        return channels, ch_quality, 0.95, True, "simulated", 0.85

    @property
    def is_connected(self):
        return self._connected

    @property
    def is_receiving(self):
        return self._receiving

    async def disconnect(self):
        self._connected = False


# ═══════════════════════════════════════════════════════════════════
# SignalR Client
# ═══════════════════════════════════════════════════════════════════

class HubClient:
    """SignalR client that sends EEG windows to CmeSim.Api hub."""
    
    def __init__(self, hub_url: str):
        self.hub_url = hub_url
        self._connection = None
        self._ready = False
    
    async def connect(self):
        from pysignalr.client import SignalRClient
        
        logger.info(f"Connecting to SignalR hub: {self.hub_url}")
        self._connection = SignalRClient(self.hub_url)
        self._connection.on("Connected", self._on_connected)
        self._connection.on("SessionStarted", self._on_session_started)
        self._connection.on("ReceiveCmeResult", self._on_cme_result)
        self._connection.on("ReceiveRawEeg", self._on_raw_eeg)  # no-op, hub broadcasts to all clients
        self._connection.on("Error", self._on_error)
        
        asyncio.create_task(self._connection.run())
        
        # Wait for connection (API may take 15-20s to start)
        for i in range(60):  # 60 * 0.5 = 30 seconds max
            await asyncio.sleep(0.5)
            if self._ready:
                logger.info("SignalR connection established")
                return
        
        raise RuntimeError("Could not connect to SignalR hub after 30s. Is the API running on port 5000?")
    
    async def start_session(self, user_id: str = "muse-athena-user"):
        if self._connection:
            await self._connection.send("StartSession", [user_id])
    
    async def send_eeg_window(self, data: dict):
        if self._connection:
            await self._connection.send("SendEegWindow", [data])
    
    async def _on_connected(self, args):
        self._ready = True
        logger.info(f"Hub connected: {args}")
    
    async def _on_session_started(self, args):
        logger.info(f"Session started: {args}")
    
    async def _on_cme_result(self, args):
        if args and len(args) > 0:
            r = args[0] if isinstance(args, list) else args
            cme = r.get('cmeVn', r.get('cme', 0))
            pflow = r.get('pFlow', 0)
            is_flow = r.get('isFlow', False)
            latency = r.get('totalLatencyMs', 0)
            wclass = r.get('windowClass', '')
            state = "\033[92mFLOW\033[0m" if is_flow else "----"
            cls_tag = f"  [{wclass}]" if wclass else ""
            logger.info(f"  CME={cme:.2f} Vn  p_flow={pflow:.3f}  [{state}]{cls_tag}  latency={latency}ms")
    
    async def _on_raw_eeg(self, args):
        pass  # Hub broadcasts raw EEG to dashboards; bridge ignores

    async def _on_error(self, args):
        logger.error(f"Hub error: {args}")


# ═══════════════════════════════════════════════════════════════════
# Main
# ═══════════════════════════════════════════════════════════════════

async def main():
    parser = argparse.ArgumentParser(description='Muse Athena EEG Bridge')
    parser.add_argument('--simulate', action='store_true', help='Use simulated EEG data')
    parser.add_argument('--osc', action='store_true', help='Receive from MindMonitor via OSC')
    parser.add_argument('--osc-port', type=int, default=7002, help='OSC listen port (default: 7002)')
    parser.add_argument('--hub-url', type=str, help='Override SignalR hub URL')
    parser.add_argument('--config', type=str, default='config.yaml', help='Config file path')
    parser.add_argument('--difficulty', type=float, help='Task difficulty (0-1)')
    parser.add_argument('--window', type=float, help='Window duration in seconds')
    args = parser.parse_args()
    
    # Load config
    config = load_config(args.config)
    hub_url = args.hub_url or config.get('hub', {}).get('url', 'http://localhost:5000/eeg-stream')
    window_sec = args.window or config.get('streaming', {}).get('window_seconds', 1.0)
    difficulty = args.difficulty or config.get('streaming', {}).get('task_difficulty', 0.5)
    osc_port = args.osc_port or config.get('osc', {}).get('port', 7002)
    
    # Default to OSC mode if neither flag is set
    if not args.simulate and not args.osc:
        args.osc = True
    
    # Create data source
    if args.simulate:
        source = SimulatedMuse()
    else:
        source = MindMonitorOSC(osc_port=osc_port, window_seconds=window_sec)
    
    await source.connect()
    
    # Connect to SignalR hub
    hub = HubClient(hub_url)
    await hub.connect()
    await hub.start_session()
    
    source_name = "SIMULATED" if args.simulate else f"MindMonitor OSC :{osc_port}"
    logger.info(f"")
    logger.info(f"╔══════════════════════════════════════════════════╗")
    logger.info(f"║  Muse Athena Bridge – RUNNING                   ║")
    logger.info(f"║  Source:  {source_name:<40s}║")
    logger.info(f"║  Hub:     {hub_url:<40s}║")
    logger.info(f"║  Window:  {window_sec}s  |  Difficulty: {difficulty:<17}║")
    logger.info(f"╚══════════════════════════════════════════════════╝")
    logger.info(f"")
    
    if args.osc and not args.simulate:
        logger.info("Waiting for MindMonitor to start streaming...")
        logger.info("  1. Open MindMonitor on your phone")
        logger.info("  2. Connect to Muse Athena")
        logger.info(f"  3. Set OSC target IP to this computer's IP")
        logger.info(f"  4. Set OSC port to {osc_port}")
        logger.info("  5. Enable OSC streaming in MindMonitor")
        logger.info("")
    
    window_count = 0
    try:
        while source.is_connected:
            await asyncio.sleep(window_sec)

            result = source.get_window(window_sec)
            if result is None:
                continue

            channels, ch_quality, quality, touching, source_mode, battery = result

            if not touching and not args.simulate:
                logger.debug("Headband not on forehead, skipping window")
                continue

            window_count += 1

            sim_mode = args.simulate
            payload = {
                "timestamp": datetime.now(timezone.utc).isoformat(),
                "channels": channels,
                "channelQuality": ch_quality,
                "quality": float(quality),
                "taskDifficulty": difficulty,
                "touching": touching,
                "sourceMode": "simulator" if sim_mode else "live",
                "bandsSource": source_mode,
            }
            if battery is not None:
                payload["batteryLevel"] = float(battery)

            try:
                await hub.send_eeg_window(payload)
            except Exception as e:
                logger.error(f"Failed to send window: {e}")
                continue

            if window_count % 10 == 0:
                q_str = f"{quality:.0%}" if quality else "?"
                good_ch = sum(1 for q in ch_quality.values() if q > 0)
                logger.info(
                    f"  [{window_count} windows]  quality={q_str}  "
                    f"channels={good_ch}/4  mode={source_mode}  touching={touching}"
                )
    
    except KeyboardInterrupt:
        logger.info("\nStopping bridge...")
    finally:
        await source.disconnect()
        logger.info(f"Bridge stopped. Total windows sent: {window_count}")


if __name__ == '__main__':
    asyncio.run(main())
