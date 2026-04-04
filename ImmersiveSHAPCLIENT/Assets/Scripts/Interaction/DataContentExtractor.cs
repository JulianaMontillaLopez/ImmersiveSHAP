using UnityEngine;

/// <summary>
/// Converts DataPointMeta into an ultra-compact, theory-grounded format.
/// Uses SHAP terminology (Base Value, Shap Value, Prediction) for clarity.
/// </summary>
public static class DataContentExtractor
{
    public struct TooltipInfo
    {
        public string body;
    }

    public static TooltipInfo Extract(GameObject go)
    {
        TooltipInfo info = new TooltipInfo();

        if (go == null)
        {
            info.body = "No data.";
            return info;
        }

        var meta = go.GetComponent<DataPointMeta>();
        if (meta == null)
        {
            info.body = "No metadata.";
            return info;
        }

        // --- Concise SHAP Layout ---

        // 1. Prediction Outcome
        //string headerColor = meta.finalPrediction > meta.baseValue ? "#FFBBBB" : "#BBBBFF";
        //string content = $"<size=110%><b>Target: <color={headerColor}>{meta.classLabel}</color></b></size> <size=80%><color=#AAAAAA>(#{meta.index})</color></size>\n";
        //content += "<color=#555555>__________________________</color>\n";

        // 2. Feature Values
        //content += $"<size=90%><b>X:</b> {meta.featureValue:F3} | <b>Z:</b> {meta.secondaryFeatureValue:F3}</size>\n\n";

        // 3. SHAP Impact (The "Contribution")
        //string color = meta.increasesPrediction ? "#FF5555" : "#3555FF";
        //string arrow = meta.increasesPrediction ? "↑" : "↓";
        //string sign = meta.shapValue >= 0 ? "+" : "";

        //content += $"<b>SHAP Value:</b> <color={color}><b>{arrow} {sign}{meta.shapValue:F4}</b></color>\n";
        //content += $"<size=85%><color=#BBBBBB>Weight: {meta.impactShare:F1}% contribution</color></size>\n\n";

        // 4. Prediction Flow (Base -> Final)
        //content += $"<size=90%>Base (Avg): {meta.baseValue:F2}  →  <b>Final Prediction: {meta.finalPrediction:F2}</b></size>";


        string headerColor = meta.finalPrediction > meta.baseValue ? "#FFBBBB" : "#BBBBFF";

        // 1. Header (Target Class)
        string content = $"<size=120%><b>Target Class: <color={headerColor}>{meta.classLabel}</color></b></size>\n";
        content += "<color=#555555>__________________________</color>\n\n";

        // 2. Spatial Mapping (Equal weight to X and Z)
        content += $"<size=100%><b>X:</b> {meta.featureName} <color=#AAAAAA>({meta.featureValue:F2})</color>\n";
        content += $"<b>Z:</b> {meta.secondaryFeatureName} <color=#AAAAAA>({meta.secondaryFeatureValue:F2})</color></size>\n\n";

        // 3. SHAP Value (Vertical / Y-Axis)
        string color = meta.increasesPrediction ? "#FF5555" : "#3555FF";
        string arrow = meta.increasesPrediction ? "↑" : "↓";
        string sign = meta.shapValue >= 0 ? "+" : "";

        content += $"<size=115%><b>Y:(SHAP Value of {meta.featureName}) </b> <color={color}><b>{arrow} {sign}{meta.shapValue:F3}</b></color></size>\n";
        //content += $"<size=85%><color=#BBBBBB><i>Relative influence of {meta.featureName} on {meta.classLabel}</i></color></size>";

        info.body = content;
        return info;
    }
}
