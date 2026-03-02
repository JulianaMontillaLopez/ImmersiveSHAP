using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona tooltips anclados (pinned).
/// Solo maneja jerarquía y ciclo de vida.
/// </summary>
public class TooltipPinManager : MonoBehaviour
{
    public static TooltipPinManager Instance { get; private set; }

    [Header("Configuración")]
    public int maxPinnedTooltips = 5;

    [Tooltip("Root del gráfico (rotación/traslación compartida)")]
    public Transform plotRoot;

    private readonly Dictionary<GameObject, TooltipUI> pinnedTooltips = new();
    private readonly List<GameObject> pinOrder = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PinTooltip(TooltipUI tooltip, GameObject sourcePoint)
    {
        if (tooltip == null || sourcePoint == null) return;
        if (pinnedTooltips.ContainsKey(sourcePoint)) return;

        if (pinOrder.Count >= maxPinnedTooltips)
            UnpinTooltip(pinOrder[0]);

        tooltip.SetPinnedState(true);

        tooltip.transform.SetParent(
            plotRoot != null ? plotRoot : transform,
            true
        );

        pinnedTooltips.Add(sourcePoint, tooltip);
        pinOrder.Add(sourcePoint);
    }

    public void UnpinTooltip(GameObject sourcePoint)
    {
        if (sourcePoint == null) return;

        if (pinnedTooltips.TryGetValue(sourcePoint, out TooltipUI tooltip))
        {
            pinnedTooltips.Remove(sourcePoint);
            pinOrder.Remove(sourcePoint);
            TooltipPool.Instance.ReturnToPool(tooltip);
        }
    }

    public bool IsPinned(GameObject sourcePoint)
    {
        return sourcePoint != null && pinnedTooltips.ContainsKey(sourcePoint);
    }

    public void ClearAllPins()
    {
        foreach (var tooltip in pinnedTooltips.Values)
            TooltipPool.Instance.ReturnToPool(tooltip);

        pinnedTooltips.Clear();
        pinOrder.Clear();
    }

    public GameObject GetLastPinnedPoint()
    {
        return pinOrder.Count > 0 ? pinOrder[^1] : null;
    }
}