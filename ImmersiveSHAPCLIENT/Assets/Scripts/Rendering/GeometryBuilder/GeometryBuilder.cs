using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages graph data points with optimized pooling and VR-friendly metadata.
/// Optimized to reuse active objects instead of cycles of deactivation/activation.
/// </summary>
public class GeometryBuilder : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject pointPrefab;

    [Header("Pooling Configuration")]
    public int initialPoolSize = 2000;
    public bool usePooling = true;

    [Header("Hierarchy Management")]
    public Transform pointsParent;

    [Header("Appearance (VR-friendly)")]
    [Range(0.005f, 0.10f)]
    [Tooltip("Proportion of the plot axis size. 0.025 = 2.5% of the axis length (RECOMMENDED).")]
    public float pointSizeRatio = 0.025f; // 🚀 AUMENTADO: Antes era 0.012, ahora 0.025 (2x más grande)


    private List<GameObject> pooledPoints = new();
    private List<GameObject> activePoints = new();

    private void Awake()
    {
        if (usePooling)
            InitializePool();
    }

    private void InitializePool()
    {
        if (pointsParent == null) pointsParent = this.transform;

        for (int i = 0; i < initialPoolSize; i++)
        {
            var go = Instantiate(pointPrefab, pointsParent);
            go.SetActive(false);
            pooledPoints.Add(go);
        }
    }

    /// <summary>
    /// Builds points and assigns comprehensive SHAP interpretability metadata.
    /// This version is OPTIMIZED to reuse currently active points and avoid SetActive cycles.
    /// </summary>
    public void BuildPoints(
        Vector3[] positions,
        List<float> shapValues,
        List<float> xValues,
        List<float> zValues,
        string xLabel,
        string zLabel,
        Vector3 scaledMin,
        Vector3 scaledMax,
        string classLabel,
        float baseValue,
        List<float> finalPredictions,
        List<float> impactShares,
        List<float> unitQuantiles,
        bool clampToBounds = false
    )
    {
        if (positions == null || positions.Length == 0) return;
        if (pointsParent == null) pointsParent = this.transform;

        int newCount = positions.Length;
        int currentActiveCount = activePoints.Count;

        // 🔄 OPTIMIZACIÓN: Manejo de Delta de objetos
        if (newCount < currentActiveCount)
        {
            // Tenemos más de los que necesitamos: Devolver excedente al pool
            for (int i = currentActiveCount - 1; i >= newCount; i--)
            {
                var go = activePoints[i];
                go.SetActive(false);
                if (usePooling) pooledPoints.Add(go);
                activePoints.RemoveAt(i);
            }
        }
        else if (newCount > currentActiveCount)
        {
            // Necesitamos más: Sacar del pool los que faltan
            for (int i = currentActiveCount; i < newCount; i++)
            {
                activePoints.Add(GetOrInstantiatePoint());
            }
        }

        // En este punto, activePoints.Count == newCount.
        // Ahora solo actualizamos la data sin llamar a SetActive(true) en los que ya estaban.

        float axisSize = Mathf.Max(0.1f, scaledMax.x - scaledMin.x);
        float scale = axisSize * pointSizeRatio;

        for (int i = 0; i < newCount; i++)
        {
            GameObject pointGO = activePoints[i];

            // 1. Posición y Escala
            Vector3 targetPos = positions[i];
            if (clampToBounds)
                targetPos = Vector3.Max(scaledMin, Vector3.Min(scaledMax, targetPos));

            pointGO.transform.localPosition = targetPos;
            pointGO.transform.localScale = Vector3.one * scale;

            // 2. Asegurar que esté activo (solo si no lo estaba ya)
            if (!pointGO.activeSelf) pointGO.SetActive(true);

            // 3. Metadata (Simplificado: asumiendo que el prefab tiene el componente)
            var meta = pointGO.GetComponent<DataPointMeta>();
            if (meta == null) meta = pointGO.AddComponent<DataPointMeta>();

            meta.index = i;
            meta.classLabel = classLabel;
            meta.featureName = xLabel;
            meta.featureValue = xValues[i];
            meta.shapValue = shapValues[i];
            meta.secondaryFeatureName = zLabel;
            meta.secondaryFeatureValue = zValues[i];

            meta.baseValue = baseValue;
            meta.finalPrediction = (i < finalPredictions.Count) ? finalPredictions[i] : 0f;
            meta.impactShare = (i < impactShares.Count) ? impactShares[i] : 0f;
            meta.quantile = (i < unitQuantiles.Count) ? unitQuantiles[i] : 0.5f;

            meta.increasesPrediction = meta.shapValue > 0f;
        }

        Debug.Log($"ImmersiveSHAP, UNITY, [GeometryBuilder] Optimized Render for {newCount} points.");
    }

    private GameObject GetOrInstantiatePoint()
    {
        if (usePooling && pooledPoints.Count > 0)
        {
            int lastIdx = pooledPoints.Count - 1;
            GameObject go = pooledPoints[lastIdx];
            pooledPoints.RemoveAt(lastIdx);
            return go;
        }
        return Instantiate(pointPrefab, pointsParent);
    }

    public List<GameObject> GetActiveInstances() => activePoints;

    public void ClearPoints(bool destroy = false)
    {
        for (int i = activePoints.Count - 1; i >= 0; i--)
        {
            var go = activePoints[i];
            if (go == null) continue;
            if (destroy) Destroy(go);
            else
            {
                go.SetActive(false);
                if (usePooling) pooledPoints.Add(go);
            }
        }
        activePoints.Clear();
    }
}
