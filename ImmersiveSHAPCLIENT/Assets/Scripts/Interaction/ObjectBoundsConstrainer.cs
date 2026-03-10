using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class ObjectBoundsConstrainer : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isGraphRendered = false; // Seguro para el inicio

    [Header("Visual Feedback")]
    public GameObject interactionVisual;

    [Header("Constraints")]
    public float maxDistanceFromCenter = 5.0f;
    public Vector3 centerPoint;
    public float minScale = 0.25f;
    public float maxScale = 2.0f;

    private Vector3 lastLegalPosition;
    private Quaternion lastLegalRotation;
    private Vector3 lastLegalScale;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        // El visual siempre apagado al despertar
        if (interactionVisual != null) interactionVisual.SetActive(false);
    }

    private void OnEnable()
    {
        // Solo escuchamos el agarre (Select), ignoramos el Hover.
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    // 🚀 MÉTODO PARA LLAMAR DESDE TU PLOT MANAGER
    // Llama a esto cuando el gráfico termine de dibujarse.
    public void NotifyGraphRendered()
    {
        isGraphRendered = true;
        centerPoint = transform.position; // Sincroniza el centro aquí
        CaptureLegalState();
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Solo mostramos la jaula si el gráfico ya existe y se presiona Grip
        if (interactionVisual != null && isGraphRendered)
            interactionVisual.SetActive(true);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (interactionVisual != null)
            interactionVisual.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!isGraphRendered) return;

        int grabCount = grabInteractable.interactorsSelecting.Count;

        if (grabCount >= 2)
        {
            ApplyConstraints();
            CaptureLegalState();
        }
        else
        {
            transform.position = lastLegalPosition;
            transform.rotation = lastLegalRotation;
            transform.localScale = lastLegalScale;
        }

        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
    }

    private void CaptureLegalState()
    {
        lastLegalPosition = transform.position;
        lastLegalRotation = transform.rotation;
        lastLegalScale = transform.localScale;
    }

    private void ApplyConstraints()
    {
        float distance = Vector3.Distance(transform.position, centerPoint);
        if (distance > maxDistanceFromCenter)
            transform.position = centerPoint + (transform.position - centerPoint).normalized * maxDistanceFromCenter;

        float s = transform.localScale.x;
        transform.localScale = Vector3.one * Mathf.Clamp(s, minScale, maxScale);
    }

    public void ResetInteractionState()
    {
        isGraphRendered = false;
        if (interactionVisual != null) interactionVisual.SetActive(false);
    }

}
