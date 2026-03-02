using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Axis : MonoBehaviour
{
    [Header("References")]
    public LineRenderer lineRenderer;
    public Transform tip;
    public TextMeshPro labelText;

    [Header("Tick Configuration")]
    public GameObject tickLabelPrefab;
    public Transform tickLabelParent;
    [Tooltip("Min distance (meters) between labels. 0.12f = 12cm of safe space.")]
    public float minLabelMargin = 0.12f;

    [Header("Font Sizing (VR Micro-scale)")]
    public float titleScaleMultiplier = 0.005f; // X,Y,Z labels
    public float tickScaleMultiplier = 0.0035f; // Numeric labels (0.1, 0.2...)

    private readonly float lineThickness = 0.008f;
    private readonly float tipSize = 0.04f;
    private readonly List<GameObject> currentTicks = new();

    public void BuildAxis(Vector3 origin, Vector3 direction, Color color,
                      float minS, float maxS, float minO, float maxO, string label, float len)
    {
        transform.position = origin;
        transform.rotation = GetAxisRotation(direction);

        Vector3 start = Vector3.up * minS;
        Vector3 end = Vector3.up * maxS;

        // 1. LineRenderer - Setup path and color
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startWidth = lineThickness;
        lineRenderer.endWidth = lineThickness;

        if (lineRenderer.material == null || lineRenderer.material.name.Contains("Default"))
            lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineRenderer.material.color = color;

        // 2. Axis Tip (Arrowhead)
        if (tip != null)
        {
            tip.localScale = Vector3.one * tipSize;
            tip.localPosition = end;
            if (tip.TryGetComponent<Renderer>(out var r)) r.material.color = color;
        }

        // 3. Axis Main Label (Title)
        if (labelText != null)
        {
            labelText.text = label;
            labelText.color = color;
            labelText.transform.localScale = Vector3.one * (len * titleScaleMultiplier);
            labelText.transform.localPosition = end + (Vector3.up * len * 0.15f);
        }

        GenerateTicks(minS, maxS, len, minO, maxO);
    }

    private void GenerateTicks(float minS, float maxS, float axisLen, float minO, float maxO)
    {
        foreach (var t in currentTicks) if (t != null) Destroy(t);
        currentTicks.Clear();

        // Calculate logical nice ticks using updated logic
        List<float> vals = CalculateNiceTicks(minO, maxO, 8);
        Vector3 lastTickPos = Vector3.one * -100f;

        foreach (float v in vals)
        {
            float range = maxO - minO;
            if (Mathf.Approximately(range, 0)) continue;

            float nx = (v - minO) / range;
            Vector3 pos = Vector3.up * Mathf.Lerp(minS, maxS, nx);

            // ⛔ CULLING: Check world-space distance between labels
            if (Vector3.Distance(pos, lastTickPos) < minLabelMargin) continue;

            GameObject tick = Instantiate(tickLabelPrefab, tickLabelParent ?? transform);
            tick.transform.localPosition = pos;
            tick.transform.localScale = Vector3.one * (axisLen * tickScaleMultiplier);

            if (tick.TryGetComponent<TextMeshPro>(out var tmp))
            {
                tmp.text = v.ToString("G3"); // Professional scientific formatting
                tmp.alignment = TextAlignmentOptions.Center;
            }
            currentTicks.Add(tick);
            lastTickPos = pos;
        }
    }

    private List<float> CalculateNiceTicks(float min, float max, int maxT)
    {
        float r = NiceNum(max - min, false);
        float step = NiceNum(r / (maxT - 1), true);
        List<float> ticks = new();
        if (step <= 0) return ticks;
        for (float t = Mathf.Floor(min / step) * step; t <= max + step * 0.1f; t += step)
            ticks.Add((float)System.Math.Round(t, 4));
        return ticks;
    }

    private float NiceNum(float range, bool round)
    {
        float exp = Mathf.Floor(Mathf.Log10(Mathf.Max(1e-6f, range)));
        float frac = range / Mathf.Pow(10, exp);
        float n;
        if (round)
        {
            if (frac < 1.5f) n = 1f; else if (frac < 3f) n = 2f; else if (frac < 7f) n = 5f; else n = 10f;
        }
        else
        {
            if (frac <= 1f) n = 1f; else if (frac <= 2f) n = 2f; else if (frac <= 5f) n = 5f; else n = 10f;
        }
        return n * Mathf.Pow(10, exp);
    }

    private Quaternion GetAxisRotation(Vector3 dir)
    {
        if (dir == Vector3.right) return Quaternion.Euler(0, 0, -90);
        if (dir == Vector3.forward) return Quaternion.Euler(90, 0, 0);
        return Quaternion.identity; // Y Axis
    }

    private void LateUpdate()
    {
        if (Camera.main == null) return;
        Transform cam = Camera.main.transform;
        // All text elements look at the user for VR legibility
        if (labelText != null) labelText.transform.rotation = Quaternion.LookRotation(labelText.transform.position - cam.position);
        foreach (var t in currentTicks) if (t != null) t.transform.rotation = Quaternion.LookRotation(t.transform.position - cam.position);
    }
}
