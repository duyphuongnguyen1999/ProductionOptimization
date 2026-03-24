# Production Intelligence & Decision Support System (PIDSS)

## 1 Data Platform

Responsible for data preparation (offline / pre-scenario)

| Module | Responsibility |
| --- | --- |
| Pidss.DataPlatform.Ingestion | Data ingestion & normalization |
| Pidss.DataPlatform.FeatureEngineering	| Feature generation (aggregated / curated features) |
| Pidss.DataPlatform.Calibration | Parameter estimation → calibration profiles |
| Pidss.DataPlatform.Synthetic.Mes.Generator | Synthetic MES data generation (mock data source) |
| Pidss.DataPlatform.Synthetic.Mes.Api |Data access layer (API abstraction over MES data) |

Outputs

- Feature Store → Data Artifact (observed + aggregated)
- Calibration Profile Store → Model Artifact (estimated system parameters)

Notes (CRITICAL)
- Data Platform runs before execution pipeline
- Does NOT consume canonical scenario
- Produces inputs for ScenarioBuilder

## 2 Platform (.NET Core)

Responsible for scenario construction + orchestration

Core Modules
- ScenarioBuilder
- DataSources (read-only abstraction)
- Adapter (single authority for canonicalization)
- Run Orchestration
- API Layer

Responsibility Breakdown

| Component | Responsibility |
| --- | --- |
| ScenarioBuilder | Merge user input + feature store + calibration profile |
| DataSources | Read-only access to Feature Store & Calibration Store |
| Adapter	| Validation + versioning + canonicalization + stage_weights |
| Orchestrator | Run lifecycle, execution control, artifact management |

## 3 Execution Engines

Engine Definition:

An Engine is any component that:

- consumes canonical_scenario.json
- performs computation during execution pipeline
- produces run artifacts

Components that operate before scenario construction  
(e.g., ingestion, feature engineering, calibration)  
MUST NOT be classified as engines.

| Type | Component |
| --- | --- |
| Execution Engine | Simulation (C++) |
| Evaluation Engine	| Analytics (Python) |
| Search Engine | Optimization (Python, optional batch mode) |

Strict Rules

Engines MUST:

- read canonical_scenario.json only

Engines MUST NOT:

- read Feature Store
- read Calibration Store
- read raw data
- parse public schema
- perform adaptation

## 4 Mapping — Conceptual → Implementation

| Concept | Implementation |
| --- | --- |
| Data Preparation | Pidss.DataPlatform.* |
| Feature Engineering | Pidss.DataPlatform.FeatureEngineering |
| Calibration (Parameter Estimation) | Pidss.DataPlatform.Calibration |
| Data Source (MES) | Pidss.DataPlatform.Synthetic.Mes.Api |
| Data Generator | Pidss.DataPlatform.Synthetic.Mes.Generator |
| Scenario Construction	| Pidss.Platform / ScenarioBuilder |
| Canonicalization | Pidss.Platform / Adapter |
| Simulation Engine | Pidss.Simulation (C++) |
| Analytics Engine | Pidss.Analytics (Python) |
| Optimization Engine	| Pidss.Optimization (Python) |
