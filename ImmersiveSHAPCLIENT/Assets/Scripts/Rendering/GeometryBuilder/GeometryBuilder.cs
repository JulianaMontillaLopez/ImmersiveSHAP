using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages graph data points with optimized pooling and VR-friendly metadata.
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
    public float pointSizeRatio = 0.025f;

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

        // 🔄 MANEJO DE DELTA: Ajustamos lista de activos
        if (newCount < currentActiveCount)
        {
            for (int i = currentActiveCount - 1; i >= newCount; i--)
            {
                var go = activePoints[i];
                if (go != null)
                {
                    go.SetActive(false);
                    if (usePooling) pooledPoints.Add(go);
                }
                activePoints.RemoveAt(i);
            }
        }
        else if (newCount > currentActiveCount)
        {
            for (int i = currentActiveCount; i < newCount; i++)
            {
                activePoints.Add(GetOrInstantiatePoint());
            }
        }

        // --- Actualización de Data ---
        float axisSize = Mathf.Max(0.1f, scaledMax.x - scaledMin.x);
        float scale = axisSize * pointSizeRatio;

        for (int i = 0; i < newCount; i++)
        {
            GameObject pointGO = activePoints[i];
            if (pointGO == null) continue;

            pointGO.transform.localPosition = positions[i];
            pointGO.transform.localScale = Vector3.one * scale;

            if (!pointGO.activeSelf) pointGO.SetActive(true);

            // 🚀 MEJORA: Caché de componente para evitar GetComponent repetitivos
            var meta = pointGO.GetComponent<DataPointMeta>();
            if (meta != null)
            {
                meta.index = i;
                meta.classLabel = classLabel;
                meta.featureName = xLabel;
                meta.featureValue = (i < xValues.Count) ? xValues[i] : 0f;
                meta.shapValue = (i < shapValues.Count) ? shapValues[i] : 0f;
                meta.secondaryFeatureName = zLabel;
                meta.secondaryFeatureValue = (i < zValues.Count) ? zValues[i] : 0f;
                meta.baseValue = baseValue;
                meta.finalPrediction = (i < finalPredictions.Count) ? finalPredictions[i] : 0f;
                meta.impactShare = (i < impactShares.Count) ? impactShares[i] : 0f;
                meta.quantile = (i < unitQuantiles.Count) ? unitQuantiles[i] : 0.5f;
                meta.increasesPrediction = meta.shapValue > 0f;
            }
        }
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

    /// <summary>
    /// MEJORADO: Ahora maneja correctamente la limpieza total para "New Plot".
    /// </summary>
    public void ClearPoints(bool destroy = false)
    {
        for (int i = activePoints.Count - 1; i >= 0; i--)
        {
            var go = activePoints[i];
            if (go == null) continue;

            if (destroy)
            {
                Destroy(go);
            }
            else
            {
                go.SetActive(false);
                if (usePooling) pooledPoints.Add(go);
            }
        }
        activePoints.Clear();

        // Si pedimos destrucción total (New Plot), también limpiamos el pool de reserva
        if (destroy)
        {
            foreach (var p in pooledPoints) if (p != null) Destroy(p);
            pooledPoints.Clear();

            // Si destruimos todo, reiniciamos el pool para la siguiente carga si usePooling es true
            if (usePooling) InitializePool();
        }
    }

    public List<GameObject> GetActiveInstances() => activePoints;
}
