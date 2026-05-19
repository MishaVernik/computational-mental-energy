"""Configuration for quantum backend service."""
import os
from dotenv import load_dotenv

load_dotenv()


class Config:
    """Configuration settings for QPU simulation."""
    
    # Latency simulation (milliseconds)
    QPU_LATENCY_MIN_MS = int(os.getenv("QPU_LATENCY_MIN_MS", "300"))
    QPU_LATENCY_MAX_MS = int(os.getenv("QPU_LATENCY_MAX_MS", "2000"))
    
    # Quantum circuit settings
    DEFAULT_SHOTS = int(os.getenv("DEFAULT_SHOTS", "1024"))
    NUM_QUBITS = 4
    NUM_FEATURES = 8
    NUM_REUPLOAD_LAYERS = int(os.getenv("NUM_REUPLOAD_LAYERS", "2"))
    PARAMS_PER_LAYER = 8  # Ry + Rz per qubit = 2 * NUM_QUBITS

    # QPU noise simulation (test mode only; disabled when using real backend)
    SIMULATE_QPU_NOISE = os.getenv("SIMULATE_QPU_NOISE", "true").lower() == "true"

    # IBM Quantum (optional)
    IBMQ_TOKEN = os.getenv("IBMQ_TOKEN", None)
    IBM_BACKEND_NAME = os.getenv("IBM_BACKEND_NAME", "ibm_kingston")
    IBM_INSTANCE_CRN = os.getenv("IBM_INSTANCE_CRN", None)
    USE_REAL_BACKEND = os.getenv("USE_REAL_BACKEND", "false").lower() == "true"
    
    @classmethod
    def get_latency_range_seconds(cls) -> tuple[float, float]:
        """Get latency range in seconds."""
        return (cls.QPU_LATENCY_MIN_MS / 1000.0, cls.QPU_LATENCY_MAX_MS / 1000.0)


config = Config()


