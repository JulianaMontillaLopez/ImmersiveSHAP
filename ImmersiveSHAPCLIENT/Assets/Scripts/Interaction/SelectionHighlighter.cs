using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona el cambio de color visual al hacer hover o seleccionar puntos.
/// Optimizado para usar MaterialPropertyBlock y evitar fugas de memoria.
/// </summary>
public class SelectionHighlighter : MonoBehaviour
{
    [Header("Highlight Colors")]
    public Color hoverColor = new Color(1f, 0.9f, 0.2f, 1f);
    public Color selectedColor = new Color(0.2f, 1f, 1f, 1f);

    private PointSelection selection;
    private MaterialPropertyBlock mpb;
    private readonly Dictionary<GameObject, Renderer> rendererCache = new();
    private readonly Dictionary<GameObject, OriginalColorHolder> holderCache = new(); // 🚀 Caché

    private const string COLOR_PROPERTY = "_BaseColor";
    private const string EMISSION_PROPERTY = "_EmissionColor";

    private void Awake()
    {
        selection = FindFirstObjectByType<PointSelection>();
        if (selection == null)
        {
            enabled = false;
            return;
        }

        mpb = new MaterialPropertyBlock();

        selection.OnHovered += HandleHover;
        selection.OnUnhovered += HandleUnhover;
        selection.OnSelected += ApplySelected;
        selection.OnDeselected += ResetColor;
    }

    private Renderer GetRenderer(GameObject go)
    {
        if (go == null) return null;
        if (!rendererCache.TryGetValue(go, out Renderer r))
        {
            r = go.GetComponent<Renderer>();
            if (r != null) rendererCache[go] = r;
        }
        return r;
    }

    private OriginalColorHolder GetHolder(GameObject go)
    {
        if (go == null) return null;
        if (!holderCache.TryGetValue(go, out OriginalColorHolder h))
        {
            h = go.GetComponent<OriginalColorHolder>();
            if (h != null) holderCache[go] = h;
        }
        return h;
    }


    private void HandleHover(GameObject go)
    {
        if (selection.IsSelected(go)) return;

        var r = GetRenderer(go);
        if (r == null) return;

        r.GetPropertyBlock(mpb);
        mpb.SetColor(COLOR_PROPERTY, hoverColor);
        mpb.SetColor(EMISSION_PROPERTY, hoverColor * 2f);
        r.SetPropertyBlock(mpb);
    }

    private void HandleUnhover(GameObject go)
    {
        if (selection.IsSelected(go)) return;
        ResetColor(go);
    }

    private void ApplySelected(GameObject go)
    {
        var r = GetRenderer(go);
        if (r == null) return;

        r.GetPropertyBlock(mpb);
        mpb.SetColor(COLOR_PROPERTY, selectedColor);
        mpb.SetColor(EMISSION_PROPERTY, selectedColor * 4f);
        r.SetPropertyBlock(mpb);
    }

    private void ResetColor(GameObject go)
    {
        var r = GetRenderer(go);
        if (r == null) return;

        // 🚀 OPTIMIZADO: Usar caché en lugar de GetComponent
        var holder = GetHolder(go);
        if (holder == null) return;

        r.GetPropertyBlock(mpb);
        mpb.SetColor(COLOR_PROPERTY, holder.originalColor);
        mpb.SetColor(EMISSION_PROPERTY, Color.black);
        r.SetPropertyBlock(mpb);
    }

    private void OnDestroy()
    {
        if (selection != null)
        {
            selection.OnHovered -= HandleHover;
            selection.OnUnhovered -= HandleUnhover;
            selection.OnSelected -= ApplySelected;
            selection.OnDeselected -= ResetColor;
        }
    }
}
