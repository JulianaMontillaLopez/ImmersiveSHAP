using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // Necesario para detectar el botón Menú

/// <summary>
/// Gestiona la pantalla de carga y el menú oculto activable por botón Menú.
/// </summary>
public class ProcessStatusUI : MonoBehaviour
{
    public static ProcessStatusUI Instance { get; private set; }

    [Header("Containers (From Walkthrough)")]
    public GameObject overlayRoot;        // LoadingContainer
    public GameObject persistentRoot;     // PersistentContainer

    [Header("Progress Elements")]
    public TextMeshProUGUI statusText;
    public Slider progressBar;

    [Header("Buttons")]
    public Button cancelLoadingButton;
    public Button newPlotButton;
    public Button exitAppButton;

    [Header("Quest 3 Menu Toggle")]
    [Tooltip("Acción recomendada: <XRController>{LeftHand}/menu")]
    public InputActionProperty menuToggleAction;

    private bool isPlotActive = false; // Bloquea el menú si no hay gráfico cargado

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        HideAll();

        // Configurar botones
        if (cancelLoadingButton != null) cancelLoadingButton.onClick.AddListener(OnCancelRequest);
        if (newPlotButton != null) newPlotButton.onClick.AddListener(OnNewPlotRequest);
        if (exitAppButton != null) exitAppButton.onClick.AddListener(OnExitRequest);
    }

    private void OnEnable() => menuToggleAction.action?.Enable();
    private void OnDisable() => menuToggleAction.action?.Disable();

    private void Update()
    {
        // Detectar pulsación del botón Menú del Quest 3
        if (isPlotActive && menuToggleAction.action != null && menuToggleAction.action.WasPressedThisFrame())
        {
            ToggleMenu();
        }
    }

    public void ShowLoading(string message)
    {
        isPlotActive = false;
        overlayRoot.SetActive(true);
        persistentRoot.SetActive(false);
        UpdateProgress(0, message);
    }

    public void UpdateProgress(float percent, string message)
    {
        if (overlayRoot != null && !overlayRoot.activeSelf) overlayRoot.SetActive(true);

        if (progressBar != null) progressBar.value = percent / 100f;
        if (statusText != null) statusText.text = message;
    }

    /// <summary>
    /// Llamado cuando los datos llegan del servidor.
    /// </summary>
    public void ShowPersistentSideControls()
    {
        isPlotActive = true;
        overlayRoot.SetActive(false); // Quitamos la pantalla de carga
        persistentRoot.SetActive(false); // EL MENU QUEDA OCULTO por defecto
        Debug.Log("[ProcessStatusUI] Gráfico listo. Pulsa el botón MENÚ para ver opciones.");
    }

    public void Hide()
    {
        overlayRoot.SetActive(false);
        persistentRoot.SetActive(false);
    }

    private void ToggleMenu()
    {
        bool currentState = persistentRoot.activeSelf;
        persistentRoot.SetActive(!currentState);
        Debug.Log($"[ProcessStatusUI] Menú {(!currentState ? "Mostrado" : "Oculto")}");
    }

    private void HideAll() => Hide();

    private void OnCancelRequest()
    {
        WebSocketClient.Instance.SendCancelSignal();
        UpdateProgress(0, "Cancelling...");
    }

    private void OnNewPlotRequest()
    {
        Debug.Log("🔄 Iniciando flujo de New Plot...");

        // 1. LIMPIAR EL GRÁFICO ACTUAL COMPLETAMENTE
        if (PlotManager.Instance != null)
        {
            PlotManager.Instance.ClearCurrentPlot();
        }

        // 2. CAMBIO DE INTERFAZ
        isPlotActive = false;
        persistentRoot.SetActive(false); // Ocultar el menú de botones

        if (VisualMappingUIManager.Instance != null)
        {
            VisualMappingUIManager.Instance.ShowPanel(); // Volver al inicio
        }
    }


    private void OnExitRequest()
    {
        WebSocketClient.Instance.SendShutdownSignal();
        Application.Quit();
    }
}
