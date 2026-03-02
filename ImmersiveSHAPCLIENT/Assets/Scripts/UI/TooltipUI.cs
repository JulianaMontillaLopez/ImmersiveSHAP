using UnityEngine;
using TMPro;

/// <summary>
/// UI de un tooltip en World Space.
/// Tamaño fijo definido exclusivamente por el prefab.
/// Seguro para pooling y VR.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class TooltipUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private RectTransform rootRect;
    [SerializeField] private Canvas canvas;

    [Header("Layout")]
    [Tooltip("Offset vertical sobre el punto (metros)")]
    public float worldOffsetY = 0.06f;

    [Range(0f, 1f)]
    public float billboardLerp = 0.5f;

    // Estado
    private Transform target;
    private Transform cam;
    public bool isPinned { get; private set; }

    private void Awake()
    {
        if (canvas == null) canvas = GetComponent<Canvas>();
        if (rootRect == null) rootRect = GetComponent<RectTransform>();
        if (Camera.main != null) cam = Camera.main.transform;

        canvas.renderMode = RenderMode.WorldSpace;
    }

    public void SetContent(string body)
    {
        if (bodyText != null)
            bodyText.text = body ?? string.Empty;
    }

    public void AttachTo(Transform targetTransform)
    {
        target = targetTransform;
        isPinned = false;

        UpdatePositionImmediate();
        gameObject.SetActive(true);
    }

    public void SetPinnedState(bool pinned)
    {
        isPinned = pinned;
    }

    public void Clear()
    {
        target = null;
        isPinned = false;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;

        if (cam == null) return;

        // Seguir al punto SOLO si es hover
        if (target != null && !isPinned)
        {
            transform.position = target.position + Vector3.up * worldOffsetY;
        }

        // Billboarding
        Vector3 dir = transform.position - cam.position;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, billboardLerp);
        }
    }

    private void UpdatePositionImmediate()
    {
        if (target != null)
            transform.position = target.position + Vector3.up * worldOffsetY;
    }
}