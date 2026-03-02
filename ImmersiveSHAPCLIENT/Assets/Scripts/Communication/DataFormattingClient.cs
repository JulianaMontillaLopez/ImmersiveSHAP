using UnityEngine;

public static class DataFormattingClient
{
    public static string FormatToJson(UserPlotConfig config)
    {
        //  Obtener valores automáticamente del Editor
        int maxPts = PointFilter.Instance != null ? PointFilter.Instance.maxPoints : 2000;
        float zThresh = PointFilter.Instance != null ? PointFilter.Instance.zScoreThreshold : 3.0f;

        RequestPayload payload = new RequestPayload()
        {
            action = "generate_plot",
            dataset = config.dataset,
            model = config.model,
            plot_type = config.plotType,
            colormap = config.colormap,
            x_feature = config.xAxis,
            z_feature = config.zAxis,
            target = config.targetClass,
            // ENVIAR METADATOS DE SINCRONIZACIÓN
            max_points = maxPts,
            z_score_threshold = zThresh
        };

        return JsonUtility.ToJson(payload);
    }

    [System.Serializable]
    private class RequestPayload
    {
        public string action;
        public string dataset;
        public string model;
        public string plot_type;
        public string colormap;
        public string x_feature;
        public string z_feature;
        public string target;
        public int max_points;
        public float z_score_threshold;

    }
}
