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

## 3.1. Key Definitions

### Process

Transforms raw materials → component or final product.  

Defined by SOP. Rarely changes.

A factory may contain multiple processes.

Some processes produce:
- Components (semi-finished goods)
- Final products (assembly / packaging)

### Component

Output of a process.

Consumed by downstream process via BOM.

Modeled in aggregate only (no WIP tracking).

### Product (Final Product)

Assembled or packaged from multiple components.

Final production capacity is constrained by:

- Final assembly capacity
- Upstream component availability

### BOM (Bill of Materials)

Defines:

- product_id
- component_id
- quantity_required_per_product

Final output limited by minimum component availability.

### Stage

A stable SOP step within a process.

Critical rule:

> Stage identity MUST NEVER be deleted or replaced by automation.

Stage represents business traceability and comparability.

Stage contains:

- stage_id
- order
- name

Stage contains NO execution logic.

### Work Unit (Execution Unit)
Represents execution capacity.

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

### Integration Concept

Integrated cell is NOT a separate type.

Integration is defined by:

- covered_stage_ids.length > 1

If multiple stages are covered:

- An `integration` object must exist
- `stage_weights` must be explicitly materialized in canonical

### Line
Logical replication of process.

Important:
- Resources are NOT necessarily 1:1 with lines.  
- Capacity must be modeled at stage resource pool level.

## 3.2. Example: Assembly Process (7 Stages)

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

## 3.3. Line vs Stage Capacity Reality

There are 7 lines.

However:

- Pressing & Heating: exactly 7 machines
- Other stages: more than 7 workstations

Therefore:

> Capacity constraints must be modeled at stage resource pools,
> NOT at fixed line mapping.

## 3.4. Batch Flow Reality

Current production flow is batch-gated:

- Batch size: 600 pieces
- Transfer only after full batch completion
- Transfer delay: 3–5 minutes (checksheet/confirmation)

Automation goal includes:

- Reducing transfer delay
- Reducing labor
- Reducing footprint
- Increasing throughput

## 3.5. Critical Rule — Integrated Automated Cell

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
	- stage_id
	- order
	- name

## 4.2. Equipment-Centric Execution (Core Design Principle)

Execution is defined by Work Units (Equipment Units).

Rules:

- covered_stage_ids is mandatory
- Single-stage = one element array
- Multi-stage = integrated execution
- stage_id field is NOT used

Automation level (manual/semi_auto/auto) is independent from integration scope.

## 4.2.1 Automation Scenario Modeling Rules

Automation scenarios must respect realistic manufacturing constraints.

The system must prevent unrealistic automation modeling.

Rules:

1. Stage Identity Preservation

Automation must NEVER delete or replace Stage definitions.

Stages represent SOP traceability and historical comparability.

Automation is modeled only through WorkUnit execution capacity.

2. Integrated Cell Representation

If an automated cell covers multiple SOP stages:

- covered_stage_ids must include all affected stages
- integration object must exist
- stage_weights must be materialized in canonical scenario

3. Batch Compatibility

Automation scenarios must consider batch compatibility across stages.

Large automation batches may cause:

- downstream blocking
- WIP explosion
- flow instability

PIDSS must evaluate automation batch size against downstream batch policy.

4. Automation Requires System-Level Evaluation

Automation must never be evaluated at machine level only.

Simulation and analytics must evaluate:

- upstream supply capability
- downstream capacity
- WIP accumulation
- footprint impact
- labor redistribution

Automation scenarios must therefore be evaluated at full process level.

## 4.3 Stage Attribution (Integrated Units)

When a WorkUnit covers multiple stages:

- Platform Adapter must compute `stage_weights`
- `stage_weights` must sum to 1
- Engines must not compute attribution logic

Attribution ensures:

- Bottleneck reporting per stage
- A/B comparability
- Traceability preservation

## 4.4 Reliability & Lifecycle Modeling

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

## 4.5. Flow Model

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

## 4.6. Production Footprint Model

Factory footprint is a hard constraint in many manufacturing environments.

PIDSS must evaluate automation scenarios not only by throughput and ROI, but also by production floor utilization.

Production footprint includes:

```
Total Production Area =
    (Machine Area + WIP Buffer Area) × Layout Factor
```

Where:

### Machine Area
```
Machine Area =
    Σ(machine_count × machine_footprint_m2)
```

This typically represents 60–80% of production floor area.

### WIP Buffer Area

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

### Layout Factor

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

## 4.7 WIP ESTIMATION MODEL

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

## 4.8 BATCH FLOW DYNAMICS

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

## 4.9. SYSTEM FAILURE MODES (CRITICAL FOR DECISION SUPPORT)

PIDSS must detect and analyze system-level automation failure modes that frequently occur in real manufacturing environments.

The purpose is to prevent local optimization (machine-level) that harms system-level performance.

Failure detection is performed by:

- C++ Simulator → generating raw metrics
- Python Analytics → detecting failure patterns
- Recommender → generating corrective actions

The following 10 failure modes MUST be supported in PIDSS v1.

### 1️⃣ Downstream Blocking

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

### 2️⃣ Upstream Starvation

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

### 3️⃣ Batch Size Mismatch

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

### 4️⃣ Bottleneck Migration

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

### 5️⃣ WIP Explosion (Lead Time Increase)

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

### 6️⃣ Reliability Dominance

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

### 7️⃣ Single Point of Failure

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

### 8️⃣ Footprint Constraint Violation

Automation scenario exceeds factory space limit.

Detected using:

- total_footprint_m2 > factory_footprint_limit

Possible recommendations:

- retire legacy machines
- replace multiple benches with integrated cell
- optimize layout

### 9️⃣ Labor Utilization Imbalance

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

### 🔟 ROI Illusion

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

# 5. ARCHITECTURE OVERVIEW

## 5.1. Public JSON Contracts (Versioned)

Public input:

- scenario.schema.json
- schema_version

Public outputs:

- simulation_result.json
- production_records.csv
- analysis_response.json
- recommendation.json

## 5.2. Canonical Scenario (Internal Execution Model)

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

## 5.3. Adapter Strategy

Only Platform (.NET) handles:

- schema validation
- version adaptation
- canonicalization
- Stage weight computation
- Normalization

Simulator & Analytics:

- NEVER parse public schema versions
- NEVER handle version branching

## 5.4. Run-Based Execution Model

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

## 5.5 Deterministic Simulation Requirement

Simulation must support deterministic execution.

Each canonical scenario must contain a `random_seed` field.

If the same canonical scenario and seed are used,
the simulator MUST produce identical outputs.

Purpose:

- reproducibility
- regression testing
- reliable A/B comparison

## 5.6 Artifact Manifest & Engine Version Tracking

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

# 6. TECH STACK

Windows-first. Visual Studio-first.

Backend:

- ASP.NET Core Web API (Platform)
- EF Core optional

Simulation:

- C++ CLI (Visual Studio .vcxproj)
- Aggregate digital twin

Analytics:

- Python (pandas/numpy)
- CLI-based execution

UI:

- WinForms (.NET)

Contracts:

- JSON + JSON Schema Draft-07

Database:

- SQL Server or PostgreSQL

Artifacts:

- Filesystem (append-only)

---

# 7. REPOSITORY STRUCTURE

```graphql
ProductionOptimization/
├─ data/
│ ├─ contracts/
│ ├─ schemas/
│ ├─ validation/
│ ├─ transforms/
│ ├─ lineage/
│ └─ documentation/
├─ platform_dotnet/
│ └─ Pidss.Platform.Api/
├─ simulator_cpp/
│ └─ Pidss.Simulator.Cli/
├─ analytics/
│ └─ Pidss.Analytics.Cli/
├─ presentation/
│ └─ Pidss.Desktop.Winforms/
├─ artifacts/
└─ docs/
```

---

# 8. DATA LAYER STRUCTURE

Data governance is centered around `data/` folder, which contains all versioned contracts, 
schemas, validation logic, transformation scripts, lineage policies, and documentation.

``` graphql
data/
 ├─ contracts/
 ├─ schemas/
 ├─ validation/
 ├─ transforms/
 ├─ lineage/
 └─ documentation/
```

Definitions:

- `contracts/` = example payloads
- `schemas/` = JSON Schema
- `validation/` = validation logic/tests
- `transforms/` = analytical transforms
- `lineage/` = artifact/run metadata policy
- `documentation/` = versioning + domain model docs

Adapters belong to Platform layer.

No separate `adapters/` folder under data is required.

---

# 9. DATABASE (RUN METADATA ONLY)

Tables:

- runs
- jobs
- run_artifacts
- run_metrics
- run_recommendations

Domain execution data NOT stored relationally. Stored as JSON artifacts.

---

# 10. JOB ORCHESTRATION

## PIDSS Run Pipeline (Final Architecture-Aligned Version)

This pipeline reflects the actual PIDSS architecture:

- Platform (.NET) handles validation, versioning, adapter, orchestration, and persistence  
- C++ CLI performs aggregate simulation  
- Python CLI performs KPI computation and recommendation  
- All runs are immutable and artifact-based  

---

## 1. Ingest & Run Creation (Platform)

1. `POST /runs` receives the public scenario payload (including `schema_version`).
2. Create a `runs` record and assign a `run_id` (UUID).
3. Create initial `jobs` entries (Simulation = Pending, Analytics = Pending).
4. Persist the snapshot:
   - `artifacts/{run_id}/scenario_snapshot.json` (immutable).

---

## 2. Validation & Canonicalization (Platform-only)

1. Validate the payload against the JSON Schema in `data/schemas/` using `schema_version`.
2. If validation fails:
   - Persist validation errors.
   - Mark run as `Failed`.
   - Stop execution.
3. Transform the public scenario into the internal execution model:
   - Generate `canonical_scenario.json`.
   - Engines must not perform version handling.
4. Persist canonical artifact:
   - `artifacts/{run_id}/canonical_scenario.json` (immutable).

---

## 3. Execution Preparation (Platform)

1. Apply concurrency control (`max_concurrent_runs`, FIFO queue).
2. Prepare execution environment:
   - Ensure artifact directories exist.
   - Create `logs/` folder.
   - Set required environment variables.
3. Update run and job status to `Queued` → `Running`.

---

## 4. Simulation Execution (C++ Engine)

1. Invoke the C++ simulator CLI with:
   - Input: `canonical_scenario.json`.
2. Monitor execution:
   - Capture exit code.
   - Capture stdout/stderr.
   - Record runtime metrics.
3. Persist outputs immutably:
   - `simulation_result.json`
   - `production_records.csv`
   - Simulator logs under `logs/`
4. If simulator fails:
   - Mark simulation job and run as `Failed`.
   - Stop pipeline.

---

## 5. Analytics Execution (Python Engine)

1. Invoke the Python analytics CLI with simulator outputs.
2. Monitor execution:
   - Capture exit code.
   - Capture stdout/stderr.
   - Record runtime metrics.
3. Persist outputs immutably:
   - `analysis_response.json`
   - `recommendation.json`
   - Analytics logs under `logs/`
4. If analytics fails:
   - Mark analytics job and run as `Failed`.

---

## 6. Post-Processing & Persistence (Platform)

1. Extract KPI summaries from `analysis_response.json`.
2. Persist metrics into `run_metrics`.
3. Persist recommendations into `run_recommendations`.
4. Index all artifacts in `run_artifacts`.
5. Optionally generate `artifact_manifest.json`.

---

## 7. Status & Observability

1. Finalize statuses:
   - Jobs → `Completed`
   - Run → `Completed`
2. Ensure structured logs are stored under:
   - `artifacts/{run_id}/logs/`
3. Guarantee reproducibility using:
   - `scenario_snapshot.json`
   - `canonical_scenario.json`
   - All generated artifacts

---

## Architectural Constraints

- Platform owns validation, versioning, adapter logic, and orchestration.
- Engines consume canonical format only.
- Artifacts are append-only and immutable per run.
- No MES logic, no dispatching, no real-time control.
- Designed for on-prem deployment and bounded parallel execution.

# 11. DEVELOPMENT RULES

- Always respect Visual Studio workflow.
- Do NOT create unnecessary build scripts.
- Do NOT introduce CMake unless explicitly requested.
- C++ must be built via .vcxproj inside the Visual Studio solution.
- Python components must remain CLI-based.
- Adapter logic must exist only inside platform_dotnet.
- Canonical model must remain stable and engine-facing.
- Public schemas may evolve, but canonical must not.
- Backward compatibility must be handled via adapter logic.
- Engines must consume canonical only (no schema-version branching).
- Artifacts are append-only and immutable per run.
- When generating documentation:
	- Always use Markdown format.
	- Provide bilingual documentation:
		- English → README.md
		- Vietnamese → README_VI.md
	-  Include language switch header:
		```
		<p align="right">
		  🇺🇸 <a href="README.md">English</a>
		  | 🇻🇳 <a href="README_VI.md">Tiếng Việt</a>
		</p>
		```
	- Maintain identical structure between languages.


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

## 🔹 Phase 0 — Repository Foundation & Data-Layer Conventions

Target: Establish structural and execution foundations aligned with run-based pipeline.

Create:

- Repository folder layout
- Artifact directory convention: `artifacts/{run_id}/`
- Logs convention: `artifacts/{run_id}/logs/`
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

Simulation outputs MUST include:
```
- throughput
- stage utilization
- blocking time
- starvation time
- WIP per stage
- total WIP
- machine area
- WIP area
- total production footprint
- bottleneck stage
```

Simulator must support:

- flow stability analysis
- WIP estimation
- footprint evaluation

without discrete-event simulation.

## 🔹 Phase 6 — Python Analytics v1

Target: Compute decision-support metrics.

Implement:

- KPI aggregation
- Bottleneck identification
- WIP stability metrics
- Failure mode detection (10 system failure modes)
- Capacity delta analysis
- ROI and payback estimation
- Recommendation logic (rule-based)

Analytics must compute:

Core KPIs:

```
throughput
lead time estimate
WIP level
bottleneck stage
capacity utilization
```

Footprint KPIs:

```
machine_area
WIP_area
production_footprint
```

Efficiency metrics:

```
throughput_per_m2
WIP_ratio
operator_utilization
```

Where:

```
throughput_per_m2 =
    throughput / production_footprint
```

This metric helps evaluate capacity density of factory floor.

Analytics must also detect:

- the 10 defined failure modes
- flow imbalance
- footprint constraint violations

Analytics must support scenario comparison.

Comparison inputs:

- baseline_run_id
- candidate_run_id

Comparison metrics include:

- throughput delta
- lead time delta
- WIP delta
- footprint delta
- ROI delta

Comparison must rely solely on stored run artifacts.
No simulation recomputation is allowed during comparison.

Outputs:

```
analysis_response.json
recommendation.json
```

Where:

- `analysis_response.json` contains detected failure modes and KPI analysis.
- `recommendation.json` contains suggested corrective actions.

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
