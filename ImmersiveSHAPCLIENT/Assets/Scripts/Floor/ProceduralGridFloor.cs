using UnityEngine;

/// <summary>
/// Draws and generates a perfect grid texture in memory mathematically 
/// without the need for external image files.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class ProceduralGridFloor : MonoBehaviour
{
    [Header("Clinical Color Generator")]
    [Tooltip("Tile background color (Suggested: #3A3A3A)")]
    public Color backgroundColor = new Color32(58, 58, 58, 255);

    [Tooltip("Grid line color (A faint darker gray)")]
    public Color lineColor = new Color32(40, 40, 40, 255);

    [Header("Grid Density")]
    [Tooltip("How many tiles to paint on the floor (100 = small human-scale tiles)")]
    public float tiling = 100f;

    private void Start()
    {
        int resolution = 64; // Square drawing quality/resolution

        // 1. Create a blank canvas (Digital texture)
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Trilinear; // Smoothing required for viewing distance in VR

        // 2. Color our grid mathematically pixel by pixel
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // If the pixel touches the border (square limits), draw it as a line
                bool isLine = x < 2 || y < 2 || x > resolution - 3 || y > resolution - 3;
                texture.SetPixel(x, y, isLine ? lineColor : backgroundColor);
            }
        }
        texture.Apply(); // Upload to GPU

        // 3. Apply to the Floor by cloning its initial material
        // (This prevents shader breakage regardless of whether you use URP or Standard pipeline)
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Material mat = new Material(renderer.sharedMaterial);

        // Set our grid texture and multiply it by the tiling factor
        mat.mainTexture = texture;
        mat.mainTextureScale = new Vector2(tiling, tiling);

        // Make the floor completely matte to eliminate glowing reflections
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0f);
        mat.color = Color.white;

        renderer.material = mat;
    }
}
