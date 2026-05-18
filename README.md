# ImmersiveSHAP: Immersive Visualization System for Explainable Artificial Intelligence

## Manuscript Information

**Title:** *ImmersiveSHAP: Immersive analytics visualization system for XAI using SHAP scatter plot*  
**Submission ID:** 10596  

### Authors

- **Juliana Andrea Montilla López** — Universidad del Cauca  
- **Daniel Valencia Medina** — Universidad del Cauca  
- **Jovani Alberto Jiménez Builes** — Universidad Nacional de Colombia, Medellín Campus  
- **Gustavo Adolfo Ramírez González** — Universidad del Cauca  

---

## Repository Overview

This repository contains the source code and computational resources associated with **ImmersiveSHAP**, an immersive visualization system designed to support Explainable Artificial Intelligence (XAI) analysis in Virtual Reality (VR).

The system transforms SHAP (SHapley Additive exPlanations) outputs into three-dimensional visual representations to support the exploration of multidimensional relationships and feature interactions in immersive environments.

ImmersiveSHAP follows a distributed Client–Server architecture based on asynchronous WebSocket communication.

The implementation consists of two primary components:

### Backend (Python Server)

Responsible for data processing and explanation generation.

Main functionalities include:

- Loading and preprocessing tabular datasets
- Training predictive models
- Computing SHAP-based explanation metrics
- Identifying feature interactions
- Serializing processed data into JSON format for visualization

### Frontend (Unity Client)

Responsible for immersive visualization and interaction.

Main functionalities include:

- Receiving and deserializing processed data
- Mapping data into three-dimensional visual structures
- Rendering interactive point-based visualizations
- Managing user interaction using VR controllers

---

## Repository Structure

The repository is organized into backend and frontend modules.

### Python Server (Backend)

Contains the data processing pipeline and communication services.

#### Main Entry

- `main.py`  
Initializes the asynchronous server and manages communication with connected clients.

#### Communication Module

`modules/communication/`

- `websocket_server.py`
- `deserialization_server.py`
- `data_formatting_server.py`

Handles message exchange and serialization processes.

#### Preprocessing Module

`modules/preprocessing/`

- `load_data.py`
- `preprocessing_data.py`
- `train_model.py`
- `explanation_generation.py`
- `generate_scatter_plot.py`
- `plot_dispatcher.py`
- `data_export.py`

Performs data preparation, model training, SHAP computation, and visualization data generation.

Additional utility modules:

- `resource_inspector.py`
- `request_manager.py`
- `status_reporter.py`

---

### Unity Client (Frontend)

Contains the scripts responsible for rendering and interaction.

#### Networking

- `WebSocketClient.cs`
- `DeserializationClient.cs`
- `DeserializedData.cs`

#### Rendering and Layout

- `PlotManager.cs`
- `SceneLayoutManager.cs`
- `GeometryBuilder.cs`
- `RendererController.cs`
- `VisualEncoder.cs`
- `DataScalerAndAligner.cs`
- `AxesAndReferenceBuilder.cs`
- `Axis.cs`

#### VR Interaction

- `GlobalPointInteractor.cs`
- `PointSelection.cs`
- `TooltipManager.cs`
- `TooltipUI.cs`
- `TooltipPinManager.cs`

---

## System Requirements

### Python Environment

Recommended:

- Python ≥ 3.10

Required packages:

```bash
websockets
shap
xgboost
scikit-learn
pandas
numpy
```

Install dependencies:

```bash
pip install websockets shap xgboost scikit-learn pandas numpy
```

### Unity Environment

- Unity Editor `6000.0.29f1`
- XR Interaction Toolkit ≥ `3.0.8`
- Android Build Support
- Meta Quest 3 headset (for standalone deployment)

---

## Installation

Clone the repository:

```bash
git clone https://github.com/your-username/ImmersiveSHAP.git

cd ImmersiveSHAP
```

Create and activate a Python virtual environment.

Install required dependencies.

---

## Reproducing System Execution

### Step 1: Run the Python Server

Navigate to the project directory and execute:

```bash
python main.py
```

The server should initialize and begin listening for client requests.

---

### Step 2: Execute the Unity Client

For development and testing:

1. Open Unity Hub
2. Add project from disk
3. Open the main scene
4. Configure:

```text
ws://localhost:8765
```

5. Press Play

---

### Step 3: Deploy to Meta Quest 3

#### Network Configuration

Replace:

```text
ws://localhost:8765
```

with:

```text
ws://<LOCAL_IP>:8765
```

Ensure that the PC and headset are connected to the same local network.

#### Build Configuration

1. Go to **File → Build Settings**
2. Select **Android**
3. Click **Switch Platform**
4. Enable **Oculus** under XR Plug-in Management

Build and deploy the application.

---

## VR Interaction Summary

| Task | Interaction | Description |
|---|---:|---|
| Pointer interaction | Wrist movement | Interacts with interface elements |
| Menu selection | Trigger | Select interface components |
| Data inspection | Trigger | Displays point information |
| Plot manipulation | Grip | Translate or rotate visualizations |
| Scaling | Dual grip | Resize visual structures |

---

## Contact

For questions regarding implementation or reproducibility, please contact the corresponding authors.

---

## Citation

If you use this repository in your research, please cite the associated manuscript:

Montilla-López, J., Valencia, D., Jimenez-Builes, Jovani A., and Ramírez-González, G. A.  
*ImmersiveSHAP: Immersive analytics visualization system for XAI using SHAP scatter plot.*

Publication details will be updated after publication.
