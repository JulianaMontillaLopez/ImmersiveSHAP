using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Orchestrates the entire 3D SHAP plot pipeline.
/// Optimized to handle filtered and scaled data correctly across the pipeline.
/// </summary>
public class PlotManager : MonoBehaviour
{
    public static PlotManager Instance { get; private set; }

    [Header("Submodules")]
    public AxesAndReferenceBuilder axesBuilder;
    public PointFilter pointFilter;
    public DataScalerAndAligner dataScaler;
    public GeometryBuilder geometryBuilder;
    public VisualEncoder visualEncoder;
    public SceneLayoutManager layoutManager;
    public RendererController rendererController;
    public PointSelection selection;

    [Header("Layout Settings")]
    public Transform plotRoot;

    // Data Buffers
    private Vector3[] rawPositions;
    private Vector3[] filteredPositions;
    private Vector3[] scaledPositions;

    private List<float> currentXValues;
    private List<float> currentShapValues;
    private List<float> currentZValues;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Pipeline entry point. Called by DeserializationClient.
    /// </summary>
    public void RenderPlot(DeserializedData data)
    {
        if (plotRoot == null)
        {
            Debug.LogError("ImmersiveSHAP, UNITY, [PlotManager] PlotRoot not assigned.");
            return;
        }

        Debug.Log($"ImmersiveSHAP, UNITY, [PlotManager] Starting render pipeline for {data.x.Length} points.");

        // Clear previous session
        SceneCleaner.ClearScene();

        // Limpiar selección previa
        if (PointSelection.Instance != null)
            PointSelection.Instance.ClearSelection();

        // 1️⃣ Data Preparation (Raw)
        DataInputManager.Prepare(
            data,
            out rawPositions,
            out currentXValues,
            out currentShapValues,
            out currentZValues
        );

        // 2️⃣ Filtering (Clips outliers or applies filters)
        (filteredPositions, currentXValues, currentShapValues, currentZValues) =
            pointFilter.ApplyFilters(rawPositions, currentXValues, currentShapValues, currentZValues);

        // 3️⃣ Scaling and Alignment (Normalizes coordinates to meters)
        dataScaler.Process(filteredPositions);
        scaledPositions = dataScaler.GetScaledPoints();
        var (scaledMin, scaledMax) = dataScaler.GetAxisRanges(true);

        // 4️⃣ Geometry Construction (UI Points)
        if (geometryBuilder.pointsParent == null)
        {
            var dataPointsGO = new GameObject("DataPoints");
            dataPointsGO.transform.SetParent(plotRoot, false);
            geometryBuilder.pointsParent = dataPointsGO.transform;
        }

        // ✅ RECTIFICADO: Pasamos los scaledPositions y la data filtrada
        geometryBuilder.BuildPoints(
            scaledPositions,
            currentShapValues,
            currentXValues,
            currentZValues,
            data.x_label,
            data.z_label,
            scaledMin,
            scaledMax,
            data.class_label,
            data.base_value,
            data.final_predictions.ToList(),
            data.impact_shares.ToList(),
            data.x_quantiles.ToList(),
            false
        );

        // 5️⃣ Retrieve active point instances for coloring
        var points = geometryBuilder.GetActiveInstances();

        // 6️⃣ Axes and Landmark Generation
        axesBuilder.BuildAxesFromScaledData(Vector3.zero, dataScaler,
            data.x_label, data.shap_label, data.z_label);

        float currentTargetScale = dataScaler.TargetScale;
        axesBuilder.BuildZeroPlane(dataScaler, currentTargetScale);

        // 7️⃣ Visual Encoding (Color Mapping)
        string colormap = VisualMappingUIManager.Instance.GetSelectedColormap();
        visualEncoder.SetActiveColormap(colormap ?? data.colormap);

        float minZ = data.z_min;
        float maxZ = data.z_max;
        var normalizedColors = currentZValues
            .Select(v => Mathf.Clamp01((v - minZ) / Mathf.Max(1e-6f, maxZ - minZ)))
            .ToArray();

        visualEncoder.Encode(points.ToArray(), normalizedColors, currentShapValues.ToArray());

        // 8️⃣ Final Layout and Rendering
        layoutManager?.PositionPlot(plotRoot);
        rendererController?.ApplyPlatformSpecificSettings();
        rendererController?.EnableRendering();

        Debug.Log("ImmersiveSHAP, UNITY, [PlotManager] Render pipeline completed successfully.");
    }



    /// <summary>
    /// Elimina el gráfico actual de la vista y resetea los estados internos.
    /// Invocado desde el botón "New Plot" del menú de pausa.
    /// </summary>
    public void ClearCurrentPlot()
    {
        Debug.Log("ImmersiveSHAP, UNITY, [PlotManager] Solicitud de limpieza total del gráfico.");
        // 1. Limpieza física de GameObjects (Usando tu lógica de SceneCleaner)
        SceneCleaner.ClearScene(completelyDestroy: true);
        // 2. Limpiar buffers de datos para liberar memoria RAM
        rawPositions = null;
        filteredPositions = null;
        scaledPositions = null;
        currentXValues?.Clear();
        currentShapValues?.Clear();
        currentZValues?.Clear();
        // 3. Ocultar el objeto raíz si es necesario
        if (plotRoot != null)
        {
            // Opcional: Podrías resetear la escala del gráfico si el usuario lo movió/escaló
            plotRoot.localScale = Vector3.one;
            // plotRoot.gameObject.SetActive(false); // Descomentar si quieres ocultar el root
        }
        // 4. Notificar a otros sistemas si es necesario (ej: ocultar leyendas)
        if (rendererController != null)
        {
            rendererController.DisableRendering();
        }

    }



    private void OnDisable()
    {
        if (geometryBuilder != null)
            geometryBuilder.ClearPoints();

        if (layoutManager != null)
            layoutManager.ResetScene();
    }
}
