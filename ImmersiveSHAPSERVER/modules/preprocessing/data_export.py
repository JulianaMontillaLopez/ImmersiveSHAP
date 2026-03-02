# modules/preprocessing/data_export.py

import numpy as np


def export(x, y, z, x_label, z_label, shap_label, colormap="cmap_red_blue",
           base_value=0.0, final_predictions=None, impact_shares=None, x_quantiles=None):
    """
    Formats output data for Unity, ensuring all interpretability metrics are included.
    Includes percentile normalization (5-95) for visual stability.
    """
    if final_predictions is None: final_predictions = []
    if impact_shares is None: impact_shares = []
    if x_quantiles is None: x_quantiles = []

    def summarize(values, name):
        try:
            values_clean = [v for v in values if v is not None and not np.isnan(v)]
            if not values_clean: return

            print(f"   • {name}: Min {min(values_clean):.3f} | Max {max(values_clean):.3f}")
        except:
            pass

    # --- Z-Axis Percentile Calculation (SHAP parity) ---
    z_array = np.array(z).astype(float)
    z_array = z_array[~np.isnan(z_array)]

    if len(z_array) > 0:
        z_min_p = float(np.percentile(z_array, 5))
        z_max_p = float(np.percentile(z_array, 95))
        if z_min_p == z_max_p:
            z_min_p, z_max_p = float(np.min(z_array)), float(np.max(z_array))
    else:
        z_min_p, z_max_p = 0.0, 1.0

    print(f"\n📤 [DataExport] Exporting {len(x)} points to Unity:")
    summarize(x, "X (Feature)")
    summarize(y, "Y (SHAP)")
    summarize(z, "Z (Secondary)")
    print(f"   • SHAP Color Range (5-95%): [{z_min_p:.4f}, {z_max_p:.4f}]")

    # Sanitize inputs (Convert None/NaN to 0.0 for Unity JSON compatibility)
    def sanitize(lst):
        return [0.0 if (v is None or np.isnan(v)) else float(v) for v in lst]

    return {
        "x": sanitize(x),
        "y": sanitize(y),
        "z": sanitize(z),
        "x_label": x_label,
        "z_label": z_label,
        "shap_label": shap_label,
        "colormap": colormap,
        "z_min": z_min_p,
        "z_max": z_max_p,
        # --- NEW Interpretability Fields ---
        "base_value": float(base_value),
        "final_predictions": sanitize(final_predictions),
        "impact_shares": sanitize(impact_shares),
        "x_quantiles": sanitize(x_quantiles)
    }

