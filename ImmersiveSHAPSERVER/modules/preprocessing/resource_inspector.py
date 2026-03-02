import numpy as np
from sklearn.datasets import (
    fetch_california_housing,
    load_iris,
    load_breast_cancer)
import shap
import matplotlib.pyplot as plt


# 🔹 Centralized Metadata Dictionary
METADATA = {
    "californiahousing": {
        "loader": fetch_california_housing,
        "type": "regression",
        "target_name": "MedHouseValue"
    },
    "iris": {
        "loader": load_iris,
        "type": "classification",
        "target_name": "species"
    },
    "breast_cancer": {
        "loader": load_breast_cancer,
        "type": "classification",
        "target_name": "diagnosis"
    }
}

AVAILABLE_MODELS = {
    "xgboostRegressor": {"needs_scaling": False, "task_type": "regression"},
    "xgboostClassifier": {"needs_scaling": False, "task_type": "classification"}
}


def get_dataset_metadata(dataset_name):
    if dataset_name not in METADATA:
        return {"error": f"Dataset '{dataset_name}' not supported."}

    info = METADATA[dataset_name]
    loader = info["loader"]

    # 🔸 Initialize basic metadata without loading full data
    feature_names = []
    class_names = []
    n_classes = None

    if info["type"] == "regression":
        # Data loading not required for metadata
        class_names = [info["target_name"]]
    elif info["type"] == "classification":
        # Load metadata object only
        dataset = loader(return_X_y=False)
        if hasattr(dataset, "target_names") and dataset.target_names is not None:
            class_names = list(dataset.target_names)
            n_classes = len(class_names)
        elif hasattr(dataset, "target"):
            # Fallback: calculate unique class names
            target = dataset.target
            class_names = [str(c) for c in np.unique(target)]
            n_classes = len(class_names)

    # 🔸 Load feature names if available
    dataset = loader(return_X_y=False)
    if hasattr(dataset, "feature_names"):
        feature_names = list(dataset.feature_names)

    return {
        "dataset_name": dataset_name,
        "feature_names": feature_names,
        "target_name": info["target_name"],
        "type": info["type"],
        "n_features": len(feature_names),
        "n_classes": n_classes,
        "class_names": class_names
    }


def get_features_for_dataset(dataset_name):
    metadata = get_dataset_metadata(dataset_name)
    return metadata.get("feature_names", [])


def get_class_names(dataset_name):
    metadata = get_dataset_metadata(dataset_name)
    if metadata.get("type") == "classification":
        return metadata.get("class_names", [])
    return [metadata.get("target_name", "target")]


def get_target_classes(dataset_name):
    metadata = get_dataset_metadata(dataset_name)
    return metadata.get("n_classes") if metadata.get("type") == "classification" else None


def get_supported_models():
    return list(AVAILABLE_MODELS.keys())


def get_supported_plot_types():
    return ["scatter"]


# 🔹 Unified map of symbolic colormaps → matplotlib
COLORMAP_MAP = {
    "cmap_red_blue": "shap_red_blue",      # SHAP uses its default
    "cmap_viridis": "viridis",
    "cmap_plasma": "plasma",
    "cmap_cool": "cool"
}

def get_supported_colormaps():
    # Exposed to Unity
    return list(COLORMAP_MAP.keys())

def resolve_colormap(name):
    if name == "cmap_red_blue":
        return shap.plots.colors.red_blue
    return plt.get_cmap(COLORMAP_MAP[name])

def get_resources():
    datasets = list(METADATA.keys())
    models = get_supported_models()
    plot_types = get_supported_plot_types()
    colormaps = get_supported_colormaps()

    dataset_features = {}
    dataset_targets = {}

    for dataset_key, meta in METADATA.items():
        loader = meta["loader"]
        data = loader()

        raw_features = data.feature_names if hasattr(data, "feature_names") else [f"feature_{i}" for i in range(data.data.shape[1])]
        feature_names = list(raw_features) if isinstance(raw_features, (np.ndarray, list, tuple)) else [str(raw_features)]

        dataset_features[dataset_key] = feature_names
        dataset_targets[dataset_key] = get_class_names(dataset_key)

    dataset_types = {name: meta["type"] for name, meta in METADATA.items()}
    model_tasks = {name: meta["task_type"] for name, meta in AVAILABLE_MODELS.items()}

    return {
        "action": "init_response",
        "datasets": datasets,
        "models": models,
        "plot_types": plot_types,
        "colormaps": colormaps,
        "features": dataset_features,
        "targets": dataset_targets,
        "dataset_types": dataset_types,
        "model_tasks": model_tasks
    }
