using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prepara los datos deserializados para su uso en el pipeline.
/// Empaqueta x, y, z como posiciones 3D y extrae vectores auxiliares.
/// </summary>
public static class DataInputManager
{
    //public static void Prepare(
    //    DeserializedData data,
    //    out Vector3[] positions,
    //    out List<float> shapValues,
    //    out List<float> colorFeatureValues)
    public static void Prepare(
    DeserializedData data,
    out Vector3[] positions,
    out List<float> primaryFeatureValues,
    out List<float> shapValues,
    out List<float> secondaryFeatureValues)
    {
        int count = data.x.Length;

        positions = new Vector3[count];
        primaryFeatureValues = new List<float>(count);
        shapValues = new List<float>(count);
        secondaryFeatureValues = new List<float>(count);

        for(int i = 0; i < count; i++)
        {
            float featureValue = data.x[i];
            float shapValue = data.y[i];
            float secondaryVal = data.z[i];

            // Geometría (render)
            positions[i] = new Vector3(featureValue, shapValue, secondaryVal);

            // Semántica (datos originales)
            primaryFeatureValues.Add(featureValue);
            shapValues.Add(shapValue);
            secondaryFeatureValues.Add(secondaryVal);
        }


        Debug.Log($"ImmersiveSHAP, UNITY, 📦 DataInputManager: {count} posiciones generadas.");
    }
}
