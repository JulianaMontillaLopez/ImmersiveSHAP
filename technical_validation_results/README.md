# Technical Validation Results – ImmersiveSHAP

This directory contains the experimental data and analysis materials used for the technical validation reported in:

**Manuscript:** *ImmersiveSHAP: Immersive analytics visualization system for XAI using SHAP scatter plot*  
**Submission ID:** 10596

The files correspond to runtime measurements collected during performance and scalability experiments executed on a Meta Quest 3 device.

---

## Experimental Overview

Technical validation was performed using:

- Iris dataset
- Breast Cancer dataset
- California Housing dataset

The California Housing dataset was additionally evaluated using different point-cloud sizes:

- 2,000
- 5,000
- 10,000
- 15,000
- 20,640

Each experiment was repeated three times:

- `p1` → repetition 1
- `p2` → repetition 2
- `p3` → repetition 3

Reported values in the manuscript were computed using aggregated statistics across repetitions.

---

## Data Acquisition

Runtime measurements were collected from both the Python backend and the Unity client running on a standalone Meta Quest 3 device.

Instrumentation included:

- Unity runtime logging
- Android Logcat
- Meta Quest Developer Hub (MQDH)
- Python execution logs

Logs were collected under real deployment conditions to reflect actual runtime behavior on target hardware.

Additional details regarding instrumentation and experimental procedures are provided in the manuscript.

---

## File Categories

### Python backend logs

```text
logPython_*.txt
```

Contains backend execution events and timing information such as preprocessing, model training, SHAP computation, and server execution stages.

### Unity runtime logs

```text
logUnity_*.txt
```

Contains client-side execution events including request handling, parsing, and rendering stages.

### Performance telemetry

```text
perf_*.csv
```

Contains exported runtime measurements including:

- FPS
- frame timing
- CPU usage
- memory statistics
- runtime telemetry

---

## File Naming Convention

Files follow the format:

```text
<source>_<dataset>_<size>_<trial>
```

Example:

```text
logPython_california_5000_p2.txt
```

Meaning:

- `logPython` → Python backend log
- `california` → California Housing dataset
- `5000` → point-cloud size
- `p2` → second repetition

---
## Analysis Scripts

The file `technical_validation_analysis_colab.ipynb` contains the Google Colab notebook used to process the collected logs and telemetry data, perform statistical analysis, and generate the figures and results reported in the manuscript.

## Reproducibility Workflow

1. Execute ImmersiveSHAP
2. Collect logs and telemetry
3. Store generated outputs
4. Execute analysis scripts
5. Reproduce figures and reported metrics