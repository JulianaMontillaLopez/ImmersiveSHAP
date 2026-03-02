using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PointFilter : MonoBehaviour
{
    public static PointFilter Instance { get; private set; } // <--- AGREGAR ESTO

    [Header("Downsampling Settings")]
    public int maxPoints = 2000;

    [Header("Jitter Settings")]
    public float jitterAmount = 0.05f;
    public int jitterThreshold = 15; // valores únicos máximos en eje para aplicar jitter

    [Header("Clipping Settings")]
    public float zScoreThreshold = 3f;// Este valor se enviará a Python

    private Vector3[] filteredPoints;


    private void Awake() // <--- AGREGAR ESTO
    {
        Instance = this;
    }

    /// <summary>
    /// Aplica clipping, downsampling, jitter y normalización a los puntos,
    /// manteniendo sincronizados los valores asociados (shap, color).
    /// </summary>
    public (Vector3[] positions, List<float> primary, List<float> shap, List<float> secondary)
    ApplyFilters(Vector3[] inputPoints, List<float> primaryFeatureValues, List<float> shapValues, List<float> secondaryFeatureValues)
    {
        // 1. Clipping de outliers
        var (pos, primary, shap, secondary) =
            ClipOutliers(inputPoints, primaryFeatureValues, shapValues, secondaryFeatureValues);

        // 2. Downsampling si hay demasiados puntos
        if (pos.Length > maxPoints)
            (pos, primary, shap, secondary) =
                Downsample(pos, primary, shap, secondary, maxPoints); 

        // 3. Jitter si los valores en X o Y son discretos
        if (ShouldApplyJitter(pos))
            pos = ApplyJitter(pos, jitterAmount);

        filteredPoints = pos;

        return (pos, primary, shap, secondary);
    }

    // --------------- MÉTODOS INTERNOS ----------------
    private (Vector3[], List<float>, List<float>, List<float>)
    ClipOutliers(Vector3[] points, List<float> primary, List<float> shap, List<float> secondary)
    {
        Vector3 mean = Vector3.zero;
        foreach (var p in points) mean += p;
        mean /= points.Length;

        Vector3 std = Vector3.zero;
        foreach (var p in points)
        {
            std.x += Mathf.Pow(p.x - mean.x, 2);
            std.y += Mathf.Pow(p.y - mean.y, 2);
            std.z += Mathf.Pow(p.z - mean.z, 2);
        }
        std.x = Mathf.Sqrt(std.x / points.Length);
        std.y = Mathf.Sqrt(std.y / points.Length);
        std.z = Mathf.Sqrt(std.z / points.Length);

        List<Vector3> fPos = new();
        List<float> fPrimary = new();
        List<float> fShap = new();
        List<float> fSecondary = new();

        for (int i = 0; i < points.Length; i++)
        {
            var p = points[i];
            // Clipping en X y Z (Variables de entrada)
            if (std.x > 0 && Mathf.Abs((p.x - mean.x) / std.x) > zScoreThreshold) continue;
            if (std.z > 0 && Mathf.Abs((p.z - mean.z) / std.z) > zScoreThreshold) continue;

            // ACLARACIÓN: No clipeamos el eje Y (SHAP) a menos que sea extremo (Z > 10)
            if (std.y > 0 && Mathf.Abs((p.y - mean.y) / std.y) > 10f) continue;
            fPos.Add(p);
            fPrimary.Add(primary[i]);
            fShap.Add(shap[i]);
            fSecondary.Add(secondary[i]);
        }
        return (fPos.ToArray(), fPrimary, fShap, fSecondary);
    }

    private (Vector3[], List<float>, List<float>, List<float>)
    Downsample(Vector3[] points, List<float> primary, List<float> shap, List<float> secondary, int count)
    {
        System.Random rand = new();
        int n = points.Length;
        int targetCount = Mathf.Min(count, n); // 🛡️ PROTECCIÓN CONTRA BUCLE INFINITO


        HashSet<int> chosen = new();
        List<Vector3> dPos = new();

        List<float> dPrimary = new();
        List<float> dShap = new();
        List<float> dSecondary = new();

        while (chosen.Count < targetCount)
        {
            int idx = rand.Next(n);
            if (chosen.Add(idx))
            {
                dPos.Add(points[idx]);
                dPrimary.Add(primary[idx]);      // ✅ Corregido
                dShap.Add(shap[idx]);
                dSecondary.Add(secondary[idx]);
            }
        }

        return (dPos.ToArray(), dPrimary, dShap, dSecondary);
    }

    private bool ShouldApplyJitter(Vector3[] points)
    {
        // ✅ REFINAMIENTO: Solo comprobamos el eje X, ya que el jitter solo afecta a X.
        // Si la característica X es discreta (pocos valores únicos), aplicamos jitter.
        var uniqueX = points.Select(p => Mathf.Round(p.x * 1000) / 1000f).Distinct().Count();
        return uniqueX <= jitterThreshold;
    }


    private Vector3[] ApplyJitter(Vector3[] points, float amount)
    {
        System.Random rand = new();
        return points.Select(p =>
            new Vector3(
                p.x + ((float)rand.NextDouble() - 0.5f) * amount, // Jitter en X (Correcto)
                p.y,                                              // 🎯 JITTER CERO EN Y (Exactitud SHAP)
                p.z
            )).ToArray();
    }

   
    public Vector3[] GetFilteredPoints() => filteredPoints;
}
