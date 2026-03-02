# modules/preprocessing/train_model.py

from xgboost import XGBRegressor, XGBClassifier
import os
#import time


def train(X, y, task_type="regression"):
    """
    Entrena un modelo XGBoost y muestra un reporte estructurado en consola.
    """

    params = {
        "n_estimators": 100,
        "use_label_encoder": False,
        "eval_metric": "rmse" if task_type == "regression" else "mlogloss"
    }

    backend = "cpu"
    if "CUDA_VISIBLE_DEVICES" in os.environ:
        params["tree_method"] = "gpu_hist"
        backend = "gpu_hist"

    # Selección del modelo
    if task_type == "regression":
        model = XGBRegressor(**params)
        model_name = "XGBRegressor"
        n_classes = None
    elif task_type == "classification":
        model = XGBClassifier(**params)
        model_name = "XGBClassifier"
        n_classes = len(set(y))
    else:
        raise ValueError(f"task_type desconocido: {task_type}")

    # ⏱ Medir tiempo
    #start = time.time()
    model.fit(X, y)
    #end = time.time()

    #training_time = end - start

    # 📊 Reporte legible
    print("\n================= TRAINING REPORT =================")
    print(f"Model:           {model_name}")
    print(f"Task type:       {task_type}")
    print(f"Backend:         {backend}")
    print(f"Samples:         {X.shape[0]}")
    print(f"Features:        {X.shape[1]}")
    if n_classes is not None:
        print(f"Classes:         {n_classes}")
    print(f"Estimators:      {params['n_estimators']}")

    #print(f"Training time:   {training_time:.4f} seconds")
    print("===================================================\n")

    return model
