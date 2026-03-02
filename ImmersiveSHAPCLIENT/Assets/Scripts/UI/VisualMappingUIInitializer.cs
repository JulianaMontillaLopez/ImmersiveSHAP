using UnityEngine;

public class VisualMappingUIInitializer : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("ImmersiveSHAP, UNITY, [VisualMappingInitializer] 🎮 VisualMappingInterface inicializada, solicitando recursos...");
        WebSocketClient.Instance.SendInitRequest();
    }
}
