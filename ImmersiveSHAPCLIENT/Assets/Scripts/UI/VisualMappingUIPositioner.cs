using UnityEngine;

/// <summary>
/// Positions the Main UI in VR space. 
/// Ensures the menu is fixed (Static) for maximum comfort, 
/// but snaps to the user whenever it is enabled.
/// </summary>
[DisallowMultipleComponent]
public class VisualMappingUIPositioner : MonoBehaviour
{
    public enum FollowMode { Static, LazyFollow, AlwaysActive }

    [Header("Behavior")]
    [Tooltip("Static: Se posiciona una vez al activarse y se queda fijo.\nLazyFollow: Te sigue solo si miras muy lejos.\nAlwaysActive: Te sigue constantemente (Billboard).")]
    public FollowMode followMode = FollowMode.Static;

    [Header("References")]
    [Tooltip("El transform del centro de la cámara/HMD.")]
    public Transform xrCamera;

    [Header("Ergonomics")]
    [Tooltip("Distancia ideal para leer e interactuar (metros).")]
    public float distance = 1.3f;
    [Tooltip("Desplazamiento vertical respecto a los ojos (ej. -0.3f para altura del pecho).")]
    public float verticalOffset = -0.5f;

    [Header("Lazy Follow (Optional)")]
    public float angleThreshold = 45f;
    public float smoothTime = 0.2f;

    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        // Auto-asignación de cámara si no existe
        if (xrCamera == null && Camera.main != null)
            xrCamera = Camera.main.transform;
    }

    private void OnEnable()
    {
        // CRUCIAL: Cada vez que el menú se activa (inicio o tras un Cancel), 
        // lo traemos de nuevo frente al usuario para que no se pierda.
        RepositionNow();
    }

    private void LateUpdate()
    {
        if (xrCamera == null) return;

        // Si es estático, no hacemos nada más después del OnEnable
        if (followMode == FollowMode.Static) return;

        bool shouldMove = (followMode == FollowMode.AlwaysActive) || NeedsReposition();

        if (shouldMove)
        {
            ApplyPlacement(useSmoothing: true);
        }
    }

    private bool NeedsReposition()
    {
        if (followMode != FollowMode.LazyFollow) return false;

        // Calculamos el ángulo entre donde estoy yo y hacia donde mira el usuario
        Vector3 dirToPanel = (transform.position - xrCamera.position).normalized;
        float angle = Vector3.Angle(xrCamera.forward, dirToPanel);
        return angle > angleThreshold;
    }

    /// <summary>
    /// Forza el panel a aparecer justo frente al usuario ahora mismo.
    /// </summary>
    public void RepositionNow()
    {
        if (xrCamera == null && Camera.main != null) xrCamera = Camera.main.transform;
        if (xrCamera == null) return;

        ApplyPlacement(useSmoothing: false);
    }

    private void ApplyPlacement(bool useSmoothing)
    {
        // 1. OBTENER ROTACIÓN HORIZONTAL (YAW)
        // Usamos solo el eje Y de la cámara para que el panel esté 
        // siempre perfectamente vertical y sea cómodo de leer.
        float yaw = xrCamera.eulerAngles.y;
        Quaternion horizontalRotation = Quaternion.Euler(0, yaw, 0);

        // 2. POSICIÓN OBJETIVO
        Vector3 forwardFlat = horizontalRotation * Vector3.forward;
        Vector3 targetPos = xrCamera.position + (forwardFlat * distance);
        targetPos.y += verticalOffset; // Bajamos el panel a la altura del pecho

        // 3. APLICAR TRANSFORM
        if (useSmoothing)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, horizontalRotation, Time.deltaTime * 5f);
        }
        else
        {
            transform.position = targetPos;
            transform.rotation = horizontalRotation;
            velocity = Vector3.zero;
        }
    }
}
