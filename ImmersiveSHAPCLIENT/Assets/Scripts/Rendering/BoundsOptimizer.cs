using UnityEngine;
/// <summary>
/// Optimized component to calculate and cache the visual boundaries of the point cloud.
/// Prevents redundant calculations of thousands of point positions.
/// OPTIMIZADO: Solo recalcula cuando es absolutamente necesario.
/// </summary>
public class BoundsOptimizer : MonoBehaviour
{
    private Bounds cachedBounds;
    private Renderer[] cachedRenderers;
    private bool isDirty = true; // 🚀 Flag para saber si necesita recalcular

    /// <summary>
    /// Marca los bounds como "sucios" para que se recalculen en la próxima llamada a GetBounds.
    /// IMPORTANTE: Solo llamar cuando realmente cambien los puntos.
    /// </summary>
    public void MarkDirty()
    {
        isDirty = true;
    }

    public void RefreshCache()
    {
        // 🚀 OPTIMIZACIÓN: Solo recalcular si está marcado como "dirty"
        if (!isDirty) return;

        // Get all renderers in children (points and axes)
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            cachedBounds = new Bounds(transform.position, Vector3.zero);
            isDirty = false;
            return;
        }
        // Encapsulate all active renderers to find the total volume
        bool initialized = false;
        foreach (var rend in cachedRenderers)
        {
            if (rend != null && rend.enabled)
            {
                if (!initialized) { cachedBounds = rend.bounds; initialized = true; }
                else cachedBounds.Encapsulate(rend.bounds);
            }
        }

        isDirty = false; // 🚀 Marcar como limpio
        Debug.Log($"[BoundsOptimizer] Bounds recalculados: {cachedBounds}");
    }

    /// <summary>
    /// Returns the center and size of the graph in world space.
    /// </summary>
    public bool GetBounds(out Bounds bounds)
    {
        // 🚀 OPTIMIZACIÓN: Recalcular solo si es necesario
        if (isDirty)
            RefreshCache();

        bounds = cachedBounds;
        return cachedBounds.size.sqrMagnitude > 0.001f;
    }
}
