using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Prevents the object from being moved too far from the center or scaled too large/small.
/// CORREGIDO: Desactiva "throw on detach" por código y siempre verifica 
/// límites después de soltar para evitar que el gráfico salga despedido.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class ObjectBoundsConstrainer : MonoBehaviour
{
    [Header("Position Limits")]
    public float maxDistanceFromCenter = 3.0f;
    public Vector3 centerPoint = new Vector3(0, 1.2f, 1.5f);

    [Header("Scale Limits")]
    public float minScale = 0.25f;
    public float maxScale = 2.0f;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isBeingGrabbed = false;
    private bool needsPostReleaseCheck = false; // 🚀 Verificar después de soltar
    private int postReleaseFrames = 0;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // 🚀 CORRECCIÓN CRÍTICA: Desactivar inercia por código
        // Esto evita que el gráfico "salga despedido" al soltarlo
        if (grabInteractable != null)
        {
            grabInteractable.throwOnDetach = false;
        }

        // 🚀 CORRECCIÓN: Asegurar que el Rigidbody no aplique física
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isBeingGrabbed = true;
        needsPostReleaseCheck = false;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isBeingGrabbed = grabInteractable.interactorsSelecting.Count > 0;

        if (!isBeingGrabbed)
        {
            // 🚀 Activar verificación post-soltar por 10 frames
            needsPostReleaseCheck = true;
            postReleaseFrames = 10;

            // 🚀 SEGURIDAD: Matar toda velocidad al soltar
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void LateUpdate()
    {
        // Procesar mientras está agarrado
        if (isBeingGrabbed)
        {
            ApplyConstraints();
            return;
        }

        // 🚀 CORRECCIÓN: También verificar por unos frames después de soltar
        // para atrapar cualquier movimiento residual
        if (needsPostReleaseCheck)
        {
            ApplyConstraints();

            // Matar velocidad residual cada frame post-release
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            postReleaseFrames--;
            if (postReleaseFrames <= 0)
                needsPostReleaseCheck = false;
        }
    }

    private void ApplyConstraints()
    {
        // 1. Constrain Position
        float distance = Vector3.Distance(transform.position, centerPoint);
        if (distance > maxDistanceFromCenter)
        {
            Vector3 fromCenter = transform.position - centerPoint;
            fromCenter *= maxDistanceFromCenter / distance;
            transform.position = centerPoint + fromCenter;

            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        // 2. Constrain Scale
        float currentScale = transform.localScale.x;
        if (currentScale < minScale)
        {
            transform.localScale = Vector3.one * minScale;
        }
        else if (currentScale > maxScale)
        {
            transform.localScale = Vector3.one * maxScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerPoint, maxDistanceFromCenter);
    }
}
