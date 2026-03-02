using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VisualEncoder: aplica color, opacidad y escala a los puntos creados por GeometryBuilder.
/// Carga los colormaps desde Resources/Colormaps.
/// OPTIMIZADO: Caché de componentes, ComputeDensityMultipliers deshabilitado por defecto.
/// </summary>
public class VisualEncoder : MonoBehaviour
{
    [Header("Colormap Resources")]
    public string colormapResourceFolder = "Colormaps";
    public string defaultColormap = "cmap_red_blue";

    [Header("Opacity")]
    [Range(0f, 1f)]
    public float globalOpacity = 1f;

    [Header("Sizing")]
    public float basePointScale = 0.02f;

    [Header("⚠️ DENSITY SCALING (Expensive O(n²) - Disable for >500 points)")]
    public bool scaleByDensity = false;
    public float densityRadius = 0.05f;
    public float minScaleMultiplier = 0.5f;
    public float maxScaleMultiplier = 2.5f;

    // Internal
    private Dictionary<string, Texture2D> cmapTextures = new Dictionary<string, Texture2D>();
    private string activeCmapName;

    // Cache global de MaterialPropertyBlock
    private MaterialPropertyBlock sharedMPB;

    // 🚀 OPTIMIZACIÓN: Caché de componentes para evitar GetComponent repetido
    private Dictionary<GameObject, (Renderer rend, OriginalColorHolder holder)> componentCache = new();

    void Awake()
    {
        sharedMPB = new MaterialPropertyBlock();
        LoadAllColormaps();
        activeCmapName = defaultColormap.ToLower();
    }

    void LoadAllColormaps()
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>(colormapResourceFolder);
        cmapTextures.Clear();

        foreach (var tex in textures)
        {
            if (tex == null) continue;
            string key = tex.name.ToLower();
            if (!cmapTextures.ContainsKey(key))
                cmapTextures.Add(key, tex);
        }

        if (!cmapTextures.ContainsKey(defaultColormap.ToLower()))
            Debug.LogWarning($"[VisualEncoder] Default colormap '{defaultColormap}' not found.");
    }

    public bool HasColormap(string name)
        => !string.IsNullOrEmpty(name) && cmapTextures.ContainsKey(name.ToLower());

    public void SetActiveColormap(string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        string key = name.ToLower();
        if (cmapTextures.ContainsKey(key))
            activeCmapName = key;
        else
            Debug.LogWarning($"[VisualEncoder] Colormap '{name}' not found.");
    }

    /// <summary>
    /// Aplica color y tamaño a cada punto.
    /// OPTIMIZADO: Usa caché de componentes.
    /// </summary>
    public void Encode(GameObject[] points, float[] normalizedColorValues, float[] shapValues = null)
    {
        if (points == null || normalizedColorValues == null)
        {
            Debug.LogWarning("[VisualEncoder] Null inputs.");
            return;
        }

        if (points.Length != normalizedColorValues.Length)
        {
            Debug.LogWarning("[VisualEncoder] Size mismatch.");
            return;
        }

        // Cargar colormap
        Texture2D cmapTexture = null;
        if (activeCmapName != null)
            cmapTextures.TryGetValue(activeCmapName, out cmapTexture);

        // 🚀 OPTIMIZACIÓN: Precompute density SOLO si está habilitado
        float[] densityMultipliers = null;
        if (scaleByDensity && points.Length <= 500) // Límite de seguridad
        {
            Debug.LogWarning("[VisualEncoder] scaleByDensity habilitado con " + points.Length + " puntos. Esto es costoso.");
            densityMultipliers = ComputeDensityMultipliers(points);
        }
        else if (scaleByDensity && points.Length > 500)
        {
            Debug.LogError($"[VisualEncoder] scaleByDensity DESHABILITADO automáticamente. Tienes {points.Length} puntos (límite: 500).");
            scaleByDensity = false;
        }

        var mpb = sharedMPB;

        for (int i = 0; i < points.Length; i++)
        {
            GameObject go = points[i];
            if (go == null) continue;

            // 🚀 OPTIMIZACIÓN: Obtener componentes con caché
            if (!componentCache.TryGetValue(go, out var cached))
            {
                var rend = go.GetComponent<Renderer>();
                var holder = go.GetComponent<OriginalColorHolder>();
                if (holder == null) holder = go.AddComponent<OriginalColorHolder>();
                cached = (rend, holder);
                componentCache[go] = cached;
            }

            // -------- COLOR --------
            Color color = Color.white;

            if (cmapTexture != null)
                color = SampleColormap(cmapTexture, Mathf.Clamp01(normalizedColorValues[i]));

            color.a = globalOpacity;

            // Guardar el color original
            cached.holder.originalColor = color;

            // Aplicar color mediante MPB
            if (cached.rend != null)
            {
                cached.rend.GetPropertyBlock(mpb);
                mpb.Clear();
                mpb.SetColor("_BaseColor", color);
                cached.rend.SetPropertyBlock(mpb);
            }

            // -------- SCALE --------
            float scaleMultiplier = 1f;

            if (scaleByDensity && densityMultipliers != null)
                scaleMultiplier = densityMultipliers[i];

            else if (shapValues != null && i < shapValues.Length)
            {
                float v = Mathf.Abs(shapValues[i]);
                float mapped = 1f + Mathf.Clamp01(v);
                scaleMultiplier = Mathf.Clamp(mapped, minScaleMultiplier, maxScaleMultiplier);
            }

            go.transform.localScale = Vector3.one * basePointScale * scaleMultiplier;
        }
    }

    private float[] ComputeDensityMultipliers(GameObject[] points)
    {
        int n = points.Length;
        float[] mults = new float[n];

        Vector3[] positions = new Vector3[n];
        for (int i = 0; i < n; i++)
            positions[i] = points[i].transform.position;

        float r2 = densityRadius * densityRadius;

        for (int i = 0; i < n; i++)
        {
            int count = 0;
            Vector3 p = positions[i];

            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                if ((positions[j] - p).sqrMagnitude <= r2)
                    count++;
            }

            float norm = Mathf.Clamp01(count / 10f);
            mults[i] = Mathf.Lerp(maxScaleMultiplier, minScaleMultiplier, norm);
        }

        return mults;
    }

    private Color SampleColormap(Texture2D tex, float normalizedValue)
    {
        if (tex == null) return Color.white;

        int x = Mathf.RoundToInt(normalizedValue * (tex.width - 1));
        x = Mathf.Clamp(x, 0, tex.width - 1);

        return tex.GetPixel(x, tex.height / 2);
    }

    /// <summary>
    /// Limpia el caché de componentes (llamar al resetear la escena).
    /// </summary>
    public void ClearCache()
    {
        componentCache.Clear();
    }
}
