# modules/preprocessing/plot_dispatcher.py

from modules.preprocessing import generate_scatter_plot


def dispatch(config, X, shap_values, explainer, y=None):
    """
    Directs the request to the corresponding plot generator.

    Parameters:
        config (dict): plot configuration sent from Unity.
        X (pd.DataFrame): preprocessed data.
        shap_values (shap.Explanation): SHAP values object.
        explainer (shap.Explainer): (optional) explainer object.
        y (pd.Series): (optional) target labels.

    Returns:
        dict: data ready for Unity.
    """
    plot_type = config.get("plot_type")
    plot_data = None

    if plot_type == "scatter":
        plot_data = generate_scatter_plot.create(config, X, shap_values, y)

    # Note: generate_dependence_plot removed for now as only scatter is active
    # elif plot_type == "dependence":
    #    plot_data = generate_dependence_plot.create(config, X, shap_values, explainer)

    else:
        return {
            "action": "error",
            "message": f"Plot type '{plot_type}' not supported."
        }

    # 🔹 Ensure 'action' field is always present
    if not isinstance(plot_data, dict):
        return {
            "action": "error",
            "message": "The generator did not return a valid dictionary."
        }

    if "action" not in plot_data:
        plot_data["action"] = "plot_response"

    return plot_data
