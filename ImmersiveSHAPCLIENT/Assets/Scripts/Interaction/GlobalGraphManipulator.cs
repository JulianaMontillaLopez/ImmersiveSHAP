using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permite rotar, mover y hacer zoom de manera Bimanual (Volante)
/// SIN necesidad de apuntar con los rayos. Basta con apretar ambos Grips en cualquier lugar.
/// </summary>
public class GlobalGraphManipulator : MonoBehaviour
{
    [Header("Mapeo de Botones (Obligatorio)")]
    [Tooltip("Asigna la acción Select del mando Izquierdo")]
    public InputActionReference leftGripAction;
    [Tooltip("Asigna la acción Select del mando Derecho")]
    public InputActionReference rightGripAction;

    [Header("Posición de las Manos (Obligatorio)")]
    public Transform leftController;
    public Transform rightController;

    [Header("Límites de Escala (Zoom)")]
    public float minScale = 0.1f;
    public float maxScale = 5.0f;

    private bool isManipulating = false;

    // Variables de anclaje inicial
    private Vector3 initialGraphPosition;
    private Quaternion initialGraphRotation;
    private Vector3 initialGraphScale;

    private Vector3 initialHandsMidpoint;
    private float initialHandsDistance;
    private Vector3 initialHandsDirection;

    private void OnEnable()
    {
        if (leftGripAction != null) leftGripAction.action.Enable();
        if (rightGripAction != null) rightGripAction.action.Enable();
    }

    private void OnDisable()
    {
        if (leftGripAction != null) leftGripAction.action.Disable();
        if (rightGripAction != null) rightGripAction.action.Disable();
    }

    private void Update()
    {
        if (leftGripAction == null || rightGripAction == null || leftController == null || rightController == null)
            return;

        // Detectar botones presionados (Sensibilidad > 0.5 para evitar toques falsos)
        bool leftPressed = leftGripAction.action.ReadValue<float>() > 0.5f;
        bool rightPressed = rightGripAction.action.ReadValue<float>() > 0.5f;

        if (leftPressed && rightPressed)
        {
            if (!isManipulating)
                StartManipulation(); // El usuario acaba de apretar ambos
            else
                UpdateManipulation(); // El usuario mantiene apretado y está moviendo
        }
        else
        {
            if (isManipulating)
                isManipulating = false; // El usuario soltó uno o ambos botones
        }
    }

    private void StartManipulation()
    {
        isManipulating = true;

        // 1. Guardamos cómo estaba el gráfico en este momento
        initialGraphPosition = transform.position;
        initialGraphRotation = transform.rotation;
        initialGraphScale = transform.localScale;

        // 2. Guardamos la postura empírica de las manos del usuario
        initialHandsMidpoint = (leftController.position + rightController.position) / 2f;
        initialHandsDistance = Vector3.Distance(leftController.position, rightController.position);
        initialHandsDirection = (rightController.position - leftController.position).normalized;

        if (initialHandsDistance < 0.01f) initialHandsDistance = 0.01f; // Prevenir división por cero
    }

    private void UpdateManipulation()
    {
        // Calcular la postura ACTUAL de las manos
        Vector3 currentHandsMidpoint = (leftController.position + rightController.position) / 2f;
        float currentHandsDistance = Vector3.Distance(leftController.position, rightController.position);
        Vector3 currentHandsDirection = (rightController.position - leftController.position).normalized;

        // ---------- A. CÁLCULO DE ESCALA (ZOOM IN/OUT) ----------
        // Cuánto han separado los brazos respecto a la postura inicial
        float scaleFactor = currentHandsDistance / initialHandsDistance;
        float newScale = initialGraphScale.x * scaleFactor;
        newScale = Mathf.Clamp(newScale, minScale, maxScale); // Respetamos tus límites

        float actualScaleFactor = newScale / initialGraphScale.x;
        transform.localScale = Vector3.one * newScale;

        // ---------- B. CÁLCULO DE ROTACIÓN (VOLANTE) ----------
        // Calculamos qué ángulo hay entre la mano Izq-Der inicial y la Izq-Der actual
        Quaternion rotationDelta = Quaternion.FromToRotation(initialHandsDirection, currentHandsDirection);
        transform.rotation = rotationDelta * initialGraphRotation;

        // ---------- C. CÁLCULO DE POSICIÓN ----------
        // Aplicamos el pivote excéntrico: el gráfico se mueve siguiendo el centro matemático entre tus dos manos
        Vector3 offsetFromMidpoint = initialGraphPosition - initialHandsMidpoint;
        Vector3 rotatedScaledOffset = rotationDelta * (offsetFromMidpoint * actualScaleFactor);

        transform.position = currentHandsMidpoint + rotatedScaledOffset;
    }
}
