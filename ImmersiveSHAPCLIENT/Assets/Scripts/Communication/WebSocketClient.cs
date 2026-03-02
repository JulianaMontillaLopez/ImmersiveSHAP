using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;

/// <summary>
/// Manages WebSocket communication with Python, tracking network latency and metrics.
/// </summary>
public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance { get; private set; }

    private WebSocket websocket;

    // Metrics tracking
    private float _lastSendTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Update with your actual server IP
        websocket = new WebSocket("ws://192.168.1.70:8765");
        //websocket = new WebSocket("ws://localhost:8765");


        websocket.OnOpen += () =>
        {
            Debug.Log("[ImmersiveSHAP, UNITY, WebSocketClient] ✅ WebSocket Opened");
            SendInitRequest();
        };

        websocket.OnError += (e) => Debug.LogError("[ImmersiveSHAP, UNITY, WebSocketClient] ❌ WebSocket Error: " + e);
        websocket.OnClose += (e) => Debug.Log("[ImmersiveSHAP, UNITY, WebSocketClient] 🔌 WebSocket Closed");

        websocket.OnMessage += (bytes) =>
        {
            // T6: Data arrival from network
            float receiveTime = Time.realtimeSinceStartup;
            float networkDelay = (receiveTime - _lastSendTime) * 1000f;

            // Metrics log (CSV compatible)
            Debug.Log($"[METRICS] CSV_DATA,UNITY,RECV_NET,{receiveTime},{networkDelay}");

            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log($"[ImmersiveSHAP, UNITY, WebSocketClient] 📩 Message received ({bytes.Length} bytes)");

            DeserializationClient.ProcessMessage(message);
        };

        ConnectWebSocket();
    }

    private async void ConnectWebSocket()
    {
        try
        {
            await websocket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError("[ImmersiveSHAP, UNITY, WebSocketClient] ❗ Connection Error: " + e.Message);
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }

    public async void Send(string json)
    {
        // T1: Data departure
        _lastSendTime = Time.realtimeSinceStartup;
        Debug.Log($"[METRICS] CSV_DATA,UNITY,SEND_REQ,{_lastSendTime}");

        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(json);
            Debug.Log("[ImmersiveSHAP, UNITY, WebSocketClient] 📤 Sent to Python: " + json);
        }
        else
        {
            Debug.LogWarning("[ImmersiveSHAP, UNITY, WebSocketClient] ⚠️ WebSocket is not connected");
        }
    }

    public void SendInitRequest()
    {
        string initMessage = "{\"action\": \"init_visualmappingui\"}";
        Send(initMessage);
    }

    public void SendCancelSignal()
    {
        string cancelMessage = "{\"action\": \"cancel\"}";
        Send(cancelMessage);
    }

    public void SendShutdownSignal()
    {
        string shutdownMessage = "{\"action\": \"shutdown\"}";
        Send(shutdownMessage);
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}

// | Mensaje de Log             | Significado                                                                                                                                 |
//| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
//| `✅ WebSocket abierto`      | La conexión entre Unity y el servidor Python se ha establecido con éxito.                                                                   |
//| `❌ WebSocket error: ...`   | Ha ocurrido un error al intentar conectarse o durante la comunicación. Indica un problema en la red, URL incorrecta, servidor apagado, etc. |
//| `🔌 WebSocket cerrado`     | La conexión WebSocket fue cerrada. Puede ser porque el servidor se desconectó, el usuario salió del juego, o por error.                     |
//| `📤 Enviado: ...`          | Unity ha enviado correctamente un mensaje (en formato JSON) al servidor. Es el momento en que haces una solicitud de gráfico.               |
//| `📩 Mensaje recibido: ...` | Unity recibió una respuesta desde el servidor (normalmente datos del gráfico SHAP).                                                         |

