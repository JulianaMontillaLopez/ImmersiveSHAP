using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

/// <summary>
/// BLOQUEO ESTRICTO: Solo permite manipulación si AMBAS manos están sujetando el gráfico.
/// Optimizado para Quest 3 y XRI 3.08.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(XRGeneralGrabTransformer))]
public class ObjectBoundsConstrainer : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    [Header("Constraints")]
    public float maxDistanceFromCenter = 3.0f;
    public Vector3 centerPoint = new Vector3(0, 1.2f, 1.5f);
    public float minScale = 0.25f;
    public float maxScale = 2.0f;

    // Variables de control de estado
    private bool needsPostReleaseCheck = false;
    private int postReleaseFrames = 0;

    // Memoria para revertir transformaciones ilegales (1 sola mano)
    private Vector3 lastLegalPosition;
    private Quaternion lastLegalRotation;
    private Vector3 lastLegalScale;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // Configuración inicial de XRI
        if (grabInteractable != null)
        {
            grabInteractable.selectMode = InteractableSelectMode.Multiple; // Vital para 2 manos
            grabInteractable.throwOnDetach = false; // Evita que salga volando al soltar
        }

        // Configuración de físicas
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Inicializar memoria de transformación
        CaptureLegalState();
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
            grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
            grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // Al soltar completamente, activamos una limpieza de velocidad por 10 frames
        if (grabInteractable.interactorsSelecting.Count == 0)
        {
            needsPostReleaseCheck = true;
            postReleaseFrames = 10;
            ResetVelocity();
        }
    }

    private void LateUpdate()
    {
        int grabCount = grabInteractable.interactorsSelecting.Count;

        // 🛑 CASO 1: BLOQUEO (1 Sola mano)
        // Si detectamos que solo hay un controlador agarrando, forzamos la posición anterior.
        if (grabCount == 1)
        {
            transform.position = lastLegalPosition;
            transform.rotation = lastLegalRotation;
            transform.localScale = lastLegalScale;
            ResetVelocity();
            return;
        }

        // 🟢 CASO 2: MOVIMIENTO PERMITIDO (2 Manos o ninguna)
        // Si hay 2+ manos o el objeto está libre (aplicando límites)
        if (grabCount >= 2 || grabCount == 0 || needsPostReleaseCheck)
        {
            ApplyConstraints();
            CaptureLegalState();

            if (needsPostReleaseCheck)
            {
                ResetVelocity();
                if (--postReleaseFrames <= 0) needsPostReleaseCheck = false;
            }
        }
    }

    private void CaptureLegalState()
    {
        lastLegalPosition = transform.position;
        lastLegalRotation = transform.rotation;
        lastLegalScale = transform.localScale;
    }

    private void ResetVelocity()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void ApplyConstraints()
    {
        // 1. Limitar Posición (Esfera de contención)
        float distance = Vector3.Distance(transform.position, centerPoint);
        if (distance > maxDistanceFromCenter)
        {
            transform.position = centerPoint + (transform.position - centerPoint).normalized * maxDistanceFromCenter;
        }

        // 2. Limitar Escala (Uniforme)
        float s = transform.localScale.x;
        if (s < minScale || s > maxScale)
        {
            transform.localScale = Vector3.one * Mathf.Clamp(s, minScale, maxScale);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerPoint, maxDistanceFromCenter);
    }
}
