import numpy as np
import os
import shap
import matplotlib.pyplot as plt
from datetime import datetime
from modules.preprocessing import data_export, resource_inspector


def create(config, X, shap_values, y=None):
    """
    Genera un scatterplot SHAP y exporta datos para Unity.
    Maneja:
        - Regresión
        - Clasificación binaria (clase positiva o negativa)
        - Clasificación multiclase
    """
    x_feat = config["x_feature"]
    z_feat = config.get("z_feature")
    colormap_name = config.get("colormap", "cmap_red_blue")
    mpl_cmap = resource_inspector.resolve_colormap(colormap_name)

    task_type = config.get("task_type", "regression")
    target_name = config.get("target")
    max_pts = config.get("max_points", -1)
    z_thresh = config.get("z_score_threshold", 3.0)

    try:
        # --- FIDELITY FIX 1: Z-Score Clipping (Matching Unity's PointFilter) ---
        num_samples = len(X)

        # Calculate mean and std for clipping features (X and Z)
        # Note: We follow Unity's logic of clipping input features, not SHAP directly
        cols_to_clip = [x_feat]
        if z_feat and z_feat != "auto":
            cols_to_clip.append(z_feat)

        mask = np.ones(num_samples, dtype=bool)

        for col in cols_to_clip:
            if col not in X.columns: continue
            col_data = X[col].values
            mean = np.mean(col_data)
            std = np.std(col_data)
            if std > 1e-6:
                z_scores = np.abs((col_data - mean) / std)
                mask &= (z_scores <= z_thresh)

        # Apply Clipping
        if not np.all(mask):
            pre_clip_count = len(X)
            X = X[mask].copy()
            shap_values = shap_values[mask]
            if y is not None:
                y = y.iloc[mask] if hasattr(y, "iloc") else y[mask]

            clipped_count = pre_clip_count - len(X)
            print(f"ImmersiveSHAP, PYTHON, [Scaler] Z-Score Clipping ({z_thresh}): Removed {clipped_count} outliers.")

        # --- FIDELITY FIX 2: Downsampling (Visual Parity) ---
        num_samples = len(X)
        final_points_count = num_samples

        if 0 < max_pts < num_samples:
            print(f"ImmersiveSHAP, PYTHON, [Scaler] Request: MaxPoints={max_pts} (Original={num_samples})")
            indices = np.random.choice(num_samples, max_pts, replace=False)
            X = X.iloc[indices].copy()
            shap_values = shap_values[indices]
            if y is not None:
                # Handle pandas series or numpy arrays
                y = y.iloc[indices] if hasattr(y, "iloc") else y[indices]
            final_points_count = max_pts

        print(f"ImmersiveSHAP, PYTHON, [Scaler] Final 2D Plot Count: {final_points_count} points.")

        # --- AUTO-Z INTERACTION LOGIC (NEW) ---
        if not z_feat or z_feat.lower() == "auto":
            print(f"ImmersiveSHAP, PYTHON, [Auto-Z] Detecting best interaction for {x_feat}...")
            try:
                # We need the list of feature indices sorted by interaction strength
                # shap.utils.approximate_interactions returns indices
                inds = shap.utils.approximate_interactions(x_feat, shap_values)

                # We pick the top interaction.
                # Note: inds includes the feature itself (often strong interaction).
                # We take the first one (strongest).
                best_idx = inds[0]
                z_feat = shap_values.feature_names[best_idx]

                print(f"ImmersiveSHAP, PYTHON, [Auto-Z] Selected: {z_feat}")
                config["z_feature"] = z_feat

            except Exception as e:
                print(f"ImmersiveSHAP, PYTHON, [Auto-Z] Failed to detect interaction: {e}. Defaulting to X feature.")
                z_feat = x_feat
                config["z_feature"] = z_feat

        # Extract values
        x_vals = X[x_feat].values.tolist()
        z_vals = X[z_feat].values.tolist()
        x_idx = X.columns.get_loc(x_feat)
        z_idx = X.columns.get_loc(z_feat)

        # FIX 1: Paridad Visual Absoluta con Unity
        # El color en Python debe representar el VALOR REAL de la variable Z (eje Z en Unity)
        color_expl = X[z_feat].values

        class_label = None

        # --- Multiclase ---
        if shap_values.values.ndim == 3:
            if y is None:
                raise ValueError("Para multiclase, se debe pasar 'y' al create().")

            target_class = config.get("target")
            if target_class is None:
                raise ValueError("Falta 'target' en config para clasificación multiclase.")

            class_names = sorted(set(y))
            if target_class not in class_names:
                raise ValueError(f"Clase '{target_class}' no encontrada. Clases disponibles: {class_names}")

            class_idx = class_names.index(target_class)
            class_label = str(target_class)

            shap_class_array = shap_values.values[:, :, class_idx]

            shap_exp = shap.Explanation(
                values=shap_class_array,
                base_values=(shap_values.base_values[:, class_idx]
                             if shap_values.base_values.ndim > 1
                             else shap_values.base_values[class_idx]),
                data=shap_values.data,
                feature_names=shap_values.feature_names
            )

            y_vals = shap_class_array[:, x_idx].tolist()

            plt.figure(figsize=(7.12, 5))
            shap.plots.scatter(
                shap_exp[:, x_idx],
                color=color_expl,  # Usamos el valor real de Z
                cmap=plt.get_cmap(mpl_cmap),
                show=False
            )

        # --- Binaria o regresión ---
        else:
            shap_array = shap_values.values.copy()
            base_values = shap_values.base_values.copy()

            target = config.get("target", "positive")

            # FIX 2: Corrección de class_label para Clasificación Binaria
            if task_type == "classification":
                if y is not None:
                    # Obtenemos las clases en el mismo orden que el request_manager
                    try:
                        class_names = sorted(y.unique())
                    except:
                        # Fallback for numpy arrays or lists
                        class_names = sorted(list(set(y.values) if hasattr(y, 'values') else y))

                    if target in class_names:
                        target_index = class_names.index(target)
                        class_label = str(target)
                        if target_index == 0:
                            shap_array = -shap_array
                            base_values = -base_values
                    elif str(target).lower() == "negative":
                        class_label = str(class_names[0])
                        shap_array = -shap_array
                        base_values = -base_values
                    else:
                        class_label = str(class_names[1])
                else:
                    class_label = target
                    if str(target).lower() == "negative":
                        shap_array = -shap_array
                        base_values = -base_values
            else:
                class_label = None

            shap_exp = shap.Explanation(
                values=shap_array,
                base_values=base_values,
                data=shap_values.data,
                feature_names=shap_values.feature_names
            )

            y_vals = shap_array[:, x_idx].tolist()

            plt.figure(figsize=(7.12, 5))
            shap.plots.scatter(
                shap_exp[:, x_idx],
                color=color_expl,  # Usamos el valor real de Z
                cmap=plt.get_cmap(mpl_cmap),
                show=False
            )

        # --- Gestión de Títulos y Guardado ---
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

        if task_type == "classification":
            display_label = class_label if class_label else "Undefined"
            filename = f"scatter_{x_feat}_z_{z_feat}_class_{display_label}_{timestamp}.svg"
            title = f"SHAP Scatter (Class: {display_label})\n{x_feat} vs SHAP({x_feat}), colored by {z_feat}"
        else:
            display_target = target_name if target_name else "Target"
            filename = f"scatter_{x_feat}_z_{z_feat}_target_{display_target}_{timestamp}.svg"
            title = f"SHAP Scatter (Target: {display_target})\n{x_feat} vs SHAP({x_feat}), colored by {z_feat}"

        output_path = os.path.join("outputs/plots", filename)
        os.makedirs("outputs/plots", exist_ok=True)

        plt.title(title, fontsize=12)
        plt.savefig(output_path, bbox_inches='tight', format='svg')
        plt.close()

        # --- NEW: Interpretability Calculations ---
        # 1. Base Values (Expected Value)
        base_val = float(shap_exp.base_values[0]) if hasattr(shap_exp.base_values, "__len__") else float(
            shap_exp.base_values)

        # 2. Final Predictions
        final_preds = (shap_exp.base_values + shap_exp.values.sum(axis=1)).tolist()

        # 3. Impact Share (%)
        total_abs_impact = np.abs(shap_exp.values).sum(axis=1)
        impact_shares = (np.abs(shap_exp.values[:, x_idx]) / np.where(total_abs_impact == 0, 1e-6,
                                                                      total_abs_impact) * 100).tolist()

        # 4. Quantiles
        from scipy import stats
        x_array = np.array(x_vals)
        x_quantiles = [float(stats.percentileofscore(x_array, v) / 100.0) for v in x_vals]

        # --- Export to Unity ---
        data_dict = data_export.export(
            x_vals,
            y_vals,
            z_vals,
            x_feat,
            z_feat,
            f"SHAP({x_feat})",
            colormap=colormap_name,
            # Pass new metadata
            base_value=base_val,
            final_predictions=final_preds,
            impact_shares=impact_shares,
            x_quantiles=x_quantiles
        )
        data_dict["plot_path"] = output_path
        data_dict["class_label"] = class_label

        return data_dict

    except Exception as e:
        return {"action": "error", "error": f"Error generando scatterplot: {str(e)}"}
