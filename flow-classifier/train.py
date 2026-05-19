"""
Train classical flow classifier on labeled data from CmeSim API.
Fetches EegWindowFeatures, trains Random Forest, saves model.pkl.
"""
import os
import pickle
import sys

import numpy as np
import httpx
from sklearn.ensemble import RandomForestClassifier

API_BASE = os.environ.get("API_BASE_URL", "http://localhost:5000")
MODEL_PATH = os.path.join(os.path.dirname(__file__), "model.pkl")


def main():
    print(f"Fetching labeled windows from {API_BASE}/api/dataset/windows?labeled=true&limit=1000")
    with httpx.Client(timeout=60) as client:
        r = client.get(f"{API_BASE}/api/dataset/windows", params={"labeled": True, "limit": 1000})
        r.raise_for_status()
        windows = r.json()

    if not windows:
        print("No labeled windows found. Run data collection and heuristic bootstrap first.")
        sys.exit(1)

    X = []
    y = []
    for w in windows:
        features = w.get("features")
        label = w.get("flowLabel")
        if features is None or label is None:
            continue
        if len(features) < 22:
            features = features + [0.0] * (22 - len(features))
        X.append(features[:22])
        y.append(bool(label))

    X_arr = np.array(X)
    y_arr = np.array(y)

    print(f"Training on {len(X)} samples...")
    model = RandomForestClassifier(n_estimators=100, random_state=42)
    model.fit(X_arr, y_arr)

    with open(MODEL_PATH, "wb") as f:
        pickle.dump(model, f)

    print(f"Model saved to {MODEL_PATH}")


if __name__ == "__main__":
    main()
