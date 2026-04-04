# PHASE-BASED ROADMAP

## 🔹 Phase 0 — Repository Foundation & Data-Layer Conventions

Target: Establish structural and execution foundations aligned with run-based pipeline.

Create:

- Repository folder layout
- Artifact directory convention: artifacts/{run_id}/
- Logs convention: artifacts/{run_id}/logs/
- Append-only artifact policy
- Status model definition:
	- Run: Created → Queued → Running → Completed/Failed
	- Job: Pending → Running → Completed/Failed
- Versioning policy
- Naming conventions
- Data dictionary template

Ensure:

- Equipment-centric execution model
- Public vs Canonical model separation
- Run-based immutability
- Engines consume canonical only

Do not implement business logic yet.

## 🔹 Phase 1 — Domain & Canonical Model

Target: Define stable internal execution model.

Define:

- Domain Concept Diagram
- Canonical Scenario JSON structure (engine-facing, stable)
- Stage vs WorkUnit separation
- Integrated cell modeling
- Batch flow & transfer policy
- Break handling & operator presence logic
- Deterministic seed handling

Output:

- `canonical_scenario.example.json`
- Domain explanation document

Canonical must not contain public-version branching logic.

## 🔹 Phase 2 — Public Contracts & Schemas

Target: Define external API contracts.

Generate:

- `scenario.schema.json`
- `simulation_result.schema.json`
- `analysis_response.schema.json`
- `recommendation.schema.json`
- (Optional) `validation_error.schema.json`

Add:

- required fields
- additionalProperties=false
- enums
- validation constraints

Ensure:

- Public schema remains flexible
- Adapter handles mapping to canonical
- Engines never parse public schema

## 🔹 Phase 3 — Database & Run Metadata

Target: Support pipeline execution tracking.

Design:

- `runs`
- `jobs`
- `run_metrics`
- `run_recommendations`
- `run_artifacts`

Include:

- Status fields + timestamps
- Job-level status tracking
- Artifact indexing fields
- Minimal lineage metadata (run_id, artifact type, path)

Ensure:

- No business domain duplication in DB
- Artifacts remain source of truth

## 🔹 Phase 4 — Platform & Adapter

Target: Implement orchestration pipeline.

Implement:

- Scenario validation service
- Version adapter (public → canonical)
- Canonical DTO + serializer
- Concurrency gate (`max_concurrent_runs`, FIFO)
- Simulator invocation logic
- Python invocation logic
- Structured log capture
- Status transition handling
- Artifact indexing
- Failure handling

Ensure:

- Engines consume canonical only
- Canonical saved immutably
- Logs stored under `artifacts/{run_id}/logs/`
- Append-only artifact rule enforced by design

## 🔹 Phase 5 — C++ Simulation v1 (Aggregate Model)

Target: Implement deterministic aggregate simulation.

Implement:

- Canonical parser
- Equipment pool capacity calculation
- Integrated cell logic
- Batch gating logic
- Transfer delay logic
- Break impact logic
- Stochastic cycle time support (optional v1)
- Output:
	- `production_records.csv`
	- `simulation_result.json`

No discrete-event modeling.

## 🔹 Phase 6 — Python Analytics v1

Target: Compute decision-support metrics.

Implement:

- KPI aggregation
- Bottleneck identification
- Capacity delta analysis
- ROI and payback estimation
- Recommendation logic (rule-based)

Output:

- `analysis_response.json`
- `recommendation.json`

Platform must persist metrics without recomputing.

## 🔹 Phase 7 — UI MVP

Target: Minimal decision-support interface.

Implement:

- Create scenario
- Trigger run
- View run status
- View KPIs
- View bottleneck stage
- Compare A/B runs
- View recommendation & ROI

UI calls Platform API only.

## 🔹 Phase 8 — Optimization Batch

Target: Support automation strategy exploration.

Implement:

- Multiple simulation trials
- Deterministic seed strategy
- Parallel execution (bounded)
- Top-K result persistence
- Ranking summary

Persist only:

- Baseline
- Top-K
- Final chosen scenario

## 🔹 Phase 9 — ML-based Decision Intelligence

Target:

- Replace rule-based recommender with ML model
- Add scenario ranking model
- Add capacity prediction model
- Add ROI prediction model
- Compare rule-based vs ML performance

Implement:

- Feature extraction from simulation outputs
- Supervised learning model (regression/classification)
- Scenario ranking model
- Model persistence
- Inference integrated into analytics pipeline
- Comparison between rule-based and ML-based recommendation

Output:

- `ml_model.pkl`
- `ml_metrics.json`
- Updated `recommendation.json`


Optional:

- Model retraining pipeline
- Cross-validation report artifact

## 🔹 Phase 10 — Observed Import (Optional / Future Extension)

Target: Bridge simulation with real production data.

Implement:

- CSV observed data import
- Normalization to internal format
- Observed vs simulated KPI comparison
- Gap analysis report

No MES integration. CSV only.
