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

        string headerColor = meta.finalPrediction > meta.baseValue ? "#FFBBBB" : "#BBBBFF";

        // 1. Header (Target)
        // Usar la palabra genérica "Target" cubre correctamente Clasificación y Regresión.
        string content = $"<size=120%><b>Target: <color={headerColor}>{meta.classLabel}</color></b></size>\n";


        // 2. Spatial Mapping (Equal weight to X and Z)
        content += $"<size=100%><b>X:</b> {meta.featureName} <color=#AAAAAA>({meta.featureValue:F2})</color>\n";
        content += $"<b>Z:</b> {meta.secondaryFeatureName} <color=#AAAAAA>({meta.secondaryFeatureValue:F2})</color></size>\n\n";

        // 3. SHAP Value (Vertical / Y-Axis)
        string color;
        string arrow;
        string sign;

        // Check for neutral/zero value using a safe float threshold
        if (Mathf.Abs(meta.shapValue) < 0.0001f)
        {
            color = "#AAAAAA"; // Gray
            arrow = "-";       // Neutral symbol instead of up/down arrow
            sign = "";         // No sign for pure zero
        }
        else if (meta.shapValue > 0f)
        {
            color = "#FF5555"; // Red for positive contribution
            arrow = "↑";
            sign = "+";
        }
        else
        {
            color = "#3555FF"; // Blue for negative contribution
            arrow = "↓";
            sign = "";         // The minus sign "-" is added automatically by the float formatting
        }

        content += $"<size=115%><b>Y: (SHAP of {meta.featureName}) </b> <color={color}><b>{arrow} {sign}{meta.shapValue:F3}</b></color></size>\n";

        info.body = content;
        return info;
    }
}
