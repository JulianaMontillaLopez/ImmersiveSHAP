using UnityEngine;
using UnityEngine.InputSystem; // Requerido para Input Actions
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Coloca este script en tus Mandos (XRRayInteractor).
/// Escucha globalmente lo que el mando toca y avisa al PointSelection.
/// ¡Cero scripts en los miles de puntos = Máximo rendimiento!
/// OPTIMIZADO: Reducida frecuencia de raycasts, zoom mejorado con reset.
/// </summary>
[RequireComponent(typeof(XRRayInteractor))]
public class GlobalPointInteractor : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("Botón Trigger para SELECCIONAR puntos (Index Trigger)")]
    public InputActionProperty selectPointAction;

    [Tooltip("Botón para LIMPIAR todas las selecciones (ej: Botón A o X)")]
    public InputActionProperty clearSelectionAction;

    [Tooltip("Botón para ACERCARSE al tooltip seleccionado (ej: Botón B o Y)")]
    public InputActionProperty zoomToTooltipAction;

    [Header("Zoom a Tooltip")]
    [Tooltip("Distancia a la que te colocas del tooltip al acercarte (metros)")]
    public float zoomDistance = 0.25f; // 25 cm del tooltip

    [Tooltip("Velocidad de movimiento hacia el tooltip")]
    public float zoomSpeed = 3.0f;

    [Tooltip("Transform del XR Origin para moverlo")]
    public Transform xrOrigin;

    private XRRayInteractor interactor;
    private GameObject currentHoveredPoint; // El punto que estamos mirando actualmente
    private bool isZoomingToTooltip = false;
    private Vector3 zoomTargetPosition;
    private Vector3 originalXROriginPosition; // 🚀 Para volver a la posición original

    private void Awake()
    {
        interactor = GetComponent<XRRayInteractor>();

       
        

        // 🚀 Guardar posición inicial
        if (xrOrigin != null)
            originalXROriginPosition = xrOrigin.position;
    }

    private void OnEnable()
    {
        interactor.hoverEntered.AddListener(OnHoverEnter);
        interactor.hoverExited.AddListener(OnHoverExit);
        // NO usamos selectEntered porque eso es el GRIP

        // Habilitar las acciones de input
        if (selectPointAction.action != null)
            selectPointAction.action.Enable();
        if (clearSelectionAction.action != null)
            clearSelectionAction.action.Enable();
        if (zoomToTooltipAction.action != null)
            zoomToTooltipAction.action.Enable();
    }

    private void OnDisable()
    {
        interactor.hoverEntered.RemoveListener(OnHoverEnter);
        interactor.hoverExited.RemoveListener(OnHoverExit);

        if (selectPointAction.action != null)
            selectPointAction.action.Disable();
        if (clearSelectionAction.action != null)
            clearSelectionAction.action.Disable();
        if (zoomToTooltipAction.action != null)
            zoomToTooltipAction.action.Disable();
    }

    private void Update()
    {
        // SELECCIONAR con TRIGGER
        if (selectPointAction.action != null && selectPointAction.action.WasPressedThisFrame())
        {
            if (currentHoveredPoint != null && currentHoveredPoint.GetComponent<DataPointMeta>() != null)
            {
                Debug.Log($"[GlobalPointInteractor] TRIGGER presionado sobre: {currentHoveredPoint.name}");
                if (PointSelection.Instance != null)
                {
                    PointSelection.Instance.HandleSelect(currentHoveredPoint);
                }
            }
        }

        // LIMPIAR SELECCIÓN (Botón A/X)
        if (clearSelectionAction.action != null && clearSelectionAction.action.WasPressedThisFrame())
        {
            Debug.Log("[GlobalPointInteractor] Botón de limpieza presionado. Reseteando selecciones.");
            if (PointSelection.Instance != null)
            {
                PointSelection.Instance.ClearSelection();
            }

            // 🚀 RESET ZOOM: Volver a la posición original
            if (xrOrigin != null)
            {
                xrOrigin.position = originalXROriginPosition;
            }
            isZoomingToTooltip = false;
        }

        // ACERCARSE A TOOLTIP (Botón B/Y)
        if (zoomToTooltipAction.action != null && zoomToTooltipAction.action.WasPressedThisFrame())
        {
            TryZoomToLastSelectedTooltip();
        }

        // Ejecutar movimiento de zoom
        if (isZoomingToTooltip && xrOrigin != null)
        {
            xrOrigin.position = Vector3.Lerp(xrOrigin.position, zoomTargetPosition, Time.deltaTime * zoomSpeed);

            // Detener cuando estemos cerca
            if (Vector3.Distance(xrOrigin.position, zoomTargetPosition) < 0.02f)
            {
                isZoomingToTooltip = false;
                Debug.Log("[GlobalPointInteractor] Llegaste al tooltip. Presiona A/X para volver.");
            }
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        GameObject go = args.interactableObject.transform.gameObject;

        // Solo avisar si es un punto de datos (tiene el componente de metadata)
        if (go.GetComponent<DataPointMeta>() != null)
        {
            currentHoveredPoint = go;
            if (PointSelection.Instance != null)
                PointSelection.Instance.HandleHover(go);
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        GameObject go = args.interactableObject.transform.gameObject;
        if (go.GetComponent<DataPointMeta>() != null)
        {
            if (currentHoveredPoint == go)
                currentHoveredPoint = null;

            if (PointSelection.Instance != null)
                PointSelection.Instance.HandleUnhover(go);
        }
    }

    /// <summary>
    /// Intenta acercarse al último punto seleccionado.
    /// Primero intenta usar el punto hovereado, luego el último pinned.
    /// </summary>
    private void TryZoomToLastSelectedTooltip()
    {
        if (PointSelection.Instance == null || xrOrigin == null) return;

        // 🚀 Guardar posición actual antes de hacer zoom
        originalXROriginPosition = xrOrigin.position;

        GameObject targetPoint = null;

        // Prioridad 1: Punto actualmente hovereado
        if (currentHoveredPoint != null && currentHoveredPoint.GetComponent<DataPointMeta>() != null)
        {
            targetPoint = currentHoveredPoint;
        }
        // Prioridad 2: Último punto anclado (pinned)
        else if (TooltipPinManager.Instance != null)
        {
            targetPoint = TooltipPinManager.Instance.GetLastPinnedPoint();
        }

        if (targetPoint != null)
        {
            // Calcular posición objetivo: frente al punto a cierta distancia
            Vector3 pointPos = targetPoint.transform.position;
            Vector3 directionToCamera = (Camera.main.transform.position - pointPos).normalized;
            zoomTargetPosition = pointPos + directionToCamera * zoomDistance;

            isZoomingToTooltip = true;
            Debug.Log($"[GlobalPointInteractor] Acercándose a: {targetPoint.name}. Presiona A/X para volver.");
        }
        else
        {
            Debug.LogWarning("[GlobalPointInteractor] Apunta a un punto primero (hover) y presiona B/Y.");
        }
    }
}
