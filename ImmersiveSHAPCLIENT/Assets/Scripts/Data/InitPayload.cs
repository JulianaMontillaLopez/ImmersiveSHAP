using System.Collections.Generic;

[System.Serializable]
public class InitPayload
{
    public string[] datasets;
    public string[] models;
    public string[] plot_types;
    public string[] colormaps;
    public Dictionary<string, string[]> features;
    public Dictionary<string, string[]> targets;
    public Dictionary<string, string> dataset_types;
    public Dictionary<string, string> model_tasks;
}
