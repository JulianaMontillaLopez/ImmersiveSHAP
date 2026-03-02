using UnityEngine;
using Newtonsoft.Json;
using System.Diagnostics;

/// <summary>
/// Deserializes JSON responses from Python and routes them to UI/Rendering managers.
/// </summary>
public static class DeserializationClient
{
    [System.Serializable]
    private class ActionWrapper
    {
        [JsonProperty("action")]
        public string Action { get; set; } = string.Empty;
    }

    public static void ProcessMessage(string json)
    {
        UnityEngine.Debug.Log("[ImmersiveSHAP, UNITY, DeserializationClient] 📥 Processing message...");

        // Start measurement (T7)
        var stopwatch = Stopwatch.StartNew();

        ActionWrapper baseMessage;
        try
        {
            baseMessage = JsonConvert.DeserializeObject<ActionWrapper>(json);
        }
        catch (JsonException e)
        {
            UnityEngine.Debug.LogError("[ImmersiveSHAP, UNITY, DeserializationClient] ❌ Invalid JSON: " + e.Message);
            return;
        }

        if (baseMessage == null || string.IsNullOrEmpty(baseMessage.Action))
        {
            UnityEngine.Debug.LogError("[ImmersiveSHAP, UNITY, DeserializationClient] ❌ Error reading 'action'.");
            return;
        }

        switch (baseMessage.Action)
        {
            case "status":
                // Handle intermediate progress updates
                var statusData = JsonConvert.DeserializeObject<ServerStatusUpdate>(json);

                if (statusData.progress < 0)
                {
                    // Cancellation or Reset signal
                    if (ProcessStatusUI.Instance != null) ProcessStatusUI.Instance.Hide();
                    if (VisualMappingUIManager.Instance != null) VisualMappingUIManager.Instance.ShowPanel();
                }
                else if (ProcessStatusUI.Instance != null)
                {
                    ProcessStatusUI.Instance.ShowLoading(statusData.message);
                    ProcessStatusUI.Instance.UpdateProgress(statusData.progress, statusData.message);
                }
                break;

            case "plot_data":
            case "plot_response": // Handle both possible action names
                try
                {
                    // Check if data is nested under a "data" key
                    var jRoot = Newtonsoft.Json.Linq.JObject.Parse(json);
                    string dataJson = json;

                    if (jRoot.ContainsKey("data") && jRoot["data"].Type == Newtonsoft.Json.Linq.JTokenType.Object)
                    {
                        dataJson = jRoot["data"].ToString();
                    }

                    DeserializedData data = JsonConvert.DeserializeObject<DeserializedData>(dataJson);

                    if (data == null || data.x == null)
                    {
                        UnityEngine.Debug.LogError("[ImmersiveSHAP, UNITY, DeserializationClient] ❌ Error: Required fields missing.");
                        return;
                    }

                    stopwatch.Stop();
                    UnityEngine.Debug.Log($"[METRICS] CSV_DATA,UNITY,DESERIALIZE_PLOT,{stopwatch.Elapsed.TotalMilliseconds}");
                    UnityEngine.Debug.Log($"[ImmersiveSHAP, UNITY, DeserializationClient] 📊 Data ready: {data.x.Length} points");

                    if (PlotManager.Instance != null)
                    {
                        PlotManager.Instance.RenderPlot(data);
                        // Hide loading and show side-controls for interaction
                        if (ProcessStatusUI.Instance != null) ProcessStatusUI.Instance.ShowPersistentSideControls();
                    }
                }
                catch (JsonException e)
                {
                    UnityEngine.Debug.LogError("[ImmersiveSHAP, UNITY, DeserializationClient] ❌ Format Error: " + e.Message);
                }
                break;

            case "init_response":
                try
                {
                    InitPayload init = JsonConvert.DeserializeObject<InitPayload>(json);
                    stopwatch.Stop();
                    UnityEngine.Debug.Log($"[METRICS] CSV_DATA,UNITY,DESERIALIZE_INIT,{stopwatch.Elapsed.TotalMilliseconds}");

                    if (VisualMappingUIManager.Instance != null)
                    {
                        VisualMappingUIManager.Instance.PopulateDropdowns(init);
                    }
                }
                catch (JsonException e)
                {
                    UnityEngine.Debug.LogError("[ImmersiveSHAP, UNITY, DeserializationClient] ❌ InitPayload Error: " + e.Message);
                }
                break;

            case "error":
                var errorMsg = JsonConvert.DeserializeObject<ErrorMessage>(json);
                UnityEngine.Debug.LogError("[ImmersiveSHAP, UNITY, DeserializationClient] ❌ Server Error: " + errorMsg.message);
                if (ProcessStatusUI.Instance != null) ProcessStatusUI.Instance.UpdateProgress(0, "Error: " + errorMsg.message);
                break;

            default:
                UnityEngine.Debug.LogWarning("[ImmersiveSHAP, UNITY, DeserializationClient] ⚠️ Unknown action: " + baseMessage.Action);
                break;
        }
    }

    [System.Serializable]
    private class ServerStatusUpdate
    {
        public string message;
        public int progress;
    }

    [System.Serializable]
    private class ErrorMessage
    {
        public string message;
    }
}
