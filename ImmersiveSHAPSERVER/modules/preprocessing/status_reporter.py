# modules/preprocessing/status_reporter.py

import json
import asyncio


async def send_status(websocket, progress, message, queue=None):
    """
    Sends a status update JSON to Unity via websocket OR puts it in a queue for a parent process.
    Format: {"action": "status", "progress": int, "message": string}
    """
    payload = {
        "action": "status",
        "progress": progress,
        "message": message
    }

    # Always log to console in Python
    print(f"ImmersiveSHAP, PYTHON, [Status] {progress}% - {message}")

    # If a queue is provided (multiprocessing mode)
    if queue is not None:
        queue.put(payload)
        return

    # Direct websocket mode (backward compatibility)
    if websocket is not None:
        try:
            await websocket.send(json.dumps(payload))
        except Exception as e:
            print(f"ImmersiveSHAP, PYTHON, [Status] Failed to send status: {e}")


def send_status_sync(websocket, progress, message, loop=None, queue=None):
    """Synchronous wrapper for status reporting."""
    payload = {
        "action": "status",
        "progress": progress,
        "message": message
    }

    print(f"ImmersiveSHAP, PYTHON, [Status] {progress}% - {message}")

    if queue is not None:
        queue.put(payload)
        return

    if websocket is not None:
        if loop is None:
            try:
                loop = asyncio.get_event_loop()
            except RuntimeError:
                loop = asyncio.new_event_loop()
                asyncio.set_event_loop(loop)

        if loop.is_running():
            asyncio.run_coroutine_threadsafe(send_status(websocket, progress, message), loop)
        else:
            loop.run_until_complete(send_status(websocket, progress, message))
