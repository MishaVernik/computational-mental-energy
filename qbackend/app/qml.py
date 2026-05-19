"""Quantum machine learning logic using Qiskit.

Circuit architecture (v2 – data re-uploading with ZZ feature interactions):

  For each re-upload layer l = 0 … L:
    1. Angle encoding:  Ry(f[i]*π)  and  Rz(f[i+4]*π)  on qubit i  (8 features)
    2. ZZ feature interaction:  RZZ((f[i]-f[i+1])²·π)  on adjacent pairs
    3. Ring entanglement:  CNOT(0→1→2→3→0)
    4. Variational ansatz:  Ry(θ), Rz(φ)  per qubit  (8 trained params per layer)

  Measurement on all qubits; p_flow = P(q₀ = |1⟩).

Total trainable parameters: 8 × (L+1) = 24 for L=2.
"""
import math
import time
import random
import logging
from typing import List, Dict, Any, Optional

import numpy as np
from qiskit import QuantumCircuit
from qiskit.transpiler.preset_passmanagers import generate_preset_pass_manager
from qiskit_aer import Aer

from .config import config

logger = logging.getLogger(__name__)

PI = math.pi


class QuantumInferenceEngine:
    """
    Quantum inference engine for flow state classification with data re-uploading,
    ZZ feature interactions, and ring entanglement.
    """

    def __init__(self):
        self.simulator = Aer.get_backend('qasm_simulator')
        self.n_qubits = config.NUM_QUBITS
        self.n_features = config.NUM_FEATURES
        self.n_layers = config.NUM_REUPLOAD_LAYERS
        self.params_per_layer = config.PARAMS_PER_LAYER
        self.total_params = self.params_per_layer * (self.n_layers + 1)
        logger.info(
            "QuantumInferenceEngine: %d qubits, %d features, %d re-upload layers, %d trainable params",
            self.n_qubits, self.n_features, self.n_layers, self.total_params,
        )

    def infer(
        self,
        features: List[float],
        model_type: str = "QSVC",
        trained_params: Optional[List[float]] = None,
    ) -> Dict[str, Any]:
        start_time = time.time()

        circuit = self._build_circuit(features, trained_params)
        simulated_latency = self._simulate_qpu_delay()

        shots = config.DEFAULT_SHOTS
        job = self.simulator.run(circuit, shots=shots)
        result = job.result()
        counts = result.get_counts()

        p_flow = self._extract_flow_probability(counts, shots)
        execution_time_ms = int((time.time() - start_time) * 1000)

        return {
            "pFlow": p_flow,
            "shotsUsed": shots,
            "depth": circuit.depth(),
            "qpuLatencyMs": simulated_latency + execution_time_ms,
        }

    # ── circuit construction ─────────────────────────────────────

    def _build_circuit(
        self,
        features: List[float],
        trained_params: Optional[List[float]] = None,
    ) -> QuantumCircuit:
        nq = self.n_qubits
        qc = QuantumCircuit(nq, nq)

        feats = self._pad_features(features)
        params = self._resolve_params(trained_params)

        for layer in range(self.n_layers + 1):
            self._encoding_layer(qc, feats)
            self._zz_interaction_layer(qc, feats)
            self._ring_entangling_layer(qc)
            offset = layer * self.params_per_layer
            self._variational_layer(qc, params[offset : offset + self.params_per_layer])

        qc.measure(range(nq), range(nq))
        return qc

    def _encoding_layer(self, qc: QuantumCircuit, feats: List[float]):
        """Angle encoding: Ry on features[0..3], Rz on features[4..7]."""
        for i in range(self.n_qubits):
            qc.ry((feats[i] + 1.0) * PI, i)
            qc.rz((feats[i + self.n_qubits] + 1.0) * PI, i)

    def _zz_interaction_layer(self, qc: QuantumCircuit, feats: List[float]):
        """ZZ feature-interaction terms for adjacent qubit pairs (ring)."""
        for i in range(self.n_qubits):
            j = (i + 1) % self.n_qubits
            angle = (feats[i] - feats[j]) ** 2 * PI
            qc.cx(i, j)
            qc.rz(angle, j)
            qc.cx(i, j)

    def _ring_entangling_layer(self, qc: QuantumCircuit):
        """Ring CNOT: 0→1→2→3→0."""
        for i in range(self.n_qubits):
            qc.cx(i, (i + 1) % self.n_qubits)

    def _variational_layer(self, qc: QuantumCircuit, layer_params: List[float]):
        """Ry(θ_i) Rz(φ_i) per qubit."""
        for i in range(self.n_qubits):
            qc.ry(layer_params[2 * i], i)
            qc.rz(layer_params[2 * i + 1], i)

    def _build_circuit_no_measure(
        self,
        features: List[float],
        trained_params: Optional[List[float]] = None,
    ) -> QuantumCircuit:
        """Build VQC circuit with measure_all (no classical register in body)."""
        nq = self.n_qubits
        qc = QuantumCircuit(nq)

        feats = self._pad_features(features)
        params = self._resolve_params(trained_params)

        for layer in range(self.n_layers + 1):
            self._encoding_layer(qc, feats)
            self._zz_interaction_layer(qc, feats)
            self._ring_entangling_layer(qc)
            offset = layer * self.params_per_layer
            self._variational_layer(qc, params[offset : offset + self.params_per_layer])

        qc.measure_all()
        return qc

    # ── helpers ───────────────────────────────────────────────────

    def _pad_features(self, features: List[float]) -> List[float]:
        """Ensure feature vector is exactly n_features long; pad or truncate."""
        f = list(features)
        if len(f) >= self.n_features:
            return f[: self.n_features]
        return f + [0.0] * (self.n_features - len(f))

    def _resolve_params(self, trained_params: Optional[List[float]]) -> List[float]:
        """Return trained params or generate deterministic defaults."""
        if trained_params is not None and len(trained_params) >= self.total_params:
            logger.info("Using TRAINED parameters (%d values)", len(trained_params))
            return list(trained_params[: self.total_params])

        if trained_params is not None and len(trained_params) > 0:
            logger.warning(
                "Received %d params, expected %d – padding with defaults",
                len(trained_params),
                self.total_params,
            )
            padded = list(trained_params)
            while len(padded) < self.total_params:
                idx = len(padded)
                padded.append(0.5 + 0.1 * (idx % 8))
            return padded[: self.total_params]

        logger.info("Using DEFAULT parameters (%d values)", self.total_params)
        return [0.5 + 0.2 * (i % 4) + 0.1 * (i // 4) for i in range(self.total_params)]

    def _extract_flow_probability(self, counts: Dict[str, int], shots: int) -> float:
        flow_count = sum(c for bs, c in counts.items() if bs[-1] == "1")
        p_flow = flow_count / shots
        if config.SIMULATE_QPU_NOISE:
            p_flow += random.uniform(-0.02, 0.02)
        return round(max(0.0, min(1.0, p_flow)), 4)

    def _simulate_qpu_delay(self) -> int:
        min_d, max_d = config.get_latency_range_seconds()
        delay = random.uniform(min_d, max_d)
        time.sleep(delay)
        return int(delay * 1000)

    # ── real IBM Quantum backend ────────────────────────────────

    def _resolve_backend(self, backend_name: str):
        """Resolve backend: try real hardware, fall back to FakeProvider noise model."""
        import os, certifi
        os.environ.setdefault("REQUESTS_CA_BUNDLE", certifi.where())
        os.environ.setdefault("SSL_CERT_FILE", certifi.where())

        FAKE_BACKENDS = {
            "fake_kyiv": "FakeKyiv",
            "fake_brisbane": "FakeBrisbane",
            "fake_kingston": "FakeKingston",
        }
        fake_cls_name = FAKE_BACKENDS.get(backend_name)
        if fake_cls_name:
            from qiskit_ibm_runtime import fake_provider
            cls = getattr(fake_provider, fake_cls_name)
            backend = cls()
            logger.info("Using fake backend: %s (%d qubits)", backend.name, backend.num_qubits)
            return backend, "fake"

        token = config.IBMQ_TOKEN
        if not token:
            raise RuntimeError("IBMQ_TOKEN not configured and no fake backend matched")

        from qiskit_ibm_runtime import QiskitRuntimeService
        svc_kwargs = {"channel": "ibm_quantum_platform", "token": token}
        if config.IBM_INSTANCE_CRN:
            svc_kwargs["instance"] = config.IBM_INSTANCE_CRN
        service = QiskitRuntimeService(**svc_kwargs)
        backend = service.backend(backend_name)
        logger.info("Using real backend: %s (%d qubits)", backend.name, backend.num_qubits)
        return backend, "real"

    def infer_real(
        self,
        features: List[float],
        trained_params: Optional[List[float]] = None,
        backend_name: str = "ibm_kyiv",
        shots: int = 1024,
        optimization_level: int = 2,
    ) -> Dict[str, Any]:
        """Run circuit on IBM Quantum backend (real or noise-model fake)."""
        from qiskit_ibm_runtime import SamplerV2

        start = time.time()

        backend, mode = self._resolve_backend(backend_name)
        logger.info("Backend resolved: %s (mode=%s, %d qubits)", backend.name, mode, backend.num_qubits)

        circuit = self._build_circuit_no_measure(features, trained_params)

        pm = generate_preset_pass_manager(
            optimization_level=optimization_level, backend=backend
        )
        isa_circuit = pm.run(circuit)

        sampler = SamplerV2(mode=backend)
        job = sampler.run([isa_circuit], shots=shots)
        result = job.result()
        pub_result = result[0]

        counts = pub_result.data.meas.get_counts()
        total_shots = sum(counts.values())

        flow_count = 0
        for bitstring, count in counts.items():
            if bitstring[-1] == "1":
                flow_count += count
        p_flow = round(max(0.0, min(1.0, flow_count / total_shots)), 4)

        elapsed_ms = int((time.time() - start) * 1000)
        logger.info(
            "Real QPU result: p_flow=%.4f, shots=%d, depth=%d, time=%dms, backend=%s",
            p_flow, total_shots, isa_circuit.depth(), elapsed_ms, backend_name,
        )

        return {
            "pFlow": p_flow,
            "shotsUsed": total_shots,
            "depth": isa_circuit.depth(),
            "qpuLatencyMs": elapsed_ms,
            "backendName": backend_name,
            "transpiledDepth": isa_circuit.depth(),
            "transpiledGateCount": isa_circuit.size(),
        }

    def infer_real_batch(
        self,
        samples: List[Dict[str, Any]],
        backend_name: str = "ibm_kyiv",
        shots: int = 1024,
        optimization_level: int = 2,
    ) -> List[Dict[str, Any]]:
        """Run a batch of circuits on IBM Quantum (real or noise-model fake)."""
        from qiskit_ibm_runtime import SamplerV2

        start = time.time()

        backend, mode = self._resolve_backend(backend_name)
        logger.info("IBM batch: %d circuits on %s (mode=%s)", len(samples), backend.name, mode)

        pm = generate_preset_pass_manager(
            optimization_level=optimization_level, backend=backend
        )

        isa_circuits = []
        for s in samples:
            feats = s["features"]
            params = s.get("trainedParams")
            circ = self._build_circuit_no_measure(feats, params)
            isa = pm.run(circ)
            isa_circuits.append(isa)

        sampler = SamplerV2(mode=backend)
        job = sampler.run(isa_circuits, shots=shots)
        logger.info("IBM job submitted: %s", job.job_id())
        result = job.result()

        results = []
        for i, pub_result in enumerate(result):
            counts = pub_result.data.meas.get_counts()
            total = sum(counts.values())
            flow_count = sum(c for bs, c in counts.items() if bs[-1] == "1")
            p_flow = round(max(0.0, min(1.0, flow_count / total)), 4)
            results.append({
                "pFlow": p_flow,
                "shotsUsed": total,
                "depth": isa_circuits[i].depth(),
                "qpuLatencyMs": 0,
                "backendName": backend_name,
            })

        elapsed_ms = int((time.time() - start) * 1000)
        logger.info("IBM batch complete: %d results in %dms", len(results), elapsed_ms)
        for r in results:
            r["qpuLatencyMs"] = elapsed_ms // max(len(results), 1)

        return results

    def health_check(self) -> Dict[str, Any]:
        try:
            qc = QuantumCircuit(1, 1)
            qc.h(0)
            qc.measure(0, 0)
            self.simulator.run(qc, shots=10).result()
            return {
                "available": True,
                "simulator": "qasm_simulator",
                "qubits": self.n_qubits,
                "totalParams": self.total_params,
                "reuploadLayers": self.n_layers,
            }
        except Exception as e:
            logger.error(f"Health check failed: {e}")
            return {"available": False, "error": str(e)}


qml_engine = QuantumInferenceEngine()
