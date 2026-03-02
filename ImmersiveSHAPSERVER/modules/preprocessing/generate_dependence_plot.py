# modules/preprocessing/generate_dependence_plot.py

import os
import shap
import matplotlib.pyplot as plt
from modules.preprocessing import data_export

def create(config, X, shap_values, explainer=None):
    x_feat = config["x_feature"]
    z_feat = config["z_feature"]
    target_class = config.get("target")

    # Resolver SHAP multiclase
    if isinstance(shap_values, list):
        if explainer is None:
            raise ValueError("Explainer requerido para multiclase")

        class_names = list(explainer.model.classes_)
        class_idx = class_names.index(target_class)
        shap_plot_vals = shap_values[class_idx]
    else:
        shap_plot_vals = shap_values

    # Datos para Unity
    x_vals = X[x_feat].values.tolist()
    z_vals = X[z_feat].values.tolist()
    y_vals = shap_plot_vals[:, x_feat].values.tolist()

    os.makedirs("outputs/plots", exist_ok=True)
    output_path = f"outputs/plots/dependence_{x_feat}_z_{z_feat}.png"

    plt.figure(figsize=(6, 5))

    shap.dependence_plot(
        shap_plot_vals[:, x_feat],
        interaction_index=z_feat,
        show=False,
        cmap=shap.plots.colors.red_blue
    )

    plt.title(
        f"SHAP Dependence: {x_feat}\ncolored by {z_feat} ({target_class})"
    )

    plt.savefig(output_path, dpi=300, bbox_inches="tight")
    plt.close()

    data_dict = data_export.export(
        x_vals,
        y_vals,
        z_vals,
        x_feat,
        z_feat,
        f"SHAP({x_feat})",
        colormap="cmap_red_blue"
    )

    data_dict["plot_path"] = output_path
    return data_dict

