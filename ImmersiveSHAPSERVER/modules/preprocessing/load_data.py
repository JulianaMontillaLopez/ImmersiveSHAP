# modules/preprocessing/load_data.py

from sklearn.datasets import fetch_california_housing, load_iris, load_breast_cancer
import pandas as pd

def load(dataset_name):
    """
    Carga un dataset por nombre.
    Actualmente soportado: californiahousing, iris, breast_cancer
    """
    if dataset_name == "californiahousing":
        raw = fetch_california_housing(as_frame=True)
        X = raw.data
        y = raw.target
        metadata = {
            "feature_names": list(X.columns),
            "target_name": "MedHouseValue"
        }

    elif dataset_name == "iris":
        raw = load_iris(as_frame=True)
        X = raw.data
        # Mapear números 0,1,2 a nombres reales
        target_names = raw.target_names  # ['setosa', 'versicolor', 'virginica']
        y = pd.Series([target_names[i] for i in raw.target], name="species")
        metadata = {
            "feature_names": list(X.columns),
            "target_name": "species"
        }

    elif dataset_name == "breast_cancer":
        raw = load_breast_cancer(as_frame=True)
        X = raw.data
        # Mapear 0/1 a nombres 'malignant'/'benign' para mayor claridad
        target_names = raw.target_names  # ['malignant', 'benign']
        y = pd.Series([target_names[i] for i in raw.target], name="diagnosis")
        metadata = {
            "feature_names": list(X.columns),
            "target_name": "diagnosis"
        }

    else:
        raise ValueError(f"Dataset no soportado: {dataset_name}")

    return X, y, metadata
