# Production Intelligence & Decision Support System (PIDSS)

## TECH STACK

### 1. Platform Layer (.NET)

Role: Scenario construction + orchestration

- ASP.NET Core Web API (Pidss.Platform)
- EF Core (optional — run metadata only)

Responsibilities
- ScenarioBuilder
- DataSources abstraction
- Adapter (canonicalization authority)
- Run orchestration
- Artifact management

### 2 Data Platform (Python + API)

Role: Data preparation (offline, pre-execution)

Components:

- Pidss.DataPlatform.Ingestion (Python)
- Pidss.DataPlatform.FeatureEngineering (Python)
- Pidss.DataPlatform.Calibration (Python)
- Pidss.DataPlatform.Synthetic.Mes.Generator (Python)
- Pidss.DataPlatform.Synthetic.Mes.Api (ASP.NET Core Web API)

Responsibilities

- Normalize raw data
- Generate features (aggregated, curated)
- Estimate system parameters (calibration profiles)
- Provide read access via API

Outputs

- Feature Store (Data Artifact)
- Calibration Profile Store (Model Artifact)

### 3. Execution Engines

Role: Execute scenario (runtime only)

Simulation

- C++ CLI (Pidss.Simulation)
- Built via .vcxproj
- Aggregate digital twin (NOT discrete-event)

Analytics

- Python CLI (Pidss.Analytics)
- pandas / numpy

Optimization

- Python CLI (Pidss.Optimization)
- Batch scenario exploration

### 4. UI Layer

- React (Pidss.Web.React)
- Runs in browser (SPA)

Responsibilities:

- Scenario input UI
- Run execution trigger
- KPI visualization
- Scenario comparison
- Recommendation display

CRITICAL:

- UI is API client ONLY  
- No business logic

### 5. Contracts & Data Format

- JSON
- JSON Schema Draft-07

### 6. Storage

| Type | Technology |
| --- | --- |
| Run Metadata | SQL Server / PostgreSQL |
| Artifacts | Filesystem (append-only) |
| Feature Store | Filesystem (JSON) |
| Calibration Store | Filesystem (JSON) |
| Model Store (future) | Filesystem |

### 7. Critical Architecture Rules

```
Engines ONLY consume canonical_scenario.json
Data Platform NEVER consumes canonical scenario
Platform is the ONLY layer that connects both worlds
```