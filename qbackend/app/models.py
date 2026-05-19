"""Pydantic models for request/response."""
from pydantic import BaseModel, Field
from typing import List, Optional


class InferRequest(BaseModel):
    """Request for quantum inference."""
    features: List[float] = Field(..., description="EEG-derived features (8 values: 4 band-power averages + 4 secondary)", min_length=1)
    modelType: str = Field(default="QSVC", description="Model type: QSVC or VQC")
    trainedParams: Optional[List[float]] = Field(
        default=None,
        description="Trained variational parameters (24 floats for 3 re-upload layers × 8 params/layer; backward-compatible with 8-param legacy models)",
    )

    class Config:
        json_schema_extra = {
            "example": {
                "features": [0.5, -0.3, 0.8, 0.1, 0.4, -0.2, 0.6, 0.05],
                "modelType": "QSVC",
                "trainedParams": None,
            }
        }


class InferResponse(BaseModel):
    """Response from quantum inference."""
    pFlow: float = Field(..., description="Probability of flow state (0-1)")
    shotsUsed: int = Field(..., description="Number of quantum circuit shots")
    depth: int = Field(..., description="Circuit depth")
    qpuLatencyMs: int = Field(..., description="QPU execution time in milliseconds")

    class Config:
        json_schema_extra = {
            "example": {
                "pFlow": 0.623,
                "shotsUsed": 1024,
                "depth": 18,
                "qpuLatencyMs": 1456,
            }
        }


class BatchInferRequest(BaseModel):
    """Batch inference for training – no simulated latency."""
    samples: List[InferRequest] = Field(..., description="List of inference requests", min_length=1)


class BatchInferResponse(BaseModel):
    """Batch inference results."""
    results: List[InferResponse]
    totalMs: int


class RealInferRequest(BaseModel):
    """Request for inference on real IBM Quantum hardware."""
    features: List[float] = Field(..., min_length=1)
    modelType: str = Field(default="QSVC")
    trainedParams: Optional[List[float]] = None
    backendName: str = Field(default="ibm_kingston")
    shots: int = Field(default=1024)


class RealInferResponse(BaseModel):
    """Response from real IBM Quantum hardware."""
    pFlow: float
    shotsUsed: int
    depth: int
    qpuLatencyMs: int
    backendName: str
    transpiledDepth: int = 0
    transpiledGateCount: int = 0


class RealBatchRequest(BaseModel):
    """Batch request for real IBM Quantum hardware."""
    samples: List[InferRequest] = Field(..., min_length=1)
    backendName: str = Field(default="ibm_kingston")
    shots: int = Field(default=1024)


class RealBatchResponse(BaseModel):
    """Batch response from real IBM Quantum hardware."""
    results: List[RealInferResponse]
    totalMs: int
    jobId: str = ""
    backendName: str = ""


class HealthResponse(BaseModel):
    """Health check response."""
    status: str
    qpuAvailable: bool
    simulatorType: str


