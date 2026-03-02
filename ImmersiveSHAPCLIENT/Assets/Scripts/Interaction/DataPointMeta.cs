using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores per-point metadata for tooltips and explainability analysis.
/// </summary>
[Serializable]
public class DataPointMeta : MonoBehaviour
{
    [Header("Identity")]
    public int index;
    public string classLabel;

    [Header("Primary Feature (X-Axis)")]
    public string featureName;
    public float featureValue;
    [Range(0, 1)] public float quantile;

    [Header("SHAP Impact (Y-Axis)")]
    public float shapValue;
    [Range(0, 100)] public float impactShare;

    [Header("Secondary Feature (Z-Axis/Color)")]
    public string secondaryFeatureName;
    public float secondaryFeatureValue;

    [Header("Model Journey")]
    public float baseValue; // E[f(x)]
    public float finalPrediction; // f(x)

    [Header("State")]
    public bool increasesPrediction;
    public int shapRank = -1;

    [TextArea(2, 4)]
    public string note;

    [NonSerialized] public Dictionary<string, float> additionalFeatures;
}
