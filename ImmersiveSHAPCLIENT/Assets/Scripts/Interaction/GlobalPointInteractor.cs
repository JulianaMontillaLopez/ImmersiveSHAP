using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

// 🚀 CAMBIO: Quitamos el [RequireComponent(typeof(XRRayInteractor))] 
// para que Unity nos deje agregarlo al Near-Far Interactor.
public class GlobalPointInteractor : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionProperty selectPointAction;
    public InputActionProperty clearSelectionAction;
    public InputActionProperty zoomToTooltipAction;

    [Header("Zoom Config")]
    public float zoomDistance = 0.25f;
    public float zoomSpeed = 3.0f;
    public Transform xrOriginTransform;

    // 🚀 CAMBIO: Usamos el tipo base 'XRBaseInteractor' para mayor compatibilidad
    private XRBaseInteractor interactor;
    private HapticImpulsePlayer hapticPlayer;
    private GameObject currentHoveredPoint;
    private bool isZooming = false;
    private Vector3 zoomTargetPos;
    private Vector3 originalOriginPos;

    private void Awake()
    {
        // Buscamos cualquier interactor que esté en este objeto (Ray, Direct o NearFar)
        interactor = GetComponent<XRBaseInteractor>();

        hapticPlayer = GetComponent<HapticImpulsePlayer>();
        if (hapticPlayer == null) hapticPlayer = gameObject.AddComponent<HapticImpulsePlayer>();

        if (xrOriginTransform == null)
        {
            XROrigin origin = FindFirstObjectByType<XROrigin>();
            if (origin != null) xrOriginTransform = origin.transform;
        }

        if (xrOriginTransform != null) originalOriginPos = xrOriginTransform.position;
    }

    private void OnEnable()
    {
        if (interactor != null)
        {
            interactor.hoverEntered.AddListener(OnHoverEnter);
            interactor.hoverExited.AddListener(OnHoverExit);
        }

        selectPointAction.action?.Enable();
        clearSelectionAction.action?.Enable();
        zoomToTooltipAction.action?.Enable();
    }

    private void OnDisable()
    {
        if (interactor != null)
        {
            interactor.hoverEntered.RemoveListener(OnHoverEnter);
            interactor.hoverExited.RemoveListener(OnHoverExit);
        }

        selectPointAction.action?.Disable();
        clearSelectionAction.action?.Disable();
        zoomToTooltipAction.action?.Disable();
    }

    private void Update()
    {
        HandleInputLogic();
        if (isZooming && xrOriginTransform != null) ExecuteZoomMovement();
    }

    private void HandleInputLogic()
    {
        if (selectPointAction.action != null && selectPointAction.action.WasPressedThisFrame())
        {
            if (currentHoveredPoint != null)
            {
                PointSelection.Instance?.HandleSelect(currentHoveredPoint);
                if (hapticPlayer != null) hapticPlayer.SendHapticImpulse(0.5f, 0.1f);
            }
        }

        if (clearSelectionAction.action != null && clearSelectionAction.action.WasPressedThisFrame())
        {
            PointSelection.Instance?.ClearSelection();
            if (xrOriginTransform != null) xrOriginTransform.position = originalOriginPos;
            isZooming = false;
        }

        if (zoomToTooltipAction.action != null && zoomToTooltipAction.action.WasPressedThisFrame())
        {
            PrepareZoom();
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        GameObject go = args.interactableObject.transform.gameObject;
        if (go.GetComponent<DataPointMeta>() != null)
        {
            currentHoveredPoint = go;
            PointSelection.Instance?.HandleHover(go);
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (currentHoveredPoint == args.interactableObject.transform.gameObject)
        {
            PointSelection.Instance?.HandleUnhover(currentHoveredPoint);
            currentHoveredPoint = null;
        }
    }

    private void PrepareZoom()
    {
        GameObject target = currentHoveredPoint;
        if (target == null && TooltipPinManager.Instance != null)
            target = TooltipPinManager.Instance.GetLastPinnedPoint();

        if (target != null && xrOriginTransform != null)
        {
            originalOriginPos = xrOriginTransform.position;
            Vector3 pointPos = target.transform.position;
            Vector3 directionToCamera = (Camera.main.transform.position - pointPos).normalized;
            zoomTargetPos = pointPos + directionToCamera * zoomDistance;
            isZooming = true;
        }
    }

    private void ExecuteZoomMovement()
    {
        xrOriginTransform.position = Vector3.Lerp(xrOriginTransform.position, zoomTargetPos, Time.deltaTime * zoomSpeed);
        if (Vector3.Distance(xrOriginTransform.position, zoomTargetPos) < 0.01f) isZooming = false;
    }
}
