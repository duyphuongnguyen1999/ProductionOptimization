# 🔷 MASTER PROMPT — Production Intelligence & Decision Support System (PIDSS)

You are acting as a Senior Software Architect + Data Platform Engineer.

Your task is to build a Production Intelligence & Decision Support System (PIDSS) for manufacturing optimization.

This system is:

- Windows-first
- Visual Studio-first
- On-prem friendly
- Run-based, append-only
- Decision-support only (NOT MES, NOT ERP, NOT PLC control)
- Equipment-centric (Stage is SOP identity only)
- Canonical execution model internally
- Versioned public JSON contracts
- Adapter-based architecture (Platform handles versioning)

---

# 1️. SYSTEM POSITIONING

PIDSS is a Decision Support & Intelligence Layer that sits above MES/ERP/SCADA.

It:

- Ingests observed production data (CSV export)
- Accepts scenario input (what-if)
- Runs aggregate simulation (C++)
- Runs analytics & ROI evaluation (Python)
- Produces explainable recommendations
- Stores run-based artifacts immutably

It DOES NOT:

- Dispatch tasks
- Control machines
- Track WIP per product
- Perform scheduling at minute resolution
- Replace MES
- Execute real-time routing or PLC logic

---

# 2️. CORE BUSINESS PROBLEM

Goal:
Increase manufacturing capacity by ~50% in 5 years without increasing:

- factory footprint
- headcount

Strategies evaluated:

- Labor optimization
- Downtime reduction
- Defect reduction
- Semi-automation
- Full automation
- Stage integration (equipment covering multiple SOP stages)
- Equipment replacement & lifecycle optimization

The system must support financial evaluation:

- CAPEX
- OPEX
- Useful life
- ROI
- Payback period
- Footprint reduction impact

---

# 3. REAL FACTORY CONTEXT (FINALIZED DOMAIN REALITY)

## 3.1. Process

Transforms raw materials → component or final product.  

Defined by SOP. Rarely changes.

A factory may contain multiple processes.

Some processes produce:
- Components (semi-finished goods)
- Final products (assembly / packaging)

## 3.2. Component

Output of a process.

Consumed by downstream process via BOM.

Modeled in aggregate only (no WIP tracking).

## 3.3. Product (Final Product)

Assembled or packaged from multiple components.

Final production capacity is constrained by:

- Final assembly capacity
- Upstream component availability

## 3.4. BOM (Bill of Materials)

Defines:

- product - component mapping
- quantity_required_per_product

Final output limited by minimum component availability.

Important:

BOM NOT directly in scenario

→ must be transformed by ScenarioBuilder

## 3.5. Stage

A stable SOP step within a process.

Stage represents business traceability and comparability.

Stage contains:

- stage_id
- order
- name

Critical rule:

> Stage identity MUST NEVER be deleted  
Stage identity MUST NEVER be replaced by automation.  
Stage contains NO execution logic.


## 3.6. Work Unit (Execution Unit)

Execution is equipment-centric.

A WorkUnit may represent:

- Manual workbench
- Semi-automatic machine
- Fully automatic machine

Each WorkUnit defines:

- unit_id
- unit_type (manual / semi_auto / auto)
- covered_stage_ids[] (minItems = 1)
- count
- cycle_time distribution
- operators_per_unit
- requires_operator_presence
- reliability (optional)
- footprint_m2 (optional)
- financial attributes (optional)

## 3.7. Integration Concept

Integrated cell is NOT a separate type.

Integration is defined by:

- `covered_stage_ids.length` > 1

If multiple stages are covered, then:

- An `integration` object must exist
- Adapter MUST compute `stage_weights` 
- `stage_weights` MUST be explicitly materialized in canonical

## 3.8 Line

Logical replication of process.

Important:
- Resources are NOT necessarily 1:1 with lines.  
- Capacity must be modeled at stage resource pool level.

### 3.8.1. Example: Assembly Process (7 Stages)

1. Pressing (semi-auto) – 6–7 sec
2. Welding (semi-auto) – 7–8 sec
3. Manual assembly – ~12 sec
4. Manual connection – 15–17 sec
5. Manual coating – 15–17 sec
6. Silicon processing (semi-auto) – ~20 sec
7. Visual inspection (manual) – ~12 sec

Manual stations:
- Parallel workbenches
- 1–4 operators per bench

Semi-auto:
- Machine-human coupling
- Machine may wait for operator
- Operator may wait for machine

### 3.8.2. Line vs Stage Capacity Reality

There are 7 lines.

However:

- Pressing & Heating: exactly 7 machines
- Other stages: more than 7 workstations

Therefore:

> Capacity constraints must be modeled at stage resource pools,
> NOT at fixed line mapping.

## 3.9. Batch Flow Reality

Current production flow is batch-gated:

- Batch size: 600 pieces
- Transfer only after full batch completion
- Transfer delay: 3–5 minutes (checksheet/confirmation)

Automation goal includes:

- Reducing transfer delay
- Reducing labor
- Reducing footprint
- Increasing throughput

## 3.10. Critical Rule — Integrated Automated Cell

When one automated cell integrates multiple stages:

- DO NOT create new SOP stages
- DO NOT delete original stage identity
- Model automation as execution override
- Preserve stage-level comparability

This ensures:

- A/B comparison validity
- SOP traceability
- Bottleneck reporting consistency

---

# 4. DOMAIN MODEL (CRITICAL — FINALIZED)

## 4.1. Stage-Centric SOP Identity

- Stage = SOP step
- Stage NEVER deleted
- Stage NEVER converted
- Stage contains NO execution logic
- Stage only defines:
	- `stage_id`
	- `order`
	- `name`

## 4.2. Equipment-Centric Execution (Core Design Principle)

Execution is defined by Work Units (Equipment Units).

Rules:

- `covered_stage_ids` is mandatory
- Single-stage = one element array
- Multi-stage = integrated execution
- `stage_id` field is NOT used

Automation level (manual/semi_auto/auto) is independent from integration scope.

## 4.3. Automation Scenario Modeling Rules

Automation scenarios must respect realistic manufacturing constraints.

The system must prevent unrealistic automation modeling.

Rules:

### 1. Stage Identity Preservation

Automation must NEVER delete or replace Stage definitions.

Stages represent SOP traceability and historical comparability.

Automation is modeled only through WorkUnit execution capacity.

### 2. Integrated Cell Representation

If an automated cell covers multiple SOP stages:

- covered_stage_ids must include all affected stages
- integration object must exist
- stage_weights must be materialized in canonical scenario

### 3. Batch Compatibility

Automation scenarios must consider batch compatibility across stages.

Large automation batches may cause:

- downstream blocking
- WIP explosion
- flow instability

PIDSS must evaluate automation batch size against downstream batch policy.

### 4. Automation Requires System-Level Evaluation

Automation must never be evaluated at machine level only.

Simulation and analytics must evaluate:

- upstream supply capability
- downstream capacity
- WIP accumulation
- footprint impact
- labor redistribution

Automation scenarios must therefore be evaluated at full process level.

## 4.4. Stage Attribution (Integrated Units)

When a WorkUnit covers multiple stages:

- Platform Adapter must compute `stage_weights`
- `stage_weights` must sum to 1
- Engines must not compute attribution logic

Attribution ensures:

- Bottleneck reporting per stage
- A/B comparability
- Traceability preservation

## 4.5 Reliability & Lifecycle Modeling

Each WorkUnit may include:

- mtbf_hours
- mttr_minutes
- age_years (optional)
- useful_life_years
- degradation model (optional)

Simulation must compute expected availability.

Analytics may compute:

- Replacement priority ranking
- ROI of replacement
- Capacity gain from improved reliability

## 4.6. Flow Model

PIDSS uses aggregate flow simulation, not discrete-event simulation.

The model includes:

- batch size policy
- stage capacity
- transfer delay
- reliability impact
- break behavior

Simulation must capture system-level flow dynamics while remaining computationally simple.

Key Principles

- No discrete-event queue simulation
- No per-product tracking
- No MES-level dispatching logic

However:

```
Aggregate WIP between stages MUST be modeled.
```

This allows detection of:

- blocking
- starvation
- WIP accumulation
- lead time increase

This enables system-level analysis using flow theory including Little's Law.

Little's Law relationship:

```
Lead Time ≈ WIP / Throughput
```

## 4.7. Production Footprint Model

Factory footprint is a hard constraint in many manufacturing environments.

PIDSS must evaluate automation scenarios not only by throughput and ROI, but also by production floor utilization.

Production footprint includes:

```
Total Production Area =
    (Machine Area + WIP Buffer Area) × Layout Factor
```

Where:

### 4.7.1. Machine Area
```
Machine Area =
    Σ(machine_count × machine_footprint_m2)
```

This typically represents 60–80% of production floor area.

### 4.7.2. WIP Buffer Area

WIP buffers store intermediate products between stages.

Although smaller than machine area, WIP buffers may reach:

```
10–30% of production floor area
```

when batch sizes or flow imbalance increase.

WIP buffer area is calculated as:

```
WIP_area_stage =
    WIP_stage × unit_buffer_area
```

Where:

- `WIP_stage` = average units waiting between stages
- `unit_buffer_area` = storage footprint per unit

Total WIP area:
```
Total_WIP_area = Σ WIP_area_stage
```

### 4.7.3. Layout Factor

Factory layouts require space for:

- aisles
- operator movement
- maintenance access

Therefore a layout multiplier is applied:

```
Layout Factor = 1.2 – 1.4
```

Final production footprint:

```
Production_Footprint =
    (Machine_Area + WIP_Area) × Layout_Factor
```

## 4.8. WIP ESTIMATION MODEL

WIP is estimated at stage boundaries using aggregate flow metrics.

For each stage:

```
WIP_stage ≈ Throughput × Effective_Wait_Time
```

Effective wait time includes:

- batch gating delay
- transfer delay
- downstream congestion

This enables WIP estimation without discrete-event simulation.

## 4.9 BATCH FLOW DYNAMICS

Batch policy strongly influences WIP and footprint.

Example:

Batch transfer:

```
batch = 600
```

Buffer range:

```
0 – 600
average ≈ 300
```

Large automation batches may cause:

```
batch_auto = 3000
batch_downstream = 600
```

This causes:

- large buffer accumulation
- unstable flow
- excessive footprint usage

PIDSS must detect and analyze this scenario.

## 4.10. Planning Model

Demand defined by:

- PlanningPeriod:
	- start_time
	- end_time
	- target_output_qty

- PlanningCalendar:

	- Shifts
	- Break definitions
	- Working days

- Break behavior:

	- manual stops during break
	- semi_auto stops during break
	- auto may continue if requires_operator_presence=false

---

# 5. SYSTEM FAILURE MODES

PIDSS must detect and analyze system-level automation failure modes that frequently occur in real manufacturing environments.

The purpose is to prevent local optimization (machine-level) that harms system-level performance.

Failure detection is performed by:

- C++ Simulator → generating raw metrics
- Python Analytics → detecting failure patterns
- Recommender → generating corrective actions

The following 10 failure modes MUST be supported in PIDSS v1.

## 5.1. Downstream Blocking

Automation increases upstream output beyond downstream capacity.

Effects:

- Buffer WIP increases
- Upstream machine becomes blocked
- Effective utilization drops

Detected using:

- blocking_time
- downstream utilization
- WIP accumulation rate

Possible recommendations:

- increase downstream capacity
- reduce auto batch size
- allow partial batch transfer
- add intermediate buffer

## 5.2. Upstream Starvation

Automated machine requires high input rate but upstream cannot supply enough material.

Effects:

- auto machine idle time
- utilization below expected level

Detected using:

- starvation_time
- upstream utilization near 100%
- auto utilization below threshold

Possible recommendations:

- increase upstream machines
- reduce auto batch size
- redesign feeding logic

## 5.3. Batch Size Mismatch

Batch sizes between stages are incompatible.

Example:

```
auto stage batch = 3000
downstream stage batch = 600
```

Effects:

- transfer delay
- uneven WIP accumulation
- flow instability

Possible recommendations:

- harmonize batch sizes
- split batches
- change auto machine batch policy

## 5.4. Bottleneck Migration

Automation removes an existing bottleneck but creates a new one downstream.

Effects:

- throughput increase smaller than expected
- system bottleneck shifts to another stage

Detected using:

- bottleneck stage change
- marginal throughput gain analysis

Possible recommendations:

- reinforce new bottleneck stage
- multi-stage automation

## 5.5. WIP Explosion (Lead Time Increase)

Flow imbalance causes uncontrolled WIP growth.

According to Little’s Law:

```
Lead Time ≈ WIP / Throughput
```

Effects:

- production lead time increases
- inventory cost increases

Detected using:

- WIP accumulation
- WIP / throughput ratio

Possible recommendations:

- rebalance stage capacity
- reduce batch sizes
- introduce intermediate buffers

## 5.6. Reliability Dominance

Highly automated machines may have lower reliability.

Effects:

- downtime dominates system throughput
- production volatility increases

Detected using:

- availability analysis
- MTBF / MTTR impact simulation

Possible recommendations:

- redundancy machines
- preventive maintenance strategy
- hybrid manual fallback

## 5.7. Single Point of Failure

Integrated automated cell covers multiple stages.

If the cell fails:

- multiple SOP stages stop simultaneously
- system resilience decreases

Detected using:

- stage coverage by single unit
- lack of redundancy

Possible recommendations:

- parallel automation cells
- maintain legacy backup equipment

## 5.8. Footprint Constraint Violation

Automation scenario exceeds factory space limit.

Detected using:

- total_footprint_m2 > factory_footprint_limit

Possible recommendations:

- retire legacy machines
- replace multiple benches with integrated cell
- optimize layout

## 5.9. Labor Utilization Imbalance

Automation changes labor requirements unevenly across stages.

Effects:

- operator idle time
- operator overload in other stages

Detected using:

- operator utilization variance
- staffing imbalance

Possible recommendations:

- reallocate operators
- cross-skill training
- rebalance staffing levels

## 5.10. ROI Illusion

Automation increases capacity beyond actual demand.

Effects:

- low equipment utilization
- long payback period

Detected using:

```
system_capacity >> demand
```

Possible recommendations:

- postpone investment
- smaller automation cell
- phased deployment strategy

---

# 6. CORE ARCHITECTURE

## 6.1. System Overview

PIDSS (Production Intelligence & Decision Support System) is:

- Decision-support only (NOT MES / ERP / PLC)
- Scenario-driven (NOT data-driven)
- Run-based, append-only
- Canonical execution model internally
- Versioned public JSON contracts
- Adapter-controlled architecture
- Equipment-centric simulation

### Core Philosophy

```
- Scenario represents system behavior
- Data represents observation
- Canonical Scenario = Single Source of Truth for ALL engines
```

### Key Implications

| Concept | Meaning |
| --- | --- |
| Data | Observed reality |
| Feature Store | Curated observation |
| Calibration Profile | Modeled system behavior |
| Scenario | Hypothetical decision space |
| Canonical Scenario | Executable truth |

---

## 6.2. System Modules (Refactored — Boundary Clean)

PIDSS consists of 3 main layers:

```
Data Platform → Platform → Execution Engines
```

### 6.2.1 Data Platform

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

### 6.2.2 Platform (.NET Core)

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

### 6.2.3 Execution Engines

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

### 6.2.4 Mapping — Conceptual → Implementation

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

---

## 6.3. Data Flow

### 6.3.1. Data Platform Pipeline

```
Synthetic MES / CSV / External Source
        ↓
[Ingestion]
        ↓
[Feature Engineering]
        ↓
Feature Store (versioned)
        ↓
[Calibration Engine]
        ↓
Calibration Profile Store (versioned)
```

### 6.3.2 Execution Pipeline (FINAL — FIXED)

```
User Input
        ↓
[ScenarioBuilder]
        ↓
Scenario Snapshot (enriched, still PUBLIC schema)
        ↓
[Adapter]
        ↓
Canonical Scenario (ONLY HERE)
        ↓
Simulation → Analytics → Optimization
```

---

## 6.4. HARD ARCHITECTURAL RULES

### 6.4.1 Scenario Authority

```
Scenario is authoritative
Data sources are NOT authoritative
```

### 6.4.2 Engine Isolation Rules

Engines MUST:

- read `canonical_scenario.json` ONLY

Engines MUST NOT:

- read Feature Store
- read Calibration Store
- read raw data
- parse public schema
- perform adaptation

### 6.4.3 ScenarioBuilder Rules

ScenarioBuilder MUST:

- read ONLY via DataSources
- merge:
	- user input
	- feature store
	- calibration profile
- output:
	- `scenario_snapshot.json` (enriched, PUBLIC schema)

ScenarioBuilder MUST NOT:

- produce canonical model
- perform schema validation
- perform versioning
- fit model
- estimate distribution
- infer parameters

### 6.4.4 Adapter Rules (SINGLE AUTHORITY — CRITICAL)

Adapter is the ONLY components that can:

- validate schema
- perform version adaptation
- canonicalize scenario
- compute `stage_weights`

```
ScenarioBuilder MUST NOT produce canonical model
Adapter is the ONLY component allowed to produce canonical_scenario.json
```

---

## 6.5. DATA ACCESS LAYER (DataSources)

### 6.5.1 Purpose

DataSources = read-only abstraction layer giữa Platform và Data Platform

### 6.5.2 Interfaces (conceptual)

- `IFeatureStoreReader`
- `ICalibrationProfileProvider`

### 6.5.3 STRICT RULES

DataSources MUST NOT:

- call external APIs directly
- contain business logic
- perform transformation
- join / aggregate / derive data

DataSources ONLY:

- read from storage
- map to DTO

### 6.5.4 Architectural Benefit

Ensures:

- ScenarioBuilder does not be data pipeline
- No modeling logic leak
- Explicit separation:

```
Data Platform → prepare data
Platform → build scenario
Engines → evaluate scenario
```

---

## 6.6. DATA PLATFORM STORAGE (PHYSICAL vs LOGICAL)

### 6.6.1 Physical Storage

```
data_storage/
├─ feature_store/
│   └─ {feature_set_id}.json
│
├─ calibration_store/
│   ├─ profiles/
│   │   └─ {profile_id}.json
│   └─ index.json
│
├─ model_store/
├─ dataset_registry/
```

### 6.6.2 Logical Classification

Feature Store

- Type: Data Artifact
- Produced by: Feature Engineering
- Contains:
	- aggregated features
	- throughput history
	- demand patterns
	- process structure (optional)

Calibration Profile Store

```
Calibration Profile is NOT a data artifact
Calibration Profile is a MODEL ARTIFACT
```

- Produced by: Calibration Engine
- Contains:
	- estimated parameters
	- distributions
	- system behavior model

### 6.6.3 Conceptual Separation

```
Feature Store = Observed Reality
Calibration Profile = Interpreted Reality
Scenario = Hypothetical Reality
```

---

## 6.7. SCENARIO LIFECYCLE 

### 6.7.1 Pipeline

```
User Input
   ↓
ScenarioBuilder
   ↓
Scenario Snapshot (public-like, enriched)
   ↓
Adapter
   ↓
Canonical Scenario (internal, deterministic)
```

### 6.7.2 Scenario Snapshot

- Immutable
- Stored immediately
- Contains:
	- user input
	- enriched data (feature + calibration reference)
- STILL follows PUBLIC schema

```
Snapshot = audit truth
Canonical = execution truth
```

### 6.7.3 Canonical Scenario

- Internal only
- Engine-facing
- Fully normalized
- Deterministic
- No version ambiguity

---

## 6.8. ARCHITECTURE DETAILS

## 6.8.1. Public JSON Contracts (Versioned)

Public input:

- `scenario.schema.json`
- `schema_version`

Public outputs:

- simulation_result.json
- production_records.csv
- analysis_response.json
- recommendation.json

## 6.8.2. Canonical Scenario (Internal Execution Model)

Platform must:

- Validate public scenario against schema
- Adapt version → canonical model
- Compute stage_weights if needed
- Output canonical_scenario.json

Canonical Scenario:

- No version ambiguity
- No oneOf
- No nullable ambiguity
- covered_stage_ids always array
- Explicit integration object when multi-stage
- Engines consume canonical only

Engines (C++ & Python) only read canonical format.

## 6.8.3. Adapter Strategy

Only Platform (.NET) handles:

- schema validation
- version adaptation
- canonicalization
- Stage weight computation
- Normalization

Simulator & Analytics:

- NEVER parse public schema versions
- NEVER handle version branching

## 6.8.4. Run-Based Execution Model

Each run:

- run_id (UUID v4)
- immutable
- artifacts stored under:

```graphql
artifacts/{run_id}/
   scenario_snapshot.json
   canonical_scenario.json
   simulation_result.json
   production_records.csv
   analysis_response.json
   recommendation.json
   artifact_manifest.json
   logs/
```

Append-only. Never overwrite.

## 6.8.5 Deterministic Simulation Requirement

Simulation must support deterministic execution.

Each canonical scenario must contain a `random_seed` field.

If the same canonical scenario and seed are used,
the simulator MUST produce identical outputs.

Purpose:

- reproducibility
- regression testing
- reliable A/B comparison

## 6.8.6 Artifact Manifest & Engine Version Tracking

Each run must generate:

artifact_manifest.json

The manifest records:

- run_id
- artifact list
- artifact creation timestamps
- simulator_version
- analytics_version

Purpose:

- run reproducibility
- artifact lineage tracking
- debugging support
- comparison traceability

---

## 6.9. PLANNING MODEL (EXECUTION CONTEXT)

Demand Definition

- PlanningPeriod:
	- start_time
	- end_time
	- target_output_qty

Calendar

- shifts
- breaks
- working days

Break Behavior

| Type | Behavior |
| --- | --- |
| manual | stops |
| semi_auto	| stops |
| auto | may continue if no operator required |

---

# 7. TECH STACK

## 7.1. Platform Layer (.NET)

Role: Scenario construction + orchestration

- ASP.NET Core Web API (Pidss.Platform)
- EF Core (optional — run metadata only)

Responsibilities
- ScenarioBuilder
- DataSources abstraction
- Adapter (canonicalization authority)
- Run orchestration
- Artifact management

## 7.2 Data Platform (Python + API)

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

## 7.3. Execution Engines

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

## 7.4. UI Layer

WinForms (.NET) (Pidss.Desktop.Winforms)

## 7.5. Contracts & Data Format

- JSON
- JSON Schema Draft-07

## 7.6. Storage

| Type | Technology |
| --- | --- |
| Run Metadata | SQL Server / PostgreSQL |
| Artifacts | Filesystem (append-only) |
| Feature Store | Filesystem (JSON) |
| Calibration Store | Filesystem (JSON) |
| Model Store (future) | Filesystem |

## 7.7. Critical Architecture Rules

```
Engines ONLY consume canonical_scenario.json
Data Platform NEVER consumes canonical scenario
Platform is the ONLY layer that connects both worlds
```

---

# 8. REPOSITORY STRUCTURE

```graphql
ProductionOptimization/
├─ data/                              # Data governance layer
│   ├─ contracts/
│   ├─ schemas/
│   ├─ validation/
│   ├─ transforms/
│   ├─ lineage/
│   └─ documentation/
|
├─ platform/
│   └─ Pidss.Platform/
│       ├─ Api/
│       │   ├─ Controllers/
│       │   ├─ Contracts/
│       │   ├─ Middleware/
│       │   └─ Filters/
│       ├─ Application/               # Use cases / orchestration
│       ├─ ScenarioBuilder/           # Scenario construction logic
│       ├─ DataSources/               # Read-only abstraction layer
│       └─ Adapters/                  # Canonicalization authority
|
├─ data_platform/
│   ├─ ingestion/
│   │   └─ Pidss.DataPlatform.Ingestion/
│   ├─ feature_engineering/
│   │   └─ Pidss.DataPlatform.FeatureEngineering/
│   ├─ calibration/
│   │   └─ Pidss.DataPlatform.Calibration/
│   └─ synthetic/
│       ├─ demand/                   # future
│       ├─ telemetry/                # future
│       └─ mes/
│           ├─ Pidss.DataPlatform.Synthetic.Mes.Generator/
│           └─ Pidss.DataPlatform.Synthetic.Mes.Api/
|
├─ engines/                           # EXECUTION ONLY
│   ├─ simulation/
│   │   └─ Pidss.Simulation/
│   ├─ analytics/
│   │   └─ Pidss.Analytics/
│   └─ optimization/
│       └─ Pidss.Optimization/
|
├─ presentation/
│   └─ Pidss.Desktop.Winforms/
|
├─ artifacts/                         # RUN-BASED IMMUTABLE STORAGE
│   └─ {run_id}/
│        ├─ scenario_snapshot.json
│        ├─ canonical_scenario.json
│        ├─ simulation_result.json
│        ├─ production_records.csv
│        ├─ analysis_response.json
│        ├─ recommendation.json
│        ├─ artifact_manifest.json
│        └─ logs/
│           ├─ platform.log
│           ├─ simulator.log
│           └─ analytics.log
|
├─ data_storage/                      # DATA PLATFORM OUTPUTS
│   ├─ feature_store/
│   │   └─ {feature_set_id}.json
│   ├─ calibration_store/
│   │   ├─ profiles/
│   │   │   └─ {profile_id}.json
│   │   └─ index.json
│   ├─ model_store/                  # future ML
│   │   └─ {model_id}/
│   │       ├─ model.pkl
│   │       ├─ metadata.json
│   │       └─ metrics.json
│   └─ dataset_registry/             # lineage & catalog
|
└─ docs/
```

---

# 9. DATA LAYER STRUCTURE

## 9.1. Purpose

Data governance is centered around `data/` folder, which contains all versioned contracts, 
schemas, validation logic, transformation scripts, lineage policies, and documentation.

`data/` = governance layer, NOT runtime storage

```
data/ defines HOW data should look
data_storage/ contains ACTUAL data artifacts
```

## 9.2. Structure

``` graphql
data/
 ├─ contracts/        # Example payloads (public API)
 ├─ schemas/          # JSON Schema definitions
 ├─ validation/       # Validation logic/tests
 ├─ transforms/       # Analytical transforms (offline)
 ├─ lineage/          # Metadata + traceability rules
 └─ documentation/    # Domain + versioning docs
```

## 9.3. Definitions:

| Folder | Purpose |
| --- | --- |
| contracts | Example scenarios & outputs |
| schemas | Validation rules (Draft-07) |
| validation | Schema + semantic validation |
| transforms | Offline transformations (NOT runtime) |
| lineage | Artifact tracking policies |
| documentation | Domain explanation |

## 9.4. Critical Rule 

```
Adapters MUST NOT exist in data/
Adapters belong ONLY to Platform
```

---

# 10. DATABASE (RUN METADATA ONLY)

## 10.1. Purpose

Database is used ONLY for:

- Run tracking
- Job orchestration
- Artifact indexing
- KPI indexing (for query)

## 10.2. Tables:

- `runs`
- `jobs`
- `run_artifacts`
- `run_metrics`
- `run_recommendations`

## 10.3. Strict Rules

Domain execution data NOT stored relationally. 

All domain data must stored as: 

- JSON artifacts.
- CSV outputs

## 10.4. Artifact Classification

| Artifact Type | Example | Owner |
| --- | --- | --- |
| Data Artifact	| Feature Store | Data Platform |
| Model Artifact | Calibration Profile | Data Platform |
| Scenario Artifact | Scenario Snapshot | Platform |
| Execution Artifact | Simulation Result | Engines |
| Analysis Artifact	| KPI / Recommendation | Analytics |

## 10.5. System Reality Model (NEW — cực mạnh khi interview)

```
Feature Store = Observed Reality
Calibration Profile = Interpreted Reality (Model)
Scenario = Hypothetical Reality (Decision Space)
Canonical Scenario = Executable Reality
```

## 10.6. Layer Isolation Guarantee

```
Data Platform → NO knowledge of execution
Engines → NO knowledge of data source
Platform → ONLY integration point
```

---

# 11. JOB ORCHESTRATION

## 11.1. PIDSS Run Pipeline (Final Architecture-Aligned Version)

This pipeline reflects the actual PIDSS architecture:

- Platform (.NET) = single orchestration authority
- ScenarioBuilder = data → scenario construction
- Adapter = canonicalization authority (ONLY place)
- Engines = pure execution (canonical only)
- Data Platform = pre-execution only (NOT in runtime pipeline)

---

## 11.2. End-to-End Execution Flow

```
User Input
   ↓
ScenarioBuilder (via DataSources)
   ↓
Scenario Snapshot (PUBLIC schema, enriched)
   ↓
Adapter (validation + canonicalization)
   ↓
Canonical Scenario
   ↓
Simulation (C++)
   ↓
Analytics (Python)
   ↓
Artifacts + Recommendations
```

---

## 11.3 Detailed Run Pipeline

### 1. Ingest & Run Creation (Platform)

#### 1. POST /runs receives:

- user scenario input
- `schema_version`
- optional `calibration_profile_id`

#### 2. Create run metadata:

- `run_id` (UUID v4)
- status = `Created`

#### 3. Initialize jobs:

- Simulation → Pending
- Analytics → Pending

#### 4. Persist raw snapshot immediately:

```
artifacts/{run_id}/scenario_snapshot.json
```

#### Rule:

```
Snapshot MUST be stored BEFORE any transformation
```

---

### 2. Scenario Construction (ScenarioBuilder + DataSources)

#### 1. Data Access (STRICT)

ScenarioBuilder reads ONLY via:

- IFeatureStoreReader
- ICalibrationProfileProvider

#### 2. Data Used

- Feature Store (aggregated features)
- Calibration Profile (model artifact)
- User input

#### 3. Responsibilities

ScenarioBuilder MUST:

- merge:
	- user input
	- feature data
	- calibration profile
- derive:
	- capacity hints
	- process structure (if needed)
	- demand shaping

#### 4. Output

```
scenario_snapshot.json (ENRICHED)
```

Still:

- PUBLIC schema compliant
- NOT canonical

#### Critical Rules

ScenarioBuilder MUST NOT:
- produce canonical model
- validate schema
- perform versioning
- estimate parameters
- fit models

---

### 3. Validation & Canonicalization (Adapter — SINGLE AUTHORITY)

#### 1. Schema Validation

- Validate snapshot against:
	- `scenario.schema.json`
	- using `schema_version`

#### 2. Version Adaptation

- Convert public schema → internal representation

#### 3. Canonicalization
- Normalize structure
- Remove ambiguity
- Ensure:
	- covered_stage_ids always array
	- integration object exists (if multi-stage)
	- no nullable ambiguity

#### 4. Stage Attribution

- Compute:
	- `stage_weights`

#### Output
```
canonical_scenario.json
```

Persist:

```
artifacts/{run_id}/canonical_scenario.json
```

#### Hard Rule (MOST IMPORTANT)

```
Adapter is the ONLY component allowed to produce canonical_scenario.json
```

---

### 4. Execution Preparation (Platform)

Responsibilities
- Concurrency control:
	- `max_concurrent_runs`
	- FIFO queue
- Environment setup:
	- artifact folders
	- logs/
	- environment variables
- Update status:
	- Run → `Queued → Running`
	- Jobs → `Running`

---

### 5. Simulation Execution (Execution Engine — C++)

#### Input

```
canonical_scenario.json ONLY
```

#### Execution

- Invoke CLI
- Capture:
	- exit code
	- stdout / stderr
	- execution time

#### Outputs
```
simulation_result.json
production_records.csv
logs/simulator.log
```

#### Engine Rules
```
Simulation MUST NOT:
- read Feature Store
- read Calibration Store
- read raw data
- parse public schema
```

---

### 6. Analytics Execution (Evaluation Engine — Python)

#### Input

- simulation outputs only

#### Responsibilities

- KPI computation
- Failure mode detection
- ROI analysis
- Recommendation generation

#### Outputs
```
analysis_response.json
recommendation.json
logs/analytics.log
```

---

### 7. Post-Processing & Persistence (Platform)

#### Responsibilities

- Extract KPI summaries
- Persist:

|Table | Data |
| --- | --- |
| run_metrics | KPIs |
| run_recommendations | structured recommendations |
| run_artifacts | artifact index |

#### Artifact Manifest 

Generate:

```
artifact_manifest.json
```

Includes:

- run_id
- artifact list
- timestamps
- simulator_version
- analytics_version

---

### 8. Status & Observability

#### Finalization

- Jobs → Completed
- Run → Completed

#### Logging

```
artifacts/{run_id}/logs/
```
#### Reproducibility Guarantee

A run is reproducible if:

- scenario_snapshot.json
- canonical_scenario.json
- deterministic seed
- all artifacts exist

---

## 11.4. Execution Contracts (NEW — VERY IMPORTANT)

### Engine Contract

```
Input:
- canonical_scenario.json

Output:
- predefined artifact set
```

### Platform Contract

```
Platform guarantees:
- canonical correctness
- artifact persistence
- run traceability
```

---

## 11.5. Failure Handling Model

| Stage | Failure Behavior |
| --- | --- |
| Validation | Stop immediately |
| ScenarioBuilder | Fail run |
| Adapter | Fail run |
| Simulation | Stop pipeline |
| Analytics	| Mark partial failure |

---

## 11.6. Deterministic Execution (Reinforced)

Canonical MUST contain:

```
random_seed
```

Guarantee:

```
Same canonical + same seed → identical outputs
```

---

## 11.7. Architectural Constraints (Refined)

- Platform = orchestration authority
- Adapter = canonical authority
- ScenarioBuilder = construction only
- DataSources = read-only gateway
- Engines = execution only

---

# 12. DEVELOPMENT RULES (Refined)

## Architecture Rules
- ScenarioBuilder MUST be interface-based
- DataSources MUST be pluggable
- Adapter MUST be isolated in Platform
- Engines MUST be stateless (per run)

## Build Rules
- Use Visual Studio solution (.sln)
- C++ via .vcxproj ONLY
- Python via CLI ONLY
- No CMake unless explicitly required

## Data Rules
- Artifacts are append-only
- No overwrite allowed
- Canonical is immutable
- Snapshot is immutable

## Documentation Rules
- Markdown only
- Provide bilingual documentation:
	- English → FILE_NAME.md
	- Vietnamese → FILE_NAME_VI.md
- Include header:
```
<p align="right">
  🇺🇸 <a href="FILE_NAME.md">English</a>
  | 🇻🇳 <a href="FILE_NAME.md">Tiếng Việt</a>
</p>
```

## Code Generation Rules (Critical for AI)

When generating code:

- Each file in the system MUST be produced as a separate artifact.
- Never merge multiple source files into a single output block.
- For every file generated:
	- Output the exact relative file path.
	- Output the full file content.
	- Do not truncate content.
- Do not generate placeholder files unless explicitly requested.
- Do not generate unrelated files outside the defined repository structure.

This ensures clean integration into the Visual Studio solution and preserves repository consistency.

---

# 12. PHASE-BASED ROADMAP

## Phase 0 — Repository Foundation & Data-Layer Conventions

### Target: 

Establish foundational structure and conventions for PIDSS to ensure the system is alligned with:

- Scenario-driven architecture
- Run-based execution model
- Strict boundary separation

### Define

#### 1. Repository & Structure

- Repository layout (platform / data_platform / engines / artifacts)
- Artifact directory:
```
artifacts/{run_id}/
```
- Logs convention:
```
artifacts/{run_id}/logs/
```

#### 2. Execution Model

- Run lifecycle:
```
Created → Queued → Running → Completed / Failed
```
- Job lifecycle:
```
Pending → Running → Completed / Failed
```

#### 3. Artifact Model

- Artifact classification:
	- snapshot (public)
	- canonical (internal)
	- simulation outputs
	- analytics outputs
	- manifest
- Append-only policy (NO overwrite)

#### 4. Scenario Lifecycle
```
User Input
   ↓
ScenarioBuilder
   ↓
Scenario Snapshot (public)
   ↓
Adapter
   ↓
Canonical Scenario
```

#### 5. Core Interfaces
- `IScenarioBuilder`
- `IDataSources`
	- `IFeatureStoreReader`
	- `ICalibrationProfileProvider`

#### Ensure
- Scenario ≠ Data (strict separation)
- Canonical = single execution source of truth
- Engines consume canonical only
- Data Platform NOT part of runtime execution

#### Out of Scope
- No business logic
- No simulation
- No analytics

---

## Phase 1 — Domain & Canonical Model

### Target: 

Define stable internal execution model, unchanging across versions, that serves as single source of truth for all engines.

### Define:

#### 1. Domain Model
- Stage (SOP identity, immutable)
- WorkUnit (execution unit)
- Integration (multi-stage execution)
- BOM → transformed to constraints (NOT direct in scenario)

#### 2. Canonical Scenario
- Fully normalized
- Deterministic
- No version ambiguity
- Engine-facing only

#### 3. Critical Rules
- `covered_stage_ids`:
	- MUST exist
	- MUST NOT be empty
- Multi-stage:
	- MUST have `integration`
	- MUST have `stage_weights`

#### 4. Execution Modeling
- Equipment-centric execution
- Batch flow modeling
- Transfer delay
- Break behavior
- Reliability (MTBF/MTTR)

#### 5. Determinism
- `random_seed` REQUIRED

### Output:
- `canonical_scenario.example.json`
- Domain explanation document

---

## Phase 2 — Public Contracts & Schemas

### Target: 

Define external API contracts.

### Define

#### 1. Input Schema
- `scenario.schema.json`
- versioned via `schema_version`

#### 2. Output Schemas
- `simulation_result.schema.json`
- `analysis_response.schema.json`
- `recommendation.schema.json`

#### 3. Validation Rules
- `required`
- `additionalProperties = false`
- enums
- constraints

### Enforce
- Public schema ≠ Canonical model
- Adapter handles:
	- validation
	- version mapping
- Engines:
	- NEVER parse public schema

---

## 🔹 Phase 3 — Database & Run Metadata

### Target: 
Support pipeline execution tracking + reproducibility.

### Design:

#### Tables:

- `runs`
- `jobs`
- `run_metrics`
- `run_recommendations`
- `run_artifacts`

#### Include:

- Status fields + timestamps
- Job-level status tracking
- Artifact indexing fields:
	- type
	- path
	- created_at

#### Add:

Artifact Manifest
```
artifact_manifest.json
```
Contains:

- run_id
- artifact list
- timestamps
- engine versions

#### Ensure:

- No business domain duplication in DB, metadata only
- Artifacts remain source of truth

---

## Phase 4 — Platform Core (ScenarioBuilder + Adapter + Orchestration)

### Target: 
Implement core runtime pipeline.

### Implement:

#### 1. ScenarioBuilder
- Read via DataSources ONLY
- Merge:
	- user input
	- feature store
	- calibration profile
- Output:
```
scenario_snapshot.json (enriched, public schema)
```

#### 2. DataSources
- Read-only abstraction
- No logic, no transformation

#### 3. Adapter (CRITICAL)
- Schema validation
- Version adaptation
- Canonicalization
- Stage weight computation

#### 4. Orchestration
- Run creation
- Concurrency control (FIFO)
- Engine invocation (C++ / Python)
- Logging
- Status transitions
- Failure handling

Enforce

- ScenarioBuilder MUST NOT produce canonical
- Adapter = ONLY canonical authority
- Canonical stored immutably

---

## Phase 5 — C++ Simulation v1 (Aggregate Model)

### Target: 
Implement deterministic aggregate simulation.

### Implement:

- Canonical parser
- Capacity computation (equipment pool)
- Integrated cell handling
- Batch gating
- Transfer delay
- Break logic

### Output:
- `production_records.csv`
- `simulation_result.json`

### MUST Support
- Throughput
- Stage utilization
- Blocking / starvation
- WIP per stage
- Total WIP
- Bottleneck stage
- Machine area
- WIP area
- Production footprint

### Constraints
- No discrete-event simulation
- Must support:
	- WIP estimation
	- flow stability
	- footprint evaluation

---

## Phase 6 — Python Analytics v1

### Target: 
Compute decision-support metrics.

### Implement:

#### 1. KPI Computation
- throughput
- lead time (via Little’s Law)
- WIP
- utilization

#### 2. Advanced Metrics
- footprint
- throughput_per_m2
- operator utilization

#### 3. Failure Mode Detection

10 system-level failure modes:

- blocking
- starvation
- batch mismatch
- bottleneck migration
- WIP explosion
- reliability dominance
- single point of failure
- footprint violation
- labor imbalance
- ROI illusion

### Scenario Comparison

#### Input:

- baseline_run_id
- candidate_run_id

#### Constraints:

- MUST use stored artifacts
- NO recomputation

#### Output

- `analysis_response.json`
- `recommendation.json`

---

## Phase 7 — UI MVP

### Target: 
Minimal decision-support interface.

### Features:
- Create scenario
- Trigger run
- View run status
- KPI visualization
- Bottleneck identification
- Scenario comparison
- Recommendation + ROI

### Constraint
- UI = API client ONLY
- No business logic

---

## Phase 8 — Optimization Batch

### Target: 
Support automation strategy exploration.

### Implement:

- Multi-scenario execution
- Deterministic seed control
- Parallel execution (bounded)
- Ranking logic

### Constraints
- MUST go through canonical pipeline
- MUST NOT bypass Platform

### Output
- Top-K scenarios
- Ranking summary

---

## Phase 9 — ML-based Decision Intelligence

### Target:

- Replace rule-based recommender with ML model
- Add scenario ranking model
- Add capacity prediction model
- Add ROI prediction model
- Compare rule-based vs ML performance

### Implement:

- Feature extraction from simulation outputs
- Supervised learning model (regression/classification)
- Scenario ranking model
- Model persistence
- Inference integrated into analytics pipeline
- Comparison between rule-based and ML-based recommendation

### Output:

- `model.pkl`
- `metrics.json`
- enhanced `recommendation.json`


### Optional:

- Model retraining pipeline
- Cross-validation report artifact
- Model evalation artifact
- Model evaluation dashboard (separate UI)

## Phase 10 — Observed Import (Optional / Future Extension)

### Target: 
Bridge simulation with real production data.

### Implement:

Future MES integration:

- MUST be API-based
- MUST NOT connect directly to MES DB in production architecture
- Observed vs simulated KPI comparison
- Gap analysis report

File-based integration:

- CSV observed data import
- Normalization to internal format
- Observed vs simulated KPI comparison
- Gap analysis report

### Constraint
- NO direct MES integration
- MES only via API 