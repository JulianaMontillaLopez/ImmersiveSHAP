using UnityEngine;

/// <summary>
/// Normalizes point cloud data into Unity space [0, TargetScale].
/// Implements "Nice Numbers" logic for axes and optimized bounding boxes.
/// </summary>
public class DataScalerAndAligner : MonoBehaviour
{
    public Vector3[] scaledPoints { get; private set; }

    // Internal State
    private Vector3 minValues, maxValues; // Original raw extremes
    private Vector3 visualMin, visualMax; // Padded "Nice" extremes for axes
    private Vector3 scaledMin, scaledMax; // Final Unity space [0, Target]

    [Header("Scale Configuration")]
    [SerializeField] private float targetScale = 1.0f;
    public float TargetScale => targetScale;

    [Tooltip("Padding factor (2% is ideal for VR).")]
    [SerializeField, Range(0f, 0.1f)] private float axisPadding = 0.02f;

    public void Process(Vector3[] inputPoints)
    {
        if (inputPoints == null || inputPoints.Length == 0) return;

        // 1. Scan for raw data extremes (Ignoring NaNs)
        minValues = Vector3.one * float.MaxValue;
        maxValues = Vector3.one * float.MinValue;

        foreach (var p in inputPoints)
        {
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z)) continue;
            minValues = Vector3.Min(minValues, p);
            maxValues = Vector3.Max(maxValues, p);
        }

        // Safety fallback if no valid coordinates found
        if (minValues.x == float.MaxValue) { minValues = Vector3.zero; maxValues = Vector3.one; }

        // 2. Compute "Nice" boundaries for the axes (expanded outward)
        visualMin = new Vector3(CalculateNiceBound(minValues.x, false), CalculateNiceBound(minValues.y, false), CalculateNiceBound(minValues.z, false));
        visualMax = new Vector3(CalculateNiceBound(maxValues.x, true), CalculateNiceBound(maxValues.y, true), CalculateNiceBound(maxValues.z, true));

        scaledMin = Vector3.zero;
        scaledMax = Vector3.one * targetScale;

        Vector3 range = visualMax - visualMin;
        // Optimization: Calculate inverted range to use multiplication instead of division in the loop
        Vector3 invRange = new Vector3(
            1f / Mathf.Max(1e-6f, range.x),
            1f / Mathf.Max(1e-6f, range.y),
            1f / Mathf.Max(1e-6f, range.z)
        );

        // 3. Normalize points into Unity world space
        scaledPoints = new Vector3[inputPoints.Length];
        for (int i = 0; i < inputPoints.Length; i++)
        {
            Vector3 p = inputPoints[i];

            // Normalize current point relative to the "Nice" visual range
            float nx = float.IsNaN(p.x) ? 0.5f : (p.x - visualMin.x) * invRange.x;
            float ny = float.IsNaN(p.y) ? 0.5f : (p.y - visualMin.y) * invRange.y;
            float nz = float.IsNaN(p.z) ? 0.5f : (p.z - visualMin.z) * invRange.z;

            scaledPoints[i] = new Vector3(
                Mathf.Clamp01(nx) * targetScale,
                Mathf.Clamp01(ny) * targetScale,
                Mathf.Clamp01(nz) * targetScale
            );
        }

        Debug.Log($"ImmersiveSHAP, UNITY, [Scaler] Result: Y-Axis from {visualMin.y:G3} to {visualMax.y:G3}");
    }

    private float CalculateNiceBound(float val, bool upward)
    {
        // Safety: If value is zero or near zero, return exactly zero to keep the origin clean
        if (Mathf.Abs(val) < 0.0001f) return 0f;

        // Correct Padding: Expand AWAY from zero regardless of sign
        float margin = Mathf.Abs(val) * axisPadding;
        float buffered = upward ? (val + margin) : (val - margin);

        // Find the magnitude (power of 10)
        float mag = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(Mathf.Abs(val))));
        float step = mag / 2f; // Steps of 0.5, 5, 50, etc.

        return upward ? Mathf.Ceil(buffered / step) * step : Mathf.Floor(buffered / step) * step;
    }

    /* ------------------------------------------------------------
     *  GETTERS (Used by PlotManager and AxesBuilder)
     * ------------------------------------------------------------ */
    public Vector3[] GetScaledPoints() => scaledPoints;

    public (Vector3 min, Vector3 max) GetAxisRanges(bool scaled = true) =>
        scaled ? (scaledMin, scaledMax) : (visualMin, visualMax);

    public Bounds GetBounds() => new Bounds((scaledMin + scaledMax) * 0.5f, scaledMax - scaledMin);
}