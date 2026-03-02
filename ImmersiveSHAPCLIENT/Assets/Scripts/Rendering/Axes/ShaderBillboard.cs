using UnityEngine;

public class ShaderBillboard : MonoBehaviour
{
    [Header("Billboard Settings")]
    public bool billboardX = true;
    public bool billboardY = true;
    public bool billboardZ = true;

    private Material billboardMaterial;
    private Transform mainCamera;

    private void Start()
    {
        mainCamera = Camera.main.transform;

        // Configurar material con shader de billboarding
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            billboardMaterial = renderer.material;
            billboardMaterial.EnableKeyword("_BILLBOARD_ON");
        }
    }

    private void Update()
    {
        if (mainCamera == null || billboardMaterial == null)
            return;

        // Actualizar posición de cámara en shader
        billboardMaterial.SetVector("_CameraPosition", mainCamera.position);

        // Billboard manual opcional
        if (!billboardMaterial.IsKeywordEnabled("_BILLBOARD_ON"))
        {
            Vector3 targetPosition = transform.position + mainCamera.forward;
            transform.LookAt(targetPosition, Vector3.up);
        }
    }

    private void OnDestroy()
    {
        if (billboardMaterial != null)
            Destroy(billboardMaterial);
    }
}