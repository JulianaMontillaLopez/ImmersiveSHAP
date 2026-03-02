using System;
using UnityEngine;

/// <summary>
/// Clase que representa los datos deserializados enviados por Python.
/// Actualizada para soportar normalización por percentiles y metadatos de clase.
/// </summary>
[Serializable]
public class DeserializedData
{
    [Header("3D Coordinates")]
    public float[] x; // Valores originales de la feature X
    public float[] y; // Impacto SHAP (eje vertical)
    public float[] z; // Valores originales de la feature Z (interacción)

    [Header("Axis Labels")]
    public string x_label;
    public string z_label;
    public string shap_label;

    [Header("Visual config")]
    public string colormap;   // ej: "cmap_red_blue"
    public string plot_type;  // ej: "scatter"

    [Header("Normalización (Fidelidad Visual)")]
    public float z_min; // Límite percentil 5 (mínimo visual para el color)
    public float z_max; // Límite percentil 95 (máximo visual para el color)

    [Header("Metadatos de la Explicación")]
    public string class_label; // Nombre real de la clase (ej: "malignant")
    public float base_value; // Valor base del modelo (E[f(x)])
    public float[] final_predictions; // Resultado final para cada punto (f(x))
    public float[] impact_shares; // % de contribución de la feature actual
    public float[] x_quantiles; // Percentil de la feature X (0-1)

    /// <summary>
    /// Helper para obtener los nombres de las características.
    /// </summary>
    public string[] featureNames => new string[] { x_label, z_label };
}

