# modules/communication/websocket_server.py

import json
import asyncio
import time
import websockets
import queue  # Import for queue.Empty exception
from multiprocessing import Process, Manager
from websockets.exceptions import ConnectionClosed

from modules.communication.data_formatting_server import format_response
from modules.preprocessing import cancellation_state, request_manager
from modules.preprocessing.request_manager import generate_plot_sync


async def handler(websocket):
    print("🔗 Cliente conectado")
    current_process = None
    manager = Manager()

    # Watchdog task to detect disconnect even when we're not sending/receiving
    watchdog = asyncio.create_task(websocket.wait_closed())

    try:
        # We need to run the message loop and the watchdog in parallel
        while not watchdog.done():
            try:
                # Use wait_for to check message loop without blocking indefinitely
                message = await asyncio.wait_for(websocket.recv(), timeout=0.1)
                t2 = time.time()

                try:
                    temp_data = json.loads(message)
                    action = temp_data.get("action")
                except:
                    action = None

                # --- 1. Control Signals ---
                if action == "cancel":
                    print("🛑 SEÑAL DE CANCELACIÓN RECIBIDA. Terminando proceso abruptamente...")
                    if current_process and current_process.is_alive():
                        current_process.terminate()
                        current_process.join()
                    continue

                if action == "shutdown":
                    print("🔌 Shutdown request. Closing server...")
                    if current_process and current_process.is_alive():
                        current_process.terminate()
                    asyncio.get_event_loop().stop()
                    break

                # --- 2. Simple Requests (Sync/Main Thread) ---
                if action == "init_visualmappingui":
                    print("📦 Solicitud de Inicialización recibida.")
                    resources = request_manager.get_initial_resources()
                    response_json = await format_response(resources)
                    await websocket.send(response_json)
                    continue

                # --- 3. Heavy ML Requests (Multiprocessing) ---
                if action == "generate_plot":
                    print(f"📊 Solicitud de Gráfico recibida. Iniciando Proceso ML...")
                    print(f"CSV_DATA,PYTHON,RECV_REQ,{t2}")

                    status_queue = manager.Queue()
                    current_process = Process(
                        target=generate_plot_sync,
                        args=(temp_data, status_queue)
                    )
                    current_process.start()

                    request_completed = False
                    try:
                        while current_process.is_alive() or not status_queue.empty():
                            # 1. CHECK DISCONNECT: If client vanished
                            if watchdog.done():
                                print("🔌 [ALERTA] Cliente desconectado durante ML. Terminando proceso...")
                                break

                            # 2. CHECK FOR MANUAL SIGNALS: Listen for 'cancel' while busy
                            try:
                                # Very short timeout to keep polling the queue
                                raw_msg = await asyncio.wait_for(websocket.recv(), timeout=0.01)
                                try:
                                    msg_data = json.loads(raw_msg)
                                    if msg_data.get("action") == "cancel":
                                        print("🛑 CANCELACIÓN MANUAL RECIBIDA DURANTE PROCESO. Abortando...")
                                        break
                                    if msg_data.get("action") == "shutdown":
                                        print("🔌 SHUTDOWN RECIBIDO DURANTE PROCESO.")
                                        asyncio.get_event_loop().stop()
                                        break
                                except:
                                    pass
                            except asyncio.TimeoutError:
                                pass  # No new control messages

                            # 3. CHECK FOR PROGRESS: Get updates from the worker process
                            try:
                                update = status_queue.get_nowait()

                                if update["action"] == "status":
                                    await websocket.send(json.dumps(update))

                                elif update["action"] == "result":
                                    plot_results = update["data"]
                                    response_json = await format_response(plot_results)
                                    t5 = time.time()
                                    await websocket.send(response_json)
                                    print(f"CSV_DATA,PYTHON,SEND_RESP,{t5}")
                                    request_completed = True  # ✅ Marcamos como completado
                                    break

                                elif update["action"] == "error":
                                    await websocket.send(json.dumps(update))
                                    break

                            except queue.Empty:
                                await asyncio.sleep(0.05)
                                if not current_process.is_alive() and status_queue.empty():
                                    break

                    finally:
                        if current_process and current_process.is_alive():
                            print("🧹 Limpiando proceso ML...")
                            current_process.terminate()
                            current_process.join()

                        current_process = None

                        # ✅ Solo enviamos confirmación si NO se completó con éxito (Cancelación o Error)
                        if not request_completed:
                            try:
                                await websocket.send(json.dumps({
                                    "action": "status",
                                    "progress": -1,
                                    "message": "Ready"
                                }))
                            except:
                                pass

                        print("✅ Petición finalizada. Servidor listo para nuevo comando.")

                else:
                    if action:
                        print(f"⚠️ Acción desconocida recibida: {action}")

            except asyncio.TimeoutError:
                # No new message, just loop back to check watchdog/recv again
                continue
            except ConnectionClosed:
                break

    except Exception as e:
        print(f"❌ Error Crítico en Handler: {e}")
    finally:
        print("🔌 Finalizando conexión y limpiando recursos...")
        if current_process and current_process.is_alive():
            current_process.terminate()
            current_process.join()
        if not watchdog.done():
            watchdog.cancel()
        print("✅ Servidor disponible para nueva conexión.")


# 🔹 Lanza el servidor
async def start_server():
    port = 8765
    print(f"🔌 WebSocket server iniciado en ws://0.0.0.0:{port} (Safety Watchdog Active)")
    #print(f"🔌 WebSocket server iniciado en ws://localhost:{port} (Safety Watchdog Active)")

    async with websockets.serve(handler, "0.0.0.0", port):
    #async with websockets.serve(handler, "localhost", port):
        await asyncio.Future()
