using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye y administra los ejes X, Y, Z a partir de datos escalados.
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

    public void BuildAxesFromScaledData(
        Vector3 origin,
        DataScalerAndAligner scaler,
        string xLabel, string yLabel, string zLabel)
    {
        if (axisPrefab == null) { Debug.LogError("[AxesBuilder] axisPrefab es NULL."); return; }

        // ✅ RECTIFICADO: Llamamos a ClearAxes(true) internamente para asegurar limpieza total
        ClearGeneratedAxes();

        var (minOriginal, maxOriginal) = scaler.GetAxisRanges(false);
        var (_, scaledMax) = scaler.GetAxisRanges(true);
        float targetScale = scaledMax.x;

        CreateAxis("X", Vector3.right, origin, 0f, targetScale, minOriginal.x, maxOriginal.x, xLabel);
        CreateAxis("Y", Vector3.up, origin, 0f, targetScale, minOriginal.y, maxOriginal.y, yLabel);
        CreateAxis("Z", Vector3.forward, origin, 0f, targetScale, minOriginal.z, maxOriginal.z, zLabel);
    }

    private void CreateAxis(string name, Vector3 direction, Vector3 origin, float minScaled, float maxScaled, float minOriginal, float maxOriginal, string label)
    {
        GameObject axisGO = Instantiate(axisPrefab, origin, Quaternion.identity, transform);
        axisGO.name = $"{name}_Axis";
        axisGO.tag = "GeneratedAxis"; // Crucial para SceneCleaner

        Axis axis = axisGO.GetComponent<Axis>();
        if (axis != null)
        {
            float axisLength = maxScaled - minScaled;
            axis.BuildAxis(origin, direction, axisColor, minScaled, maxScaled, minOriginal, maxOriginal, label, axisLength);
        }
    }

    public void BuildZeroPlane(DataScalerAndAligner scaler, float targetScale)
    {
        if (zeroPlanePrefab == null) return;

        var (minOrig, maxOrig) = scaler.GetAxisRanges(false);

        if (minOrig.y < 0 && maxOrig.y > 0)
        {
            float totalRange = maxOrig.y - minOrig.y;
            float normalizedZero = (0f - minOrig.y) / totalRange;
            float yPos = normalizedZero * targetScale;

            GameObject plane = Instantiate(zeroPlanePrefab, transform);
            plane.name = "SHAP_ZeroPlane";
            plane.tag = "GeneratedAxis"; // ✅ CORRECCIÓN: Etiquetado para limpieza

            plane.transform.localPosition = new Vector3(targetScale / 2, yPos, targetScale / 2);
            plane.transform.localScale = new Vector3(targetScale, targetScale, 1f);
            plane.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
    }

    private void ClearGeneratedAxes()
    {
        // Limpieza rápida de cualquier cosa con el tag
        GameObject[] toDelete = GameObject.FindGameObjectsWithTag("GeneratedAxis");
        foreach (var obj in toDelete)
        {
            // Solo borrar si es hijo de este objeto para no borrar ejes de otros posibles sistemas
            if (obj.transform.parent == this.transform)
                Destroy(obj);
        }
    }

    /// <summary>
    /// MEJORADO: Ahora limpia la lista de hijos de forma exhaustiva para "New Plot".
    /// </summary>
    public void ClearAxes(bool destroy = false)
    {
        // Usamos un bucle inverso para poder destruir hijos sin romper el iterador
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (destroy)
            {
                Destroy(child.gameObject);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
        Debug.Log($"ImmersiveSHAP, UNITY, 🧹 AxesBuilder: Ejes {(destroy ? "Destruidos" : "Desactivados")}.");
    }
}
