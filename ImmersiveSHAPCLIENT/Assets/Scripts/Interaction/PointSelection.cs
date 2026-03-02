using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Gestiona la selección de puntos con lógica de alternancia (Toggle).
/// Ahora escucha directamente a los XRSimpleInteractable para evitar el overhead de scripts por punto.
/// </summary>
public class PointSelection : MonoBehaviour
{
    public static PointSelection Instance { get; private set; }

    [Header("Mode")]
    public bool allowMultiSelection = true;
    public int maxSelections = 5;

    private readonly HashSet<GameObject> selectionSet = new();
    private readonly List<GameObject> selectionOrder = new();

    public event Action<GameObject> OnSelected;
    public event Action<GameObject> OnDeselected;
    public event Action<GameObject> OnHovered;
    public event Action<GameObject> OnUnhovered;

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
    /// Este método debe ser llamado por los puntos (o un relay global) cuando son clickeados.
    /// Como el usuario quiere eliminar PointXRRelay, los puntos pueden llamar a esto 
    /// mediante el evento selectEntered del XRSimpleInteractable mapeado a este componente.
    /// </summary>
    public void HandleSelect(GameObject go)
    {
        if (go == null) return;

        if (selectionSet.Contains(go))
        {
            // TOGGLE OFF: Si ya estaba, deseleccionar
            Deselect(go);
        }
        else
        {
            // TOGGLE ON: Si no estaba, seleccionar
            Select(go);
        }
    }

    private void Select(GameObject go)
    {
        // Límite de 5
        if (selectionOrder.Count >= maxSelections)
        {
            Deselect(selectionOrder[0]); // Quitar el más viejo
        }

        selectionSet.Add(go);
        selectionOrder.Add(go);

        OnSelected?.Invoke(go);
        OnUnhovered?.Invoke(go); // Quitar hover visual al seleccionar
    }

    private void Deselect(GameObject go)
    {
        if (selectionSet.Contains(go))
        {
            selectionSet.Remove(go);
            selectionOrder.Remove(go);

            OnDeselected?.Invoke(go);

            // Si el tooltip estaba anclado, avisar al PinManager
            if (TooltipPinManager.Instance != null)
                TooltipPinManager.Instance.UnpinTooltip(go);
        }
    }

    public void HandleHover(GameObject go) => OnHovered?.Invoke(go);
    public void HandleUnhover(GameObject go) => OnUnhovered?.Invoke(go);

    public bool IsSelected(GameObject go) => selectionSet.Contains(go);

    public void ClearSelection()
    {
        // Hacer copia para evitar errores de modificación de colección durante el loop
        var current = new List<GameObject>(selectionOrder);
        foreach (var g in current) Deselect(g);
    }
}
