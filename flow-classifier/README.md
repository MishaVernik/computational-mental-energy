# Flow Classifier Service

Classical ML service for flow state detection from EEG features.

## Usage

```bash
pip install -r requirements.txt
uvicorn app.main:app --host 0.0.0.0 --port 8002
```

## Endpoints

- `POST /classify` – Input: `{ "features": [22 floats], "action_type": "coding" }`. Output: `{ "flow_probability": 0.7, "flow_label": true }`
- `GET /health` – Health check

## Bootstrap

Until a model is trained, the service uses a heuristic: `flow = (alpha_AF7 + alpha_AF8) / (theta_AF7 + theta_AF8) >= 1`.

## Training (offline)

Run `python train.py` to fetch labeled data from CmeSim.Api and train a Random Forest. Save model to `model.pkl` for loading at startup.
