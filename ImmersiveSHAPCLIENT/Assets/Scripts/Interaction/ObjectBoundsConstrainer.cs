using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics; // Para la vibración

/// <summary>
/// BLOQUEO ESTRICTO: Solo permite manipulación si AMBAS manos están sujetando el gráfico.
/// Optimizado para Quest 3 y XRI 3.0.8.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class ObjectBoundsConstrainer : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isGraphRendered = false;

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

        if (rb != null)
        {
            rb.isKinematic = true;     // 🔒 Bloquea fuerzas externas
            rb.useGravity = false;      // 🚫 Evita que se caiga si falla el kinematic
            rb.linearDamping = 10f;     // ⚓ Mucha resistencia al movimiento
            rb.angularDamping = 10f;
        }

        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            bc.enabled = false;
            bc.isTrigger = true; // 👻 Lo volvemos fantasma por seguridad
        }
    }


    private void OnEnable()
    {
        // Suscribimos los eventos de vibración y agarre
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.AddListener(OnHoverEnter);
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEnter);
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    // 🚀 SE ACTIVA DESDE PLOT MANAGER
    public void NotifyGraphRendered()
    {
        isGraphRendered = true;
        centerPoint = transform.position;
        CaptureLegalState();

        // Habilitamos el colisionador para poder agarrarlo
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null) bc.enabled = true;
    }

    // 🚀 SE ACTIVA DESDE UI MANAGER (Botón New Plot)
    public void ResetInteractionState()
    {
        isGraphRendered = false;
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null) bc.enabled = false;
    }

    // 📳 VIBRACIÓN: Al poner la mano sobre el gráfico
    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (!isGraphRendered) return;

        // Buscamos si el mando que entró tiene capacidad de vibración
        if (args.interactorObject.transform.TryGetComponent(out HapticImpulsePlayer hapticPlayer))
        {
            // Intensidad 0.4 (40%), Duración 0.1 segundos
            hapticPlayer.SendHapticImpulse(0.4f, 0.1f);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        CaptureLegalState();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // Al soltar, nos aseguramos de que no haya inercia
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (!isGraphRendered) return;

        int grabCount = grabInteractable.interactorsSelecting.Count;

        // MODO VOLANTE: Si hay 2 manos, permitimos transformar
        if (grabCount >= 2)
        {
            ApplyConstraints();
            CaptureLegalState();
        }
        else
        {
            // Si hay 0 o 1 mano, el gráfico se queda clavado en su sitio legal
            transform.position = lastLegalPosition;
            transform.rotation = lastLegalRotation;
            transform.localScale = lastLegalScale;
        }
    }

    private void CaptureLegalState()
    {
        lastLegalPosition = transform.position;
        lastLegalRotation = transform.rotation;
        lastLegalScale = transform.localScale;
    }

    private void ApplyConstraints()
    {
        // Aumentamos el radio de seguridad para que no "salte" 
        // mientras el usuario se mueve por la habitación
        float distance = Vector3.Distance(transform.position, centerPoint);

        // Si la distancia es muy grande, en lugar de teletransportarlo 
        // violentamente, simplemente lo bloqueamos o lo traemos suavemente.
        if (distance > maxDistanceFromCenter)
        {
            // En lugar de snap instantáneo, lo movemos a la frontera suavemente
            Vector3 targetPos = centerPoint + (transform.position - centerPoint).normalized * maxDistanceFromCenter;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
        }

        // Aseguramos que la escala nunca sea 0
        float s = transform.localScale.x;
        if (s < 0.01f) s = 0.5f;
        transform.localScale = Vector3.one * Mathf.Clamp(s, minScale, maxScale);
    }

}
