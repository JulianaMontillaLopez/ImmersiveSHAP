# modules/communication/data_formatting_server.py

import json
import time


async def format_response(response_data):

    t_start = time.time()
    json_payload = json.dumps(response_data)  # SERIALIZACIÓN
    duration_ms = (time.time() - t_start) * 1000

    payload_bytes = len(json_payload.encode("utf-8"))

    print(f"CSV_DATA,PYTHON,SERIALIZE,{duration_ms:.3f}")
    print(f"CSV_DATA,PYTHON,PAYLOAD_SIZE,{payload_bytes}")

    return json_payload
