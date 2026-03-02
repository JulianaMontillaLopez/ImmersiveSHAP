using UnityEngine;

/// <summary>
/// Muestra tooltips temporales en hover usando el TooltipPool.
/// </summary>
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    private TooltipUI currentHoverTooltip;
    private GameObject currentHoverTarget;
    private PointSelection selection;

    private void Awake()
    {
        Instance = this;
        selection = FindFirstObjectByType<PointSelection>();

        if (selection != null)
        {
            selection.OnHovered += ShowTooltip;
            selection.OnUnhovered += HideTooltip;
            selection.OnSelected += HandleSelection;
        }
    }

    private void ShowTooltip(GameObject go)
    {
        if (go == null) return;

        // Si el punto ya está seleccionado (pinned), no mostramos el de hover
        if (selection.IsSelected(go)) return;

        // Limpiar anterior si existe
        if (currentHoverTooltip != null) HideTooltip(currentHoverTarget);

        currentHoverTarget = go;
        currentHoverTooltip = TooltipPool.Instance.Get();

        var info = DataContentExtractor.Extract(go);
        currentHoverTooltip.SetContent(info.body);
        currentHoverTooltip.AttachTo(go.transform);
    }

    private void HideTooltip(GameObject go)
    {
        if (currentHoverTarget == go && currentHoverTooltip != null)
        {
            TooltipPool.Instance.ReturnToPool(currentHoverTooltip);
            currentHoverTooltip = null;
            currentHoverTarget = null;
        }
    }

    private void HandleSelection(GameObject go)
    {
        // Cuando un punto se selecciona:
        // 1. Si tenía un tooltip de hover, lo "promocionamos" o simplemente lo quitamos para que el PinManager cree el suyo
        if (currentHoverTarget == go)
        {
            var tooltipToPin = currentHoverTooltip;
            currentHoverTooltip = null;
            currentHoverTarget = null;

            // Lo mandamos al PinManager
            TooltipPinManager.Instance.PinTooltip(tooltipToPin, go);
        }
        else
        {
            // Si se selecciona sin haber hecho hover previo (raro pero posible)
            var newTooltip = TooltipPool.Instance.Get();
            var info = DataContentExtractor.Extract(go);
            newTooltip.SetContent(info.body);
            newTooltip.AttachTo(go.transform);
            TooltipPinManager.Instance.PinTooltip(newTooltip, go);
        }
    }

    private void OnDestroy()
    {
        if (selection != null)
        {
            selection.OnHovered -= ShowTooltip;
            selection.OnUnhovered -= HideTooltip;
            selection.OnSelected -= HandleSelection;
        }
    }
}
