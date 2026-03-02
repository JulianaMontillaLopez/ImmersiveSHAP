from modules.preprocessing import load_data
from modules.preprocessing import preprocessing_data
from modules.preprocessing import train_model
from modules.preprocessing import explanation_generation
from modules.preprocessing import plot_dispatcher
from modules.preprocessing import resource_inspector
from modules.preprocessing import status_reporter
from modules.preprocessing import cancellation_state
import time


def generate_plot_sync(request_data, status_queue=None):
    """
    Synchronous pipeline for SHAP. Designed to run in a separate PROCESS.
    Reports progress via status_queue.
    """
    # Reset cancellation state for the new process
    cancellation_state.reset()

    try:
        dataset_name = request_data["dataset"]
        model_name = request_data.get("model")

        # 📄 LOG CONFIGURATION
        print("\n📥 PROCESSING REQUEST (Worker Process):")
        print(f"   • Dataset:   {dataset_name}")
        print(f"   • Model:     {model_name}")
        print("--------------------------------------------")

        # 1️⃣ Metadatos e Inicio
        status_reporter.send_status_sync(None, 5, "Initializing Request...", queue=status_queue)
        dataset_metadata = resource_inspector.get_dataset_metadata(dataset_name)
        if "error" in dataset_metadata:
            raise ValueError(dataset_metadata["error"])

        task_type = dataset_metadata["type"]
        cancellation_state.check_interruption()

        # 2️⃣ Load Data
        status_reporter.send_status_sync(None, 15, f"Loading Dataset: {dataset_name}", queue=status_queue)
        X, y_labels, _ = load_data.load(dataset_name)
        cancellation_state.check_interruption()

        # 3️⃣ Prepare Target (y)
        if task_type == "classification":
            class_mapping = {
                name: i for i, name in enumerate(sorted(y_labels.unique()))
            }
            y_num = y_labels.map(class_mapping)
        else:
            y_num = y_labels

        cancellation_state.check_interruption()

        # 4️⃣ Preprocesar
        status_reporter.send_status_sync(None, 30, "Preprocessing Data...", queue=status_queue)
        model_info = resource_inspector.AVAILABLE_MODELS.get(model_name, {})
        needs_scaling = model_info.get("needs_scaling", True)

        if needs_scaling:
            X_proc = preprocessing_data.clean_and_scale(X)
        else:
            X_proc = X

        cancellation_state.check_interruption()

        # 5️⃣ Train Model
        status_reporter.send_status_sync(None, 40, f"Training AI Model ({model_name})...", queue=status_queue)
        t_train_start = time.perf_counter()
        model = train_model.train(X_proc, y_num, task_type)
        t_train_end = time.perf_counter()
        training_time = t_train_end - t_train_start
        cancellation_state.check_interruption()

        # 6️⃣ SHAP Explanations
        status_reporter.send_status_sync(None, 65, "Calculating SHAP Explanations...", queue=status_queue)
        t_shap_start = time.perf_counter()
        explainer, shap_values = explanation_generation.explain(model, X_proc)
        t_shap_end = time.perf_counter()
        shap_time = t_shap_end - t_shap_start
        cancellation_state.check_interruption()

        # ⏱️ Tiempos
        ml_total_time = t_shap_end - t_train_start
        print(f"CSV_DATA,PYTHON,ML_TOTAL_MS,{ml_total_time * 1000:.3f}")

        # 7️⃣ Generate Data for Unity
        status_reporter.send_status_sync(None, 90, "Exporting Visual Data...", queue=status_queue)
        request_data["task_type"] = task_type
        plot_data = plot_dispatcher.dispatch(
            request_data, X_proc, shap_values, explainer, y_labels
        )

        status_reporter.send_status_sync(None, 100, "Done!", queue=status_queue)

        # Return final result through queue
        if status_queue:
            status_queue.put({"action": "result", "data": plot_data})

        return plot_data

    except Exception as e:
        error_msg = f"Error during 'generate_plot_sync': {str(e)}"
        print(f"❌ {error_msg}")
        if status_queue:
            status_queue.put({"action": "error", "message": error_msg})
        raise RuntimeError(error_msg)


def get_initial_resources():
    """Returns available datasets, models, and configurations."""
    return resource_inspector.get_resources()
