# Quantum Backend Service

Python FastAPI service that simulates a quantum computing backend for EEG flow state classification.

## Architecture

This service provides:
- **POST /qpu/infer**: Execute quantum circuit with EEG features → return p_flow
- **GET /health**: Service health check
- **GET /stats**: Monitoring endpoint (placeholder)

## Quantum Circuit

Simple 4-qubit circuit:
1. **Angle encoding**: Features → R_y rotations
2. **Entangling layer**: CX gates
3. **Variational ansatz**: R_y, R_z with fixed parameters (simulates trained model)
4. **Measurement**: All qubits

The probability p_flow is extracted from measurement statistics (first qubit = |1>).

## Installation

```bash
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt
```

## Configuration

Copy `.env.example` to `.env` and adjust:

```env
QPU_LATENCY_MIN_MS=300      # Minimum simulated QPU delay
QPU_LATENCY_MAX_MS=2000     # Maximum simulated QPU delay
DEFAULT_SHOTS=1024          # Number of circuit shots
IBMQ_TOKEN=                 # Optional: IBM Quantum token
```

## Running

```bash
# Development
uvicorn app.main:app --reload --port 8001

# Production
uvicorn app.main:app --host 0.0.0.0 --port 8001 --workers 4
```

## Testing

```bash
# Health check
curl http://localhost:8001/health

# Inference
curl -X POST http://localhost:8001/qpu/infer \
  -H "Content-Type: application/json" \
  -d '{"features": [0.5, -0.3, 0.8, 0.1], "modelType": "QSVC"}'
```

Expected response:
```json
{
  "pFlow": 0.623,
  "shotsUsed": 1024,
  "depth": 8,
  "qpuLatencyMs": 1456
}
```

## Imitation Model Notes

This is NOT a production quantum ML service. It simulates:
- **Realistic latency**: Random delays (300-2000ms) to imitate QPU queue
- **Measurement statistics**: Probability extraction from quantum state
- **Circuit execution**: Uses Qiskit Aer simulator (fast, local)

For real quantum ML:
- Train actual QSVC/VQC model with labeled EEG data
- Use IBM Quantum hardware (set IBMQ_TOKEN)
- Implement proper error mitigation
- Handle circuit transpilation and optimization


