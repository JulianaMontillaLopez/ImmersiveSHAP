using UnityEngine;

public static class SceneCleaner
{
    /// <summary>
    /// Limpia la visualización actual llamando a los submódulos responsables.
    /// </summary>
    /// <param name="completelyDestroy">Si es true, borra los objetos. Si es false, los oculta/devuelve al pool.</param>
    public static void ClearScene(bool completelyDestroy = false)
    {
        // 1. Limpiar Puntos de Datos
        var geometryBuilder = Object.FindFirstObjectByType<GeometryBuilder>();
        if (geometryBuilder != null)
        {
            geometryBuilder.ClearPoints(completelyDestroy);
        }

        // 2. Limpiar Ejes y Referencias
        var axesBuilder = Object.FindFirstObjectByType<AxesAndReferenceBuilder>();
        if (axesBuilder != null)
        {
            axesBuilder.ClearAxes(completelyDestroy);
        }

        // 3. Limpiar Selección Visual (importante para que no queden tooltips huérfanos)
        if (PointSelection.Instance != null)
        {
            PointSelection.Instance.ClearSelection();
        }

        Debug.Log($"ImmersiveSHAP, UNITY, 🧹 SceneCleaner: Escena limpia (Destruir: {completelyDestroy}).");
    }
}
