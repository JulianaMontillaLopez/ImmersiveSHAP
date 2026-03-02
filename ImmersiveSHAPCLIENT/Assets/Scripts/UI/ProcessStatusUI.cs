using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the loading overlay that shows Python server progress.
/// Integrado con DeserializationClient y WebSocketClient.
/// </summary>
public class ProcessStatusUI : MonoBehaviour
{
    public static ProcessStatusUI Instance { get; private set; }

    [Header("UI Containers")]
    public GameObject overlayRoot;        // El panel de carga (bloquea la vista) -> Arrastrar el 'StatusCanvas' o 'LoadingContainer'
    public GameObject persistentRoot;   // Controles pequeños que quedan visibles tras cargar -> Arrastrar el 'PersistentContainer'

    [Header("Visual Elements")]
    public TextMeshProUGUI statusText;
    public Slider progressBar;

    [Header("Buttons")]
    public Button cancelLoadingButton; // Corresponde a 'CancelBtn'
    public Button newPlotButton;         // Corresponde a 'NewPlotBtn'
    public Button exitButton; // Corresponde a 'ExitBtn'


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Estado inicial
        Hide();
        if (persistentRoot != null) persistentRoot.SetActive(false);

        // Setup button listeners
        if (cancelLoadingButton != null)
            cancelLoadingButton.onClick.AddListener(OnCancelRequest);
        if (newPlotButton != null) 
            newPlotButton.onClick.AddListener(OnNewPlotRequest);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitRequest);
    }

    /// <summary>
    /// Alias para mostrar la UI con un mensaje específico, requerido por DeserializationClient.
    /// </summary>
    public void ShowLoading(string message)
    {
        if (overlayRoot != null && !overlayRoot.activeSelf)
            overlayRoot.SetActive(true);

        // Al empezar a cargar, ocultamos los controles laterales si existieran
        if (persistentRoot != null) persistentRoot.SetActive(false);

        UpdateProgress(0, message);
    }

    public void UpdateProgress(float percent, string message)
    {
        // Si recibimos progreso y la UI está apagada, la encendemos
        if (overlayRoot != null && !overlayRoot.activeSelf)
            overlayRoot.SetActive(true);

        if (progressBar != null) progressBar.value = percent / 100f;
        if (statusText != null) statusText.text = message;

        Debug.Log($"[ProcessStatusUI] {percent}% - {message}");
    }

    /// <summary>
    /// Llamado cuando el gráfico se ha renderizado.
    /// Oculta el bloqueador de carga y muestra controles de interacción.
    /// </summary>
    public void ShowPersistentSideControls()
    {
        Hide(); // Ocultamos el overlay de carga
        if (persistentRoot != null)
            persistentRoot.SetActive(true);

        Debug.Log("[ProcessStatusUI] Plot ready. Showing interaction controls.");
    }

    public void Hide()
    {
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    private void OnCancelRequest()
    {
        UpdateProgress(0, "Cancelling Process...");
        // Desactivamos el botón para evitar spam
        if (cancelLoadingButton != null) cancelLoadingButton.interactable = false;

        WebSocketClient.Instance.SendCancelSignal();
    }

    private void OnNewPlotRequest()
    {
        // Oculta los controles y vuelve a mostrar el panel de configuración original
        persistentRoot.SetActive(false);
        if (VisualMappingUIManager.Instance != null)
            VisualMappingUIManager.Instance.ShowPanel();
    }

    private void OnExitRequest()
    {
        Debug.Log("[App] Exit Request. Closing application.");
        WebSocketClient.Instance.SendShutdownSignal();
        Application.Quit();
    }

    private void OnDestroy()
    {
        if (cancelLoadingButton != null) cancelLoadingButton.onClick.RemoveAllListeners();
        if (exitButton != null) exitButton.onClick.RemoveAllListeners();
    }
}
