using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;

/// <summary>
/// Controls URP rendering settings and platform-specific optimizations (Quest vs PC).
/// Ensures the application maintains target framerates in VR.
/// </summary>
public class RendererController : MonoBehaviour
{
    public enum QualityProfile { QuestQuality, PCQuality }

    [Header("References")]
    public Transform plotRoot;
    public Material pointMaterial;

    [Header("Runtime Toggles")]
    public bool disableShadowsWhenHidden = true;

    private bool _isRenderingEnabled = true;
    private UnityEngine.ShadowQuality originalShadowsState = UnityEngine.ShadowQuality.All;
    private UniversalRenderPipelineAsset urpAsset;

    private void Awake()
    {
        // Cache the current URP asset
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (urpAsset != null)
        {
            originalShadowsState = QualitySettings.shadows;
        }

        // Automatic optimization based on detected hardware
        ApplyPlatformSpecificSettings();
    }

    /// <summary>
    /// Detects if the device is a mobile VR headset (Quest) or a PC.
    /// </summary>
    public void ApplyPlatformSpecificSettings()
    {
        bool isQuest = IsQuestDevice();
        Debug.Log($"[RendererController] Applying {(isQuest ? "QUEST" : "PC")} optimizations.");
        UpdateRenderingSettings(isQuest ? QualityProfile.QuestQuality : QualityProfile.PCQuality);
    }

    private bool IsQuestDevice()
    {
        // Android platform is a strong indicator for Quest/Mobile VR
        if (Application.platform == RuntimePlatform.Android) return true;

        // Check if XR is initialized (covers cases like Link or specific VR runtimes)
        var mgr = XRGeneralSettings.Instance?.Manager;
        return mgr != null && mgr.isInitializationComplete;
    }

    /// <summary>
    /// Configures URP settings for performance or visual fidelity.
    /// </summary>
    public void UpdateRenderingSettings(QualityProfile profile)
    {
        if (urpAsset == null) return;

        switch (profile)
        {
            case QualityProfile.QuestQuality:
                SetMSAA(2);
                SetRenderScale(0.9f); // Slight downscale for performance stability
                SetShadows(false, 10f); // Shadows are expensive on mobile mobile tiles
                EnableGPUInstancing(true); // CRITICAL for point clouds
                break;

            case QualityProfile.PCQuality:
                SetMSAA(4);
                SetRenderScale(1.0f);
                SetShadows(true, 50f);
                EnableGPUInstancing(true);
                break;
        }
    }

    /* ------------------------------------------------------------
     *  VISIBILITY CONTROL (Used by PlotManager)
     * ------------------------------------------------------------ */

    /// <summary>
    /// Hides the plot and optionally disables global shadows to save power.
    /// </summary>
    public void DisableRendering()
    {
        if (!_isRenderingEnabled) return;

        // Optimization: Just disable the root object instead of finding all renderers
        if (plotRoot != null) plotRoot.gameObject.SetActive(false);

        if (disableShadowsWhenHidden)
        {
            originalShadowsState = QualitySettings.shadows;
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
        }

        _isRenderingEnabled = false;
    }

    /// <summary>
    /// Resumes plot rendering and restores visual settings.
    /// </summary>
    public void EnableRendering()
    {
        if (_isRenderingEnabled) return;

        if (plotRoot != null) plotRoot.gameObject.SetActive(true);

        if (disableShadowsWhenHidden)
        {
            QualitySettings.shadows = originalShadowsState;
        }

        _isRenderingEnabled = true;
    }

    /* ------------------------------------------------------------
      *  LOW LEVEL URP WRAPPERS
      * ------------------------------------------------------------ */

    public void SetShadows(bool enabled, float distance)
    {
        QualitySettings.shadows = enabled ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.Disable;
        if (urpAsset != null) urpAsset.shadowDistance = distance;
    }

    public void SetMSAA(int samples)
    {
        // Note: Changing MSAA on the asset at runtime only works on certain mobile GPUs or 
        // if using different Pipeline Assets. 
        if (urpAsset != null) urpAsset.msaaSampleCount = samples;
    }

    public void SetRenderScale(float scale)
    {
        if (urpAsset != null) urpAsset.renderScale = scale;
    }

    private void EnableGPUInstancing(bool enable)
    {
        if (pointMaterial != null) pointMaterial.enableInstancing = enable;
    }
}
