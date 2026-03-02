# modules/preprocessing/cancellation_state.py

import threading

# Global flag to signal cancellation across threads/tasks
_cancelled = False
_lock = threading.Lock()


def reset():
    global _cancelled
    with _lock:
        _cancelled = False


def cancel():
    global _cancelled
    with _lock:
        _cancelled = True
        print("ImmersiveSHAP, PYTHON, [Cancellation] Signal RECEIVED. Halting processes...")


def is_cancelled():
    with _lock:
        return _cancelled


def check_interruption():
    """Utility to raise an exception if cancellation has been requested."""
    if is_cancelled():
        raise InterruptedError("Process cancelled by user.")
