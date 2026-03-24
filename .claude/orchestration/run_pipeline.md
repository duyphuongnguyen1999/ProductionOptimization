# Production Intelligence & Decision Support System (PIDSS)

# JOB ORCHESTRATION

## 1. PIDSS Run Pipeline (Final Architecture-Aligned Version)

This pipeline reflects the actual PIDSS architecture:

- Platform (.NET) = single orchestration authority
- ScenarioBuilder = data → scenario construction
- Adapter = canonicalization authority (ONLY place)
- Engines = pure execution (canonical only)
- Data Platform = pre-execution only (NOT in runtime pipeline)

---

## 2. End-to-End Execution Flow

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

## 3 Detailed Run Pipeline

### 3.1. Ingest & Run Creation (Platform)

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

### 3.2. Scenario Construction (ScenarioBuilder + DataSources)

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

### 3.3. Validation & Canonicalization (Adapter — SINGLE AUTHORITY)

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

### 3.4. Execution Preparation (Platform)

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

### 3.5. Simulation Execution (Execution Engine — C++)

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

### 3.6. Analytics Execution (Evaluation Engine — Python)

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

### 3.7. Post-Processing & Persistence (Platform)

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

### 3.8. Status & Observability

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