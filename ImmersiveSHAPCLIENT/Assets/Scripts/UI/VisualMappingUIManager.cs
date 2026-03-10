using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class VisualMappingUIManager : MonoBehaviour
{
    public static VisualMappingUIManager Instance { get; private set; }

    [Header("Contenedor UI")]
    public GameObject visualMappingPanel;

    [Header("Dropdown References")]
    public TMP_Dropdown datasetDropdown;
    public TMP_Dropdown modelDropdown;
    public TMP_Dropdown plotTypeDropdown;
    public TMP_Dropdown colormapDropdown;
    public TMP_Dropdown xAxisDropdown;
    public TMP_Dropdown zAxisDropdown;
    public TMP_Dropdown targetDropdown;

    [Header("Buttons")]
    public Button generateButton;
    public Button resetButton;
    public Button exitButton;

    private InitPayload currentPayload;
    private Dictionary<string, string[]> featuresByDataset;
    private Dictionary<string, string[]> targetsByDataset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (generateButton != null)
        {
            generateButton.onClick.RemoveAllListeners();
            generateButton.onClick.AddListener(OnGeneratePlot);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(OnReset);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
    }

    public void OnExitButtonClicked()
    {
        Debug.Log("ImmersiveSHAP, UNITY, [VisualMappingUIManager] 🛑 Terminando aplicación y desconectando sesión...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void PopulateDropdowns(InitPayload payload)
    {
        Debug.Log("ImmersiveSHAP, UNITY, [VisualMappingUIManager] 🧩 Populating dropdowns from InitPayload...");
        this.currentPayload = payload;
        featuresByDataset = payload.features;
        targetsByDataset = payload.targets;

        PopulateDropdown(datasetDropdown, payload.datasets);
        PopulateDropdown(modelDropdown, payload.models);
        PopulateDropdown(plotTypeDropdown, payload.plot_types);
        PopulateDropdown(colormapDropdown, payload.colormaps);

        PopulateDropdown(xAxisDropdown, new string[] { });
        PopulateDropdown(zAxisDropdown, new string[] { });
        PopulateDropdown(targetDropdown, new string[] { });

        datasetDropdown.onValueChanged.RemoveAllListeners();
        datasetDropdown.value = 0;
        datasetDropdown.RefreshShownValue();
        datasetDropdown.onValueChanged.AddListener(delegate { OnDatasetChanged(); });
    }

    private void PopulateDropdown(TMP_Dropdown dropdown, string[] options)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        List<string> optionList = new List<string> { "-- Select --" };
        if (options != null && options.Length > 0)
        {
            optionList.AddRange(options);
        }
        dropdown.AddOptions(optionList);
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    private void OnDatasetChanged()
    {
        string selectedDataset = GetDropdownValue(datasetDropdown);
        if (string.IsNullOrEmpty(selectedDataset) || selectedDataset == "-- Select --")
        {
            PopulateDropdown(modelDropdown, currentPayload.models);
            return;
        }

        if (currentPayload.dataset_types != null && currentPayload.dataset_types.TryGetValue(selectedDataset, out string datasetType))
        {
            List<string> filteredModels = new List<string>();
            foreach (var modelName in currentPayload.models)
            {
                if (currentPayload.model_tasks != null && currentPayload.model_tasks.TryGetValue(modelName, out string modelTask))
                {
                    if (modelTask == datasetType) filteredModels.Add(modelName);
                }
            }
            PopulateDropdown(modelDropdown, filteredModels.ToArray());
        }

        if (featuresByDataset != null && featuresByDataset.TryGetValue(selectedDataset, out var features))
        {
            PopulateDropdown(xAxisDropdown, features);

            // ✅ IMPROVEMENT: Populate Z-Axis with "Auto" option
            if (zAxisDropdown != null)
            {
                zAxisDropdown.ClearOptions();
                List<string> zOptions = new List<string> { "-- Select --", "Auto (SHAP Interaction)" };
                if (features != null) zOptions.AddRange(features);
                zAxisDropdown.AddOptions(zOptions);
                zAxisDropdown.value = 0;
                zAxisDropdown.RefreshShownValue();
            }
        }

        if (targetsByDataset != null && targetsByDataset.TryGetValue(selectedDataset, out var targets))
        {
            PopulateDropdown(targetDropdown, targets);
        }
    }

    public string GetSelectedColormap()
    {
        if (colormapDropdown == null) return null;
        string val = GetDropdownValue(colormapDropdown);
        if (val == "-- Select --") return null;
        return val;
    }

    public void OnGeneratePlot()
    {
        if (!IsValidSelection(datasetDropdown, "Dataset") ||
            !IsValidSelection(modelDropdown, "Model") ||
            !IsValidSelection(xAxisDropdown, "X Axis") ||
            !IsValidSelection(zAxisDropdown, "Z Axis") ||
            !IsValidSelection(targetDropdown, "Target") ||
            !IsValidSelection(colormapDropdown, "Colormap"))
        {
            Debug.LogWarning("ImmersiveSHAP, UNITY, [VisualMappingUIManager] ⚠️ Required fields are not selected.");
            return;
        }

        // ✅ LOGIC: Convert UI "Auto (SHAP Interaction)" to backend "auto"
        string zAxisValue = GetDropdownValue(zAxisDropdown);
        if (zAxisValue == "Auto (SHAP Interaction)") zAxisValue = "auto";

        var config = new UserPlotConfig
        {
            dataset = GetDropdownValue(datasetDropdown),
            model = GetDropdownValue(modelDropdown),
            plotType = "scatter",
            xAxis = GetDropdownValue(xAxisDropdown),
            zAxis = zAxisValue, // Sends "auto" or the feature name
            targetClass = GetDropdownValue(targetDropdown),
            colormap = GetSelectedColormap()
        };

        string json = DataFormattingClient.FormatToJson(config);
        Debug.Log($"ImmersiveSHAP, UNITY, [VisualMappingUIManager] 📤 Enviando configuración al servidor:\n{json}");
        WebSocketClient.Instance.Send(json);

        HidePanel();
    }

    public void ShowPanel()
    {
        if (visualMappingPanel != null)
        {

            visualMappingPanel.SetActive(true);

            // 🚀 NUEVO: Desactivamos la interacción con el gráfico mientras elegimos datos
            ObjectBoundsConstrainer obc = FindFirstObjectByType<ObjectBoundsConstrainer>();
            if (obc != null) obc.ResetInteractionState();

            var positioner = GetComponentInParent<VisualMappingUIPositioner>();
            if (positioner != null) positioner.RepositionNow();
        }
        


    }

    public void HidePanel()
    {
        if (visualMappingPanel != null)
        {
            Debug.Log("ImmersiveSHAP, UNITY, [VisualMappingUIManager] 🙈 Ocultando panel...");
            visualMappingPanel.SetActive(false);
        }
    }

    private string GetDropdownValue(TMP_Dropdown dropdown)
    {
        return dropdown.options[dropdown.value].text;
    }

    private bool IsValidSelection(TMP_Dropdown dropdown, string fieldName)
    {
        if (dropdown == null) return false;
        string value = GetDropdownValue(dropdown);
        if (value == "-- Select --")
        {
            Debug.LogWarning($"ImmersiveSHAP, UNITY, [VisualMappingUIManager] ⚠️ Field '{fieldName}' not selected.");
            return false;
        }
        return true;
    }

    public void OnReset()
    {
        Debug.Log("ImmersiveSHAP, UNITY, [VisualMappingUIManager] 🔄 Reseteando valores de la interfaz...");

        datasetDropdown.value = 0;
        modelDropdown.value = 0;
        plotTypeDropdown.value = 0;
        colormapDropdown.value = 0;
        xAxisDropdown.value = 0;
        zAxisDropdown.value = 0;
        targetDropdown.value = 0;

        datasetDropdown.RefreshShownValue();
        modelDropdown.RefreshShownValue();
        plotTypeDropdown.RefreshShownValue();
        colormapDropdown.RefreshShownValue();
        xAxisDropdown.RefreshShownValue();
        zAxisDropdown.RefreshShownValue();
        targetDropdown.RefreshShownValue();
    }
}
