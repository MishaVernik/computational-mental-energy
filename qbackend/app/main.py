"""FastAPI application for quantum backend service."""
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from .models import (
    InferRequest, InferResponse, HealthResponse,
    BatchInferRequest, BatchInferResponse,
    RealInferRequest, RealInferResponse,
    RealBatchRequest, RealBatchResponse,
)
from .qml import qml_engine
from .config import config

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Lifespan context manager for startup/shutdown."""
    logger.info("Starting Quantum Backend Service")
    logger.info(f"QPU Latency Range: {config.QPU_LATENCY_MIN_MS}-{config.QPU_LATENCY_MAX_MS} ms")
    logger.info(f"Default Shots: {config.DEFAULT_SHOTS}")
    logger.info(f"Qubits: {config.NUM_QUBITS}")
    yield
    logger.info("Shutting down Quantum Backend Service")


app = FastAPI(
    title="CME Quantum Backend",
    description="Quantum inference service for EEG-based flow state detection (Imitation Model)",
    version="1.0.0",
    lifespan=lifespan
)

# CORS middleware (allow Web API to call this service)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, restrict this
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/", tags=["Root"])
async def root():
    """Root endpoint."""
    return {
        "service": "CME Quantum Backend",
        "version": "1.0.0",
        "status": "running"
    }


@app.get("/health", response_model=HealthResponse, tags=["Health"])
async def health_check():
    """
    Health check endpoint.
    
    Returns quantum backend availability status.
    """
    health = qml_engine.health_check()
    
    return HealthResponse(
        status="ok" if health["available"] else "degraded",
        qpuAvailable=health["available"],
        simulatorType=health.get("simulator", "unknown")
    )


@app.post("/qpu/infer", response_model=InferResponse, tags=["Inference"])
async def qpu_infer(request: InferRequest):
    """
    Quantum inference endpoint.
    
    Executes a quantum circuit with angle-encoded EEG features
    and returns the probability of "flow" mental state.
    
    **Flow:**
    1. Build parametric quantum circuit with feature encoding
    2. Simulate QPU queue + execution delay
    3. Run circuit on Qiskit Aer simulator (or real backend if configured)
    4. Extract p_flow from measurement statistics
    5. Return result with metadata
    
    **Imitation Model Notes:**
    - This simulates a trained QSVC/VQC model without actual training
    - Circuit parameters are fixed (would be optimized in real system)
    - Latency is artificially injected to imitate real QPU behavior
    """
    try:
        logger.info(f"Inference request: {len(request.features)} features, model={request.modelType}, trained_params={'Yes' if request.trainedParams else 'No'}")
        
        # Validate features
        if len(request.features) == 0:
            raise HTTPException(status_code=400, detail="Features array cannot be empty")
        
        if len(request.features) > 100:  # Sanity check
            raise HTTPException(status_code=400, detail="Too many features (max 100)")
        
        # Validate trained parameters if provided (accept 8 legacy or 24 v2 params; engine pads if needed)
        if request.trainedParams is not None:
            if len(request.trainedParams) < 1:
                raise HTTPException(status_code=400, detail="Trained parameters array cannot be empty")
        
        # Execute quantum inference WITH trained parameters (if provided)
        result = qml_engine.infer(request.features, request.modelType, request.trainedParams)
        
        logger.info(f"Inference complete: p_flow={result['pFlow']:.3f}, latency={result['qpuLatencyMs']}ms")
        
        return InferResponse(**result)
    
    except Exception as e:
        logger.error(f"Inference failed: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Quantum inference failed: {str(e)}")


@app.post("/qpu/infer-batch", response_model=BatchInferResponse, tags=["Training"])
async def qpu_infer_batch(request: BatchInferRequest):
    """Batch inference for training – skips simulated QPU delay."""
    import time as _time
    start = _time.time()
    results = []
    for sample in request.samples:
        circuit = qml_engine._build_circuit(sample.features, sample.trainedParams)
        shots = config.DEFAULT_SHOTS
        job = qml_engine.simulator.run(circuit, shots=shots)
        counts = job.result().get_counts()
        p_flow = qml_engine._extract_flow_probability(counts, shots)
        results.append(InferResponse(
            pFlow=p_flow, shotsUsed=shots,
            depth=circuit.depth(), qpuLatencyMs=0,
        ))
    total_ms = int((_time.time() - start) * 1000)
    logger.info(f"Batch inference: {len(results)} samples in {total_ms}ms")
    return BatchInferResponse(results=results, totalMs=total_ms)


@app.post("/qpu/infer-real", response_model=RealInferResponse, tags=["Real Hardware"])
async def qpu_infer_real(request: RealInferRequest):
    """Run inference on real IBM Quantum hardware (ibm_kingston, ibm_kyiv, etc.)."""
    try:
        logger.info(f"Real QPU request: backend={request.backendName}, shots={request.shots}")
        result = qml_engine.infer_real(
            request.features,
            trained_params=request.trainedParams,
            backend_name=request.backendName,
            shots=request.shots,
        )
        return RealInferResponse(**result)
    except Exception as e:
        logger.error(f"Real QPU inference failed: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Real QPU inference failed: {str(e)}")


@app.post("/qpu/infer-real-batch", response_model=RealBatchResponse, tags=["Real Hardware"])
async def qpu_infer_real_batch(request: RealBatchRequest):
    """Run batch inference on real IBM Quantum hardware (single job submission)."""
    import time as _time
    try:
        logger.info(f"Real QPU batch: {len(request.samples)} circuits on {request.backendName}")
        start = _time.time()
        samples_dicts = [
            {"features": s.features, "trainedParams": s.trainedParams}
            for s in request.samples
        ]
        results = qml_engine.infer_real_batch(
            samples_dicts,
            backend_name=request.backendName,
            shots=request.shots,
        )
        total_ms = int((_time.time() - start) * 1000)
        return RealBatchResponse(
            results=[RealInferResponse(**r) for r in results],
            totalMs=total_ms,
            backendName=request.backendName,
        )
    except Exception as e:
        logger.error(f"Real QPU batch failed: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Real QPU batch failed: {str(e)}")


@app.get("/stats", tags=["Monitoring"])
async def get_stats():
    """
    Get service statistics (placeholder for monitoring).
    
    In a real system, this would return:
    - Total requests processed
    - Average latency
    - Queue length
    - Error rate
    """
    return {
        "message": "Statistics endpoint (not implemented in imitation model)",
        "note": "In production, would return request counts, latency stats, etc."
    }


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8001)


