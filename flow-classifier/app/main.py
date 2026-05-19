"""
Flow state classifier service.
Uses classical ML (Random Forest / MLP) to predict flow from EEG features.
Training: run train.py to fetch from API, train, save model.pkl.
Inference: POST /classify returns FlowProbability, FlowLabel.
"""
import logging
import os
import pickle
from pathlib import Path
from typing import Optional

import numpy as np
from fastapi import FastAPI
from pydantic import BaseModel

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="Flow Classifier", version="0.1.0")

# Model loaded at startup (or None if not trained yet)
_model: Optional[object] = None
MODEL_PATH = Path(__file__).resolve().parent.parent / "model.pkl"


def _load_model():
    global _model
    if MODEL_PATH.exists():
        try:
            with open(MODEL_PATH, "rb") as f:
                _model = pickle.load(f)
            logger.info("Loaded trained model from %s", MODEL_PATH)
        except Exception as e:
            logger.warning("Failed to load model: %s", e)
            _model = None
    else:
        _model = None


class ClassifyRequest(BaseModel):
    """EEG features: 20 band powers (5 bands x 4 channels) + TaskDifficulty + Quality."""
    features: list[float]  # 22 values
    action_type: Optional[str] = None


class ClassifyResponse(BaseModel):
    flow_probability: float
    flow_label: bool


def _heuristic_classify(features: list[float]) -> tuple[float, bool]:
    """Bootstrap: alpha/theta ratio for frontal channels (AF7, AF8 = indices 5-14)."""
    if len(features) < 20:
        return 0.5, False
    # Alpha: indices 2,7 (TP9, AF7), 12,17 (AF8, TP10) - simplified: use 5-9 for AF7, 10-14 for AF8
    alpha_af7 = features[7] if len(features) > 7 else 0
    alpha_af8 = features[12] if len(features) > 12 else 0
    theta_af7 = features[6] if len(features) > 6 else 0.1
    theta_af8 = features[11] if len(features) > 11 else 0.1
    theta_sum = theta_af7 + theta_af8
    if theta_sum == 0:
        return 0.5, False
    ratio = (alpha_af7 + alpha_af8) / theta_sum
    prob = min(ratio / 2.0, 1.0)
    return prob, ratio >= 1.0


@app.post("/classify", response_model=ClassifyResponse)
def classify(req: ClassifyRequest) -> ClassifyResponse:
    """Classify flow state from EEG features."""
    if _model is not None:
        X = np.array([req.features[:22]]).reshape(1, -1)
        prob = float(_model.predict_proba(X)[0, 1]) if hasattr(_model, "predict_proba") else float(_model.predict(X)[0])
        return ClassifyResponse(flow_probability=prob, flow_label=prob >= 0.5)
    prob, label = _heuristic_classify(req.features)
    return ClassifyResponse(flow_probability=prob, flow_label=label)


@app.get("/health")
def health():
    return {"status": "ok", "model_loaded": _model is not None}


@app.on_event("startup")
def startup():
    _load_model()
