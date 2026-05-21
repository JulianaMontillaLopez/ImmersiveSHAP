# ImmersiveSHAP: Immersive Visualization System for Explainable Artificial Intelligence (XAI)

---

## 📄 Manuscript Information

**Title:** *ImmersiveSHAP: Immersive analytics visualization system for XAI using SHAP scatter plot*  
**Manuscript ID:** 10596  

---

## 👥 Authors

- **Juliana Andrea Montilla López** — Universidad del Cauca  
- **Daniel Valencia Medina** — Universidad del Cauca  
- **Jovani Alberto Jiménez Builes** — Universidad Nacional de Colombia Sede Medellín  
- **Gustavo Adolfo Ramírez González** — Universidad del Cauca  

---

## 📌 Overview

ImmersiveSHAP is an immersive visualization system for Explainable Artificial Intelligence (XAI) that supports the exploration of SHAP explanations in Virtual Reality environments. 
The system transforms machine learning explanations into three-dimensional immersive visualizations that allow users to inspect feature contributions and interactions.
The architecture follows a distributed **Client–Server model** based on asynchronous WebSocket communication:

- **Python Server:** preprocessing, model training, SHAP computation, and data serialization
- **Unity Client:** immersive visualization and VR interaction

---

## 📦 Software Stack

### Python Backend

- Python 3.11.9
- NumPy 2.2.6
- scikit-learn 1.7.0
- XGBoost 3.0.2
- SHAP 0.48
- Matplotlib 3.10.3
- WebSockets 15.0.1

### Unity Frontend

- Unity 6000.0.29f1
- Universal Render Pipeline (URP)
- XR Interaction Toolkit 3.0.8
- OpenXR
- NativeWebSocket

All Python package versions used during development are included in:

```text
requirements.txt
```

to facilitate reproducibility.

---

## 📊 Datasets

The system uses datasets provided directly by the scikit-learn API. Datasets are loaded programmatically during execution, and therefore, no external files are required.

Implemented datasets:

- Iris Dataset (`load_iris`)
- Breast Cancer Wisconsin Dataset (`load_breast_cancer`)
- California Housing Dataset (`fetch_california_housing`)

Dataset loading implementation:

```python
from sklearn.datasets import (
    load_iris,
    load_breast_cancer,
    fetch_california_housing
)
```

Dataset documentation:

https://scikit-learn.org/stable/datasets.html

---

## 📂 Repository Structure

Repository root:

```text
ImmersiveSHAP/
├── ImmersiveSHAPCLIENT/          # Unity client project
├── ImmersiveSHAPSERVER/          # Python serve modules
└── README.md
```

---
## 📂 Detailed File Description

The following tables summarize the primary implementation modules and scripts included in the repository.

---

### 🖥 Python Server

Location:

```text
ImmersiveSHAPSERVER/modules/
```

Main server entry:

```text
ImmersiveSHAPSERVER/main.py
```

| Module/File | Script | Description |
|---|---|---|
| Preprocessing | `request_manager.py` | Orchestrates the analytical workflow and coordinates processing modules |
| Preprocessing | `load_data.py` | Loads datasets and returns features, targets, and metadata |
| Preprocessing | `preprocessing_data.py` | Applies data cleaning and preprocessing operations |
| Preprocessing | `train_model.py` | Trains machine learning models |
| Preprocessing | `explanation_generation.py` | Computes SHAP explanations |
| Preprocessing | `generate_scatter_plot.py` | Generates SHAP scatter plots, extracts spatial information, and exports a 2D scatter plot to the `output/` folder. |
| Preprocessing | `data_export.py` | Serializes processed data for transmission |
| Preprocessing | `resource_inspector.py` | Lists available datasets and models |
| Preprocessing | `status_reporter.py` | Reports execution status |
| Preprocessing | `cancellation_state.py` | Handles task interruption and cancellation |
| Communication | `websocket_server.py` | Manages asynchronous WebSocket communication |
| Communication | `deserialization_server.py` | Validates and routes incoming requests |
| Communication | `data_formatting_server.py` | Formats responses into JSON messages |

The `output/` folder is generated automatically at runtime and is therefore not included in this repository.


---

### 🕶️ Unity Client

Location:

```text
ImmersiveSHAPCLIENT/Assets/Scripts/
```

Main Unity scene:

```text
Sample Scene
```


| Module/File| Script | Description |
|---|---|---|
| Communication | `WebSocketClient.cs` | Establishes connection with the Python server |
| Communication | `DeserializationClient.cs` | Parses incoming messages |
| Communication | `DataFormattingClient.cs` | Structures the JSON messages sent to the server |
| Core | `PlotManager.cs` | Coordinates visualization workflow |
| Rendering | `DataInputManager.cs` / `PointFilter.cs` | Validates and filters incoming data |
| Rendering | `DataScalerAndAligner.cs` | Maps incoming data into 3D coordinates |
| Rendering | `VisualEncoder.cs` | Maps data attributes into visual encodings |
| Rendering | `GeometryBuilder.cs` | Generates point-cloud geometry |
| Rendering | `AxesAndReferenceBuilder.cs` | Creates axes, labels, and reference structures |
| Rendering | `RendererController.cs` | Executes rendering operations |
| Rendering | `SceneLayoutManager.cs` | Organizes scene layout |
| Rendering | `BoundsOptimizer.cs`, `SceneCleaner.cs` | Maintains rendering performance |
| UI | `VisualMappingUIManager.cs` | Manages immersive visualization controls |
| Interaction | `GlobalGraphManipulator.cs` | Supports bimanual scaling and rotation |
| Interaction | `PointSelection.cs`, `GlobalPointInteractor.cs` | Handles point selection and ray interactions |
| Interaction | `SelectionHighlighter.cs` | Highlights selected objects |
| Interaction | `DataPointMeta.cs`, `DataContentExtractor.cs` | Stores and retrieves metadata |
| Interaction | `TooltipManager.cs`, `TooltipUI.cs` | Displays contextual information |
| Interaction | `TooltipPinManager.cs`, `DataComparisonExtractor.cs` | Supports comparison and inspection tasks |
| UI | `ProcessStatusUI.cs` | Displays backend processing status |

---

# ⚙️ Requirements

## Python

- Python ≥ 3.10

## Unity

- Unity Editor 6000.0.29f1
- OpenXR enabled
- XR Interaction Toolkit
- Android Build Support

---

# 🚀 Installation

Clone repository:

```bash
git clone https://github.com/JulianaMontillaLopez/ImmersiveSHAP.git

cd ImmersiveSHAP
```

Create a Python virtual environment:

```bash
python -m venv venv
```

Activate environment:

Linux/Mac:

```bash
source venv/bin/activate
```

Windows:

```bash
venv\Scripts\activate
```

Install dependencies:

```bash
pip install -r requirements.txt
```

---

# ▶️ Execution

## Step 1: Run Python Server

Navigate to:

```text
ImmersiveSHAPSERVER/
```

Execute:

```bash
python main.py
```

If execution is successful, the terminal should display:

```text
🔌 WebSocket server iniciado en ws://0.0.0.0:8765
```

This indicates that the server is active and listening for incoming requests.

No modifications are required in:

```text
modules/communication/websocket_server.py
```

The server automatically listens on all available interfaces.

---

## Step 2: Run Unity Client

Open:

```text
ImmersiveSHAPCLIENT/
```

in Unity Hub.

Then:

1. Open project
2. Load the **Sample Scene**
3. Open:

```text
Assets/Scripts/Communication/WebSocketClient.cs
```

Configure endpoint:

```text
ws://<LOCAL_IP>:8765
```

Example:

```text
ws://192.168.1.50:8765
```

Save changes.

Press:

```text
Play
```

Ensure that both:

- Python server
- Unity client

are connected through the same local network.

---

## Step 3: Deploy to Meta Quest 3

Use the same endpoint configuration:

```text
Assets/Scripts/Communication/WebSocketClient.cs
```

```text
ws://<LOCAL_IP>:8765
```

Unity Build settings:

- Platform: Android
- Scripting Backend: IL2CPP
- Architecture: ARM64
- XR Plugin: OpenXR

Deploy using:

```text
Build and Run
```

Requirements:

- Meta Quest Developer Mode enabled
- PC and headset connected to same Wi-Fi network
- Firewall allowing port:

```text
8765
```

---

## 🎮 VR Interaction Summary

| Task | Input | Behavior |
|---|---|---|
| Selection | Trigger | Select data points |
| Inspection | Hover + Trigger | Show SHAP tooltip |
| Manipulation | Grip | Move and rotate visualization |
| Scaling | Dual Grip | Resize visualization |
| Navigation | Physical movement | Explore immersive environment |

---

## 🧪 Reproducibility Notes

- Datasets are automatically loaded through scikit-learn APIs
- No manual dataset installation is required
- SHAP values are computed during execution
- JSON messages generated by Python are consumed directly by Unity
- Dependency versions are fixed in `requirements.txt`

---

## 📌 Citation

If you use this repository in your research, please cite:

Montilla-López, J., Valencia, D., Jiménez-Builes, J. A., and Ramírez-González, G. A.

*ImmersiveSHAP: Immersive analytics visualization system for XAI using SHAP scatter plot.*

Publication information will be updated after publication.

---

## 📬 Contact

For questions regarding implementation or reproducibility, please contact the corresponding author.
