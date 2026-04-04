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
    private Vector3 zoomTargetPos;
    private Vector3 originalOriginPos;

    // Añadimos dos booleanos para controlar los estados
    private bool isZoomedIn = false;
    private bool isMoving = false;


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
        // Usamos nuestro nuevo controlador booleano
        if (isMoving && xrOriginTransform != null) ExecuteZoomMovement();
    }

    private void HandleInputLogic()
    {
        // 1. Botón de Seleccionar Punto (Gatillo)
        if (selectPointAction.action != null && selectPointAction.action.WasPressedThisFrame())
        {
            if (currentHoveredPoint != null)
            {
                PointSelection.Instance?.HandleSelect(currentHoveredPoint);
                if (hapticPlayer != null) hapticPlayer.SendHapticImpulse(0.5f, 0.1f);
            }
        }

        // 2. Botón de Limpiar Selección (Gatillo al aire libre u otro)
        if (clearSelectionAction.action != null && clearSelectionAction.action.WasPressedThisFrame())
        {
            PointSelection.Instance?.ClearSelection();
            if (isZoomedIn)
            {
                // Ordenamos volver a casa suavemente si limpiamos
                zoomTargetPos = originalOriginPos;
                isZoomedIn = false;
                isMoving = true;
            }
        }

        // 3. Botón "Y/B" de ACERCAR/ALEJAR (Toggle)
        if (zoomToTooltipAction.action != null && zoomToTooltipAction.action.WasPressedThisFrame())
        {
            ToggleZoom();
        }
    }

    // -- LA NUEVA LÓGICA DE TOGGLE --
    private void ToggleZoom()
    {
        // Si no estamos cerca, Acercar
        if (!isZoomedIn)
        {
            GameObject target = currentHoveredPoint;
            if (target == null && TooltipPinManager.Instance != null)
                target = TooltipPinManager.Instance.GetLastPinnedPoint();

            if (target != null && xrOriginTransform != null)
            {
                // CRUCIAL: Solo guardamos la posición original CUANDO NO ESTAMOS DENTRO.
                // Así evitamos sobreescribir la memoria y quedar atrapados.
                originalOriginPos = xrOriginTransform.position;

                Vector3 pointPos = target.transform.position;
                Vector3 directionToCamera = (Camera.main.transform.position - pointPos).normalized;
                zoomTargetPos = pointPos + directionToCamera * zoomDistance;

                isZoomedIn = true;
                isMoving = true;
            }
        }
        else // Si ya estamos cerca, Alejar (Volver a posición original)
        {
            zoomTargetPos = originalOriginPos;
            isZoomedIn = false;
            isMoving = true;
        }
    }

    private void ExecuteZoomMovement()
    {
        // Interpola la posición suavemente
        xrOriginTransform.position = Vector3.Lerp(xrOriginTransform.position, zoomTargetPos, Time.deltaTime * zoomSpeed);

        // Cuando ya está prácticamente en el destino, frenamos y ajustamos perfecto
        if (Vector3.Distance(xrOriginTransform.position, zoomTargetPos) < 0.01f)
        {
            xrOriginTransform.position = zoomTargetPos; // Snap de precisión final
            isMoving = false; // Paramos el motor
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
}
