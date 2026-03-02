using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye y administra los ejes X, Y, Z a partir de datos escalados.
/// Debe ejecutarse SIEMPRE en main thread ya que usa Unity API.
/// </summary>
public class AxesAndReferenceBuilder : MonoBehaviour
{
    public static AxesAndReferenceBuilder Instance { get; private set; }

    [Header("Prefab del eje (debe contener componente Axis)")]
    [SerializeField] private GameObject axisPrefab;

    [Header("Configuración estética")]
    [SerializeField] private Color axisColor = Color.white;

    [Header("Zero Plane Reference")]
    public GameObject zeroPlanePrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Construye los ejes usando datos ya escalados por DataScalerAndAligner.
    /// Ejecutar SIEMPRE en main thread.
    /// </summary>
    public void BuildAxesFromScaledData(
        Vector3 origin,
        DataScalerAndAligner scaler,
        string xLabel, string yLabel, string zLabel)
    {
        if (axisPrefab == null)
        {
            Debug.LogError("[AxesBuilder] axisPrefab es NULL.");
            return;
        }

        // ✅ Eliminar ejes previos generados
        ClearGeneratedAxes();

        // ✅ Obtener rangos originales y escalados
        var (minOriginal, maxOriginal) = scaler.GetAxisRanges(false);
        var (_, scaledMax) = scaler.GetAxisRanges(true);

        float targetScale = scaledMax.x; // x == y == z en el scaler

        // ✅ Crear cada eje
        CreateAxis(
            "X", Vector3.right, origin,
            0f, targetScale,
            minOriginal.x, maxOriginal.x, xLabel);

        CreateAxis(
            "Y", Vector3.up, origin,
            0f, targetScale,
            minOriginal.y, maxOriginal.y, yLabel);

        CreateAxis(
            "Z", Vector3.forward, origin,
            0f, targetScale,
            minOriginal.z, maxOriginal.z, zLabel);
    }

    private void CreateAxis(
       string name,
       Vector3 direction,
       Vector3 origin,
       float minScaled, float maxScaled,
       float minOriginal, float maxOriginal,
       string label)
    {
        GameObject axisGO = Instantiate(axisPrefab, origin, Quaternion.identity, transform);
        axisGO.name = $"{name}_Axis";
        axisGO.tag = "GeneratedAxis";

        Axis axis = axisGO.GetComponent<Axis>();
        if (axis == null)
        {
            Debug.LogError($"[AxesBuilder] El prefab del eje no contiene componente Axis.");
            return;
        }

        float axisLength = maxScaled - minScaled;

        axis.BuildAxis(
            origin,
            direction,
            axisColor,
            minScaled,
            maxScaled,
            minOriginal,
            maxOriginal,
            label,
            axisLength
        );
    }

    public void BuildZeroPlane(DataScalerAndAligner scaler, float targetScale)
    {
        // 🛡️ SEGURIDAD: Evita que el juego se rompa si no asignaste el prefab
        if (zeroPlanePrefab == null)
        {
            Debug.LogWarning("ImmersiveSHAP, UNITY, ⚠️ [AxesAndReferenceBuilder] zeroPlanePrefab no asignado en el Inspector.");
            return;
        }
        var (minOrig, maxOrig) = scaler.GetAxisRanges(false);

        // Solo creamos el plano si hay valores negativos y positivos
        if (minOrig.y < 0 && maxOrig.y > 0)
        {
            // 1. Calculamos la posición normalizada (0 a 1)
            float totalRange = maxOrig.y - minOrig.y;
            float normalizedZero = (0f - minOrig.y) / totalRange;

            // 2. Calculamos la altura real en el espacio de Unity
            float yPos = normalizedZero * targetScale;

            // 3. Instanciar
            GameObject plane = Instantiate(zeroPlanePrefab, transform);
            plane.name = "SHAP_ZeroPlane";

            // 4. Posicionamiento: Centrado en X y Z (targetScale/2)
            plane.transform.localPosition = new Vector3(targetScale / 2, yPos, targetScale / 2);

            // 5. Escala: El plano debe cubrir toda el área del plot
            plane.transform.localScale = new Vector3(targetScale, targetScale, 1f);

            // 6. Rotación: Los Quads por defecto están verticales, lo giramos 90 grados
            plane.transform.localRotation = Quaternion.Euler(90, 0, 0);

            // 7. Etiqueta: Para que SceneCleaner lo borre automáticamente
            plane.tag = "GeneratedAxis";
        }
    }

    /// <summary>
    /// Limpia todos los ejes generados dinámicamente.
    /// </summary>
    private void ClearGeneratedAxes()
    {
        List<Transform> toDelete = new List<Transform>();

        foreach (Transform child in transform)
        {
            if (child.CompareTag("GeneratedAxis"))
                toDelete.Add(child);
        }

        foreach (var t in toDelete)
            Destroy(t.gameObject);
    }

    /// <summary>
    /// Corrige el mapeo de coordenadas escaladas → originales.
    /// </summary>
    public static float MapScaledToOriginal(
        float scaled, float minOriginal, float maxOriginal, float targetScale)
    {
        float normalized = scaled / targetScale; // [0,1]
        return Mathf.Lerp(minOriginal, maxOriginal, normalized);
    }

    /// <summary>
    /// Desactiva o destruye todos los ejes.
    /// </summary>
    public void ClearAxes(bool destroy = false)
    {
        foreach (Transform child in transform)
        {
            if (destroy)
            {
                Destroy(child.gameObject);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
