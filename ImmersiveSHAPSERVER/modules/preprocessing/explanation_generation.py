# modules/preprocessing/explanation_generation.py

import shap

def explain(model, X):
    """
    Crea un explainer y calcula los valores SHAP.
    """
    explainer = shap.Explainer(model, X)
    shap_values = explainer(X)

    return explainer, shap_values
