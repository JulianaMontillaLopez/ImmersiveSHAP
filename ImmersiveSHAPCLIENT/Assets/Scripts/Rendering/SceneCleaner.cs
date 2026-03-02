using UnityEngine;
/// <summary>
/// Static utility to clear the visualization from the scene.
/// Always called before building a new plot to prevent data overlap.
/// </summary>
public static class SceneCleaner
{
    public static void ClearScene()
    {
        // 1. Use the GeometryBuilder to handle point pooling instead of destruction
        var geometryBuilder = Object.FindFirstObjectByType<GeometryBuilder>();
        if (geometryBuilder != null)
            geometryBuilder.ClearPoints(destroy: false); // ♻️ reuse objects
        // 2. Clear Axis references
        var axesBuilder = Object.FindFirstObjectByType<AxesAndReferenceBuilder>();
        if (axesBuilder != null)
            axesBuilder.ClearAxes(destroy: false); // ♻️ deactivate instead of destroy
        Debug.Log("ImmersiveSHAP, UNITY, 🧹 SceneCleaner: Scene cleared via submodules.");
    }
}