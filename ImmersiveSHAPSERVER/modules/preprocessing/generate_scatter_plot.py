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

        # Extract values
        x_vals = X[x_feat].values.tolist()
        x_idx = X.columns.get_loc(x_feat)

        print(f"ImmersiveSHAP, PYTHON, [Scatter] Generating plot for {x_feat} (index {x_idx})")
        print(
            f"ImmersiveSHAP, PYTHON, [Scatter] SHAP Object Type: {type(shap_values)}, Shape: {getattr(shap_values, 'shape', 'No Shape')}")

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
            # --- Multiclase (Native Explanation Slicing) ---
            print(f"ImmersiveSHAP, PYTHON, [Scatter] Slicing multiclass class {class_idx}...")
            shap_exp = shap_values[:, :, class_idx]
            print(f"ImmersiveSHAP, PYTHON, [Scatter] Slice Shape: {shap_exp.shape}")

            # --- AUTO-Z (Specific to this class slice) ---
            if not z_feat or z_feat.lower() == "auto":
                try:
                    # Usamos .values para evitar errores de metadatos en Explanation
                    # Firma: approximate_interactions(index, shap_values, X)
                    inds = shap.utils.approximate_interactions(x_idx, shap_exp.values, X.values)
                    best_idx = inds[1] if (len(inds) > 1 and inds[0] == x_idx) else inds[0]
                    z_feat = shap_exp.feature_names[best_idx]
                    print(f"ImmersiveSHAP, PYTHON, [Auto-Z] Multiclass Partner: {z_feat}")
                    config["z_feature"] = z_feat
                except Exception as e:
                    print(f"ImmersiveSHAP, PYTHON, [Auto-Z] Failed in multiclass slice: {e}")
                    z_feat = x_feat

            # Final data extraction
            z_vals = X[z_feat].values.tolist()
            color_expl = X[z_feat].values
            y_vals = shap_exp.values[:, x_idx].tolist()

            plt.figure(figsize=(7.12, 5))
            shap.plots.scatter(
                shap_exp[:, x_idx],
                color=color_expl,
                cmap=plt.get_cmap(mpl_cmap),
                show=False
            )

        # --- Binaria o regresión ---
        # --- Binaria o Regresión (Native Explanation API) ---
        else:
            # SHAP 0.48+ handles Explanation objects natively, we slice to create a copy
            shap_exp = shap_values[:]
            target = config.get("target", "positive")
            class_label = None

            if task_type == "classification":
                # Determinamos el orden de las clases para saber si hay que invertir signos
                if y is not None:
                    try:
                        classes = sorted(y.unique())
                    except:
                        classes = sorted(list(set(y)))

                    if target in classes:
                        target_index = classes.index(target)
                        class_label = str(target)
                        print(f"ImmersiveSHAP, PYTHON, [Scatter] Binary target {target} mapped to index {target_index}")

                        # Si es la clase 0, invertimos para que el impacto positivo sea hacia "arriba"
                        if target_index == 0:
                            shap_exp.values = -shap_exp.values
                            shap_exp.base_values = -shap_exp.base_values
                    else:
                        # Fallback si no se encuentra el target
                        class_label = str(classes[1]) if len(classes) > 1 else str(classes[0])
                else:
                    class_label = target
                    if str(target).lower() == "negative":
                        shap_exp.values = -shap_exp.values
                        shap_exp.base_values = -shap_exp.base_values

            # --- AUTO-Z (Binary/Regression) ---
            if not z_feat or z_feat.lower() == "auto":
                try:
                    # Usamos .values y x_idx para máxima compatibilidad
                    inds = shap.utils.approximate_interactions(x_idx, shap_exp.values, X.values)
                    best_idx = inds[1] if (len(inds) > 1 and inds[0] == x_idx) else inds[0]
                    z_feat = shap_exp.feature_names[best_idx]
                    print(f"ImmersiveSHAP, PYTHON, [Auto-Z] Partner: {z_feat}")
                    config["z_feature"] = z_feat
                except Exception as e:
                    print(f"ImmersiveSHAP, PYTHON, [Auto-Z] Failed: {e}")
                    z_feat = x_feat

            # Final data extraction
            z_vals = X[z_feat].values.tolist()
            color_expl = X[z_feat].values
            y_vals = shap_exp.values[:, x_idx].tolist()

            plt.figure(figsize=(7.12, 5))
            shap.plots.scatter(
                shap_exp[:, x_idx],
                color=color_expl,
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

        # --- Interpretability Calculations ---
        # 1. Base Value
        try:
            base_val = float(shap_exp.base_values[0]) if hasattr(shap_exp.base_values, "__len__") else float(
                shap_exp.base_values)
        except:
            base_val = 0.0

        # 2. Final Predictions (base + sum of impacts)
        final_preds = (shap_exp.base_values + shap_exp.values.sum(axis=1)).tolist()

        # 3. Impact Share (%)
        total_abs_impact = np.abs(shap_exp.values).sum(axis=1)
        # Avoid division by zero
        safe_total = np.where(total_abs_impact == 0, 1e-6, total_abs_impact)
        impact_shares = (np.abs(shap_exp.values[:, x_idx]) / safe_total * 100).tolist()

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
            base_value=base_val,
            final_predictions=final_preds,
            impact_shares=impact_shares,
            x_quantiles=x_quantiles
        )
        data_dict["plot_path"] = output_path
        data_dict["class_label"] = class_label

        return data_dict

    except Exception as e:
        import traceback
        traceback.print_exc()  # Print full error to Python console
        return {"action": "error", "message": f"Error en Python: {str(e)}"}
