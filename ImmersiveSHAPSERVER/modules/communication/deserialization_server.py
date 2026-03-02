# modules/communication/deserialization_server.py
import json
from modules.preprocessing import request_manager

# Añadimos ', websocket=None' aquí para aceptar el segundo argumento


async def handle_message(message, websocket=None):
    try:
        data = json.loads(message)
        action = data.get("action")

        if action == "init_visualmappingui":
            return request_manager.get_initial_resources()

        elif action == "generate_plot":
            # Le pasamos el websocket al manager para que pueda enviar el progreso
            return await request_manager.generate_plot(data, websocket)

        else:
            return {"action": "error", "message": f"Action '{action}' not recognized"}

    except json.JSONDecodeError:
        return {"action": "error", "message": "Invalid JSON"}

    except Exception as e:
        return {"action": "error", "message": f"Error handling message: {str(e)}"}
