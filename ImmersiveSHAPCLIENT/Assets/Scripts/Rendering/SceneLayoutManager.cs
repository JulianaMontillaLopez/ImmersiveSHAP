using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the physical layout and placement of the SHAP plot in the VR space.
/// Ensures the graph is oriented as a "Diorama" facing the user.
/// OPTIMIZADO: No recalcula bounds innecesariamente.
/// </summary>
[DisallowMultipleComponent]
public class SceneLayoutManager : MonoBehaviour
{
    [Header("Layout Parameters")]
    [Tooltip("Forward distance from the camera (meters) where the plot will be placed.")]
    public float targetDistanceFromCamera = 1.5f;

    [Header("References (auto-assign if null)")]
    public Transform xrCameraTransform;
    public Transform plotRoot;

    [Header("Lighting")]
    public Light directionalLightPrefab;
    public float baseLightIntensity = 1.0f;
    public Color baseLightColor = Color.white;
    [HideInInspector] public Light createdDirectionalLight;

    private BoundsOptimizer boundsOptimizer;

    private void Awake()
    {
        // Auto-assign Camera if missing
        if (xrCameraTransform == null && Camera.main != null)
            xrCameraTransform = Camera.main.transform;

        // Ensure BoundsOptimizer is linked
        boundsOptimizer = GetComponent<BoundsOptimizer>();
        if (boundsOptimizer == null)
            boundsOptimizer = gameObject.AddComponent<BoundsOptimizer>();
    }

    public void ResetScene()
    {
        if (createdDirectionalLight != null)
        {
            Destroy(createdDirectionalLight.gameObject);
            createdDirectionalLight = null;
        }
    }

    /// <summary>
    /// Positions the plot in front of the camera and aligns its orientation 
    /// as a holographic display (Y-Up, X-Horizontal).
    /// OPTIMIZADO: Solo recalcula bounds cuando es necesario.
    /// </summary>
    public void PositionPlot(Transform plotRoot)
    {
        if (xrCameraTransform == null || plotRoot == null) return;

        // 1. Reset scale to base
        plotRoot.localScale = Vector3.one;
        // 1. Calculamos el "Forward" nivelado (ignora si miras arriba/abajo)
        Vector3 forwardLevel = xrCameraTransform.forward;
        forwardLevel.y = 0;
        forwardLevel.Normalize();
        // 2. Rotación: Siempre mirando al usuario pero nivelada con el suelo
        plotRoot.rotation = Quaternion.LookRotation(forwardLevel, Vector3.up);
        // 3. Posición fija a la altura del pecho (ej. 1.2 metros sobre el suelo)
        // En lugar de seguir la altura de la cámara, usamos una altura fija ergonómica
        Vector3 targetPos = xrCameraTransform.position + forwardLevel * targetDistanceFromCamera;
        targetPos.y = 1.2f; // <--- Bloqueamos la altura para que sea siempre cómoda

        // 4. ALIGNMENT & BOUNDS: Center the visual volume at the targetPos
        Bounds currentBounds = new Bounds(targetPos, Vector3.zero);

        if (boundsOptimizer != null)
        {
            // 🚀 OPTIMIZACIÓN: Marcar como dirty solo la primera vez
            boundsOptimizer.MarkDirty();
            boundsOptimizer.RefreshCache(); // Crucial: recalculate based on new points
            if (boundsOptimizer.GetBounds(out Bounds bounds))
            {
                currentBounds = bounds;
                Vector3 offset = targetPos - bounds.center;
                plotRoot.position += offset;
            }
            else
            {
                plotRoot.position = targetPos;
            }
        }

        // 5. LIGHTING: Dynamic adjustment based on graph size (now using correct scope)
        float sceneScale = currentBounds.extents.magnitude * 2f;
        EnsureDirectionalLight(sceneScale);

        Debug.Log($"ImmersiveSHAP, UNITY, [SceneLayoutManager] Diorama view initialized at {targetDistanceFromCamera}m");
    }

    private void EnsureDirectionalLight(float sceneScale)
    {
        if (createdDirectionalLight == null)
        {
            if (directionalLightPrefab != null)
            {
                createdDirectionalLight = Instantiate(directionalLightPrefab, this.transform);
            }
            else
            {
                var go = new GameObject("SceneLayout_DirectionalLight");
                go.transform.SetParent(this.transform, false);
                createdDirectionalLight = go.AddComponent<Light>();
                createdDirectionalLight.type = LightType.Directional;
            }
            createdDirectionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        createdDirectionalLight.color = baseLightColor;
        // Intensity scales with plot size for better visibility
        createdDirectionalLight.intensity = Mathf.Clamp(baseLightIntensity * (sceneScale * 0.75f), 0.5f, 4f);
        createdDirectionalLight.shadows = LightShadows.Soft;
    }

    private void OnDisable()
    {
        ResetScene();
    }
}
