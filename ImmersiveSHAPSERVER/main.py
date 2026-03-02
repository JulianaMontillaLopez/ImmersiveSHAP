import asyncio
from modules.communication.websocket_server import start_server
from modules.preprocessing.resource_inspector import get_features_for_dataset
from modules.preprocessing.resource_inspector import get_target_classes


#if __name__ == "__main__":
#    print(get_features_for_dataset("californiahousing"))
#    # Esperado: ['MedInc', 'HouseAge', 'AveRooms', ...]

#if __name__ == "__main__":
#    print(get_target_classes("californiahousing"))


if __name__ == "__main__":
    asyncio.run(start_server())



#   What it does:
# - Uses asyncio.run() to start the WebSocket server asynchronously.
# - Imports start_server() from websocket_server.py.