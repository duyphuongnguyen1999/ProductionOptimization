# PIDSS Artifact Convention

**Version:** 1.0.0  
**Phase:** 0 — Repository Foundation & Data-Layer Conventions  
**Status:** Active

---

## 1. Overview

Every execution of a scenario (a **Run**) produces a dedicated, immutable artifact directory.  
Artifacts are the **source of truth** for all run outputs. The database stores only metadata and indexed references — never domain execution data.

---

## 2. Artifact Directory Layout

```
artifacts/
└─ {run_id}/                          # UUID v4
   ├─ scenario_snapshot.json          # Immutable: original public scenario payload as submitted
   ├─ canonical_scenario.json         # Immutable: platform-adapted canonical execution model
   ├─ simulation_result.json          # Output: C++ simulator aggregate summary
   ├─ production_records.csv          # Output: C++ simulator per-stage period records
   ├─ analysis_response.json          # Output: Python analytics — KPIs, failure modes, footprint
   ├─ recommendation.json             # Output: Python analytics — ranked recommendations with ROI
   ├─ artifact_manifest.json          # Platform-generated: integrity index + engine versions
   └─ logs/
      ├─ platform.log                 # Platform orchestration log (structured JSON)
      ├─ simulator.log                # C++ CLI stdout + stderr
      └─ analytics.log                # Python CLI stdout + stderr
```

---

## 3. Append-Only Policy

### Rule

> **Once written, no artifact file may be modified, renamed, or deleted.**

### Enforcement

- Platform code must never open artifact files in write or truncate mode after initial creation.
- Failed runs leave all partial artifacts on disk as-is.
- Retry or re-run always creates a **new `run_id`** and a fresh artifact directory.
- No background cleanup process may remove artifact directories.

### Rationale

- **Reproducibility:** any prior run can be fully re-inspected from its artifact directory alone.
- **Auditability:** every decision recommendation is traceable to its exact inputs.
- **Debugging:** failed or unexpected runs preserve all partial outputs for root cause analysis.

---

## 4. File Definitions

### `scenario_snapshot.json`

- **Written by:** Platform, immediately upon run creation.
- **Content:** Exact copy of the public scenario payload as submitted, including `schema_version`.
- **Purpose:** Immutable source of record. Used for audit, reproducibility, and potential re-adaptation under a future schema version.
- **Schema:** Matches `scenario.vN.schema.json` at submission time.

---

### `canonical_scenario.json`

- **Written by:** Platform adapter, after successful validation and adaptation.
- **Content:** Complete, engine-facing canonical execution model. No `schema_version` field.

The canonical scenario contains:

- **`factory_footprint_limit_m2`** — top-level hard space constraint for the factory
- **`layout_factor`** — top-level multiplier for production footprint computation (default 1.3)
- **`processes[]`** — array of process definitions, each containing:
  - `process_id`, `output` (component or product reference)
  - `stages[]` — ordered SOP steps (`stage_id`, `order`, `name` only)
  - `work_units[]` — execution resources, each containing:
    - `unit_id`, `unit_type`, `covered_stage_ids[]` (always array, min 1 element)
    - `count`, `cycle_time`, `operators_per_unit`, `requires_operator_presence`
    - `integration.stage_weights` — **always present and pre-materialized** when `covered_stage_ids.length > 1`
    - `reliability` (optional): `mtbf_hours`, `mttr_minutes`, `age_years`, `useful_life_years`
    - `footprint_m2` (optional): machine floor area per unit
    - `financial` (optional): CAPEX, OPEX, useful life for ROI
  - `batch_size`, `transfer_delay_sec` — flow policy per stage or process
  - `unit_buffer_area_m2` — floor area per WIP unit at stage boundaries (for WIP buffer area computation)
- **`bom[]`** — top-level BOM definitions: `product_id`, `component_id`, `quantity_required_per_product`
- **`shift_calendar`** — working days, shift windows, break definitions
- **`planning_period`** — `start_time`, `end_time`, `target_output_qty`
- **`random_seed`** — always present; assigned by adapter if not provided in public scenario

- **Purpose:** Stable input consumed by both C++ Simulator and Python Analytics.
- **Critical rule:** Domain execution data (process structure, BOM, WorkUnit definitions, stage weights) exists **only** in this file and `scenario_snapshot.json`. It is **not** stored in the relational database.

---

### `simulation_result.json`

- **Written by:** C++ Simulator CLI.
- **Content:** Aggregate simulation summary. Must include all of the following field groups:

**Throughput & Flow:**
- `throughput` — completed units per hour (process level and total)
- `lead_time_estimate` — estimated flow time via Little's Law (`total_wip / throughput`)
- `total_wip` — aggregate WIP across all stage boundaries
- `wip_per_stage` — map of `{ stage_id: float }` — average WIP at each stage boundary

**Stage-Level Performance:**
- `stage_utilization` — map of `{ stage_id: float }` — fraction of available time actively processing
- `blocking_time` — map of `{ stage_id: float }` — cumulative time blocked per stage (seconds)
- `starvation_time` — map of `{ stage_id: float }` — cumulative time starved per stage (seconds)

**Footprint:**
- `machine_area_m2` — total floor area of all WorkUnits: `Σ(count × footprint_m2)`
- `wip_area_m2` — total floor area of WIP buffers: `Σ(wip_per_stage × unit_buffer_area_m2)`
- `production_footprint_m2` — `(machine_area_m2 + wip_area_m2) × layout_factor`

**Reliability:**
- `effective_availability` — map of `{ unit_id: float }` — computed from MTBF/MTTR where reliability data is present

- **Schema:** `data/schemas/simulation_result.v1.schema.json` (defined in Phase 2)

---

### `production_records.csv`

- **Written by:** C++ Simulator CLI.
- **Content:** Per-stage, per-period records. Columns include stage_id, period, units_produced (attributed via stage_weights for integrated WorkUnits), utilization, blocking_time, starvation_time, wip_at_boundary.
- **Schema:** `data/schemas/production_records.v1.schema.json` (defined in Phase 2)

---

### `analysis_response.json`

- **Written by:** Python Analytics CLI.
- **Content:** Full KPI and diagnostic analysis. Must include all of the following field groups:

**Core KPIs:**
- `throughput` — validated and normalized from simulation output
- `lead_time_estimate` — from simulation; validated against Little's Law
- `total_wip`, `wip_per_stage`
- `bottleneck_stage` — stage_id with highest utilization / longest blocking time
- `capacity_utilization` — actual vs max theoretical throughput
- `operator_utilization` — per stage or process

**Footprint KPIs:**
- `machine_area_m2`, `wip_area_m2`, `production_footprint_m2` — passed through from simulation
- `throughput_per_m2` — `throughput / production_footprint_m2`
- `wip_ratio` — `total_wip / baseline_wip` (baseline from reference run or planning target)
- `footprint_constraint_status` — `within_limit` or `violation` based on `factory_footprint_limit_m2`

**Failure Mode Detection:**
- `failure_modes[]` — array of detected failure mode objects, each containing:
  - `code` — FM-01 through FM-10
  - `name` — human-readable failure mode name
  - `severity` — `low`, `medium`, `high`, `critical`
  - `affected_stages[]` — stage_ids implicated
  - `detection_evidence` — key metric values that triggered detection
  - `description` — narrative explanation

**WIP Stability:**
- `wip_stability` — `stable`, `accumulating`, or `unstable`
- `wip_growth_rate` — units per hour (positive = accumulating)

- **Schema:** `data/schemas/analysis_response.v1.schema.json` (defined in Phase 2)

---

### `recommendation.json`

- **Written by:** Python Analytics CLI.
- **Content:** Ranked recommendations with quantified impact. Must include:

- `recommendations[]` — ranked array, each containing:
  - `rank` — integer priority
  - `type` — e.g., `increase_capacity`, `reduce_batch_size`, `add_redundancy`, `replace_equipment`, `rebalance_labor`, `postpone_investment`
  - `target_stage_ids[]` — affected stages
  - `rationale` — explanation referencing specific failure modes or KPI gaps
  - `linked_failure_modes[]` — FM codes this recommendation addresses
  - `estimated_impact` — quantified: throughput_delta, footprint_delta, roi_percent, payback_years
  - `confidence` — `rule_based` (Phase 6) or `ml_model` (Phase 9)

- **Schema:** `data/schemas/recommendation.v1.schema.json` (defined in Phase 2)

---

### `artifact_manifest.json`

- **Written by:** Platform, during post-processing after all jobs complete.
- **Purpose:** Integrity index and engine version record for the run.

```json
{
  "run_id": "a3f2b1c4-7e8d-4f9a-b012-3456789abcde",
  "created_at": "2025-01-15T08:30:00Z",
  "engine_versions": {
    "simulator_version": "1.0.0",
    "analytics_version": "1.0.0"
  },
  "artifacts": [
    {
      "type": "scenario_snapshot",
      "filename": "scenario_snapshot.json",
      "path": "artifacts/a3f2b1c4-7e8d-4f9a-b012-3456789abcde/scenario_snapshot.json",
      "size_bytes": 4096,
      "sha256": "e3b0c44298fc1c149afb...",
      "written_at": "2025-01-15T08:30:01Z"
    },
    {
      "type": "canonical_scenario",
      "filename": "canonical_scenario.json",
      "path": "artifacts/a3f2b1c4-7e8d-4f9a-b012-3456789abcde/canonical_scenario.json",
      "size_bytes": 8192,
      "sha256": "a4c1d2e3f4b5...",
      "written_at": "2025-01-15T08:30:02Z"
    }
  ]
}
```

**`engine_versions` is required.** Both `simulator_version` and `analytics_version` must be recorded. These are captured from the engine CLI's first stdout line at startup and stored in job metadata before being written to the manifest.

---

### `logs/platform.log`

- **Written by:** Platform.
- **Content:** Structured JSON log of all pipeline steps, status transitions, adapter decisions, timing, and validation results.

### `logs/simulator.log`

- **Written by:** Platform (captures C++ CLI stdout + stderr).
- **Content:** First line: engine version string (`PIDSS-Simulator X.Y.Z`). Remainder: diagnostic output.

### `logs/analytics.log`

- **Written by:** Platform (captures Python CLI stdout + stderr).
- **Content:** First line: engine version string (`PIDSS-Analytics X.Y.Z`). Remainder: diagnostic output.

---

## 5. Scenario Comparison Policy

A/B scenario comparison compares two previously completed runs to evaluate a candidate scenario against a baseline.

### Rule

> **Comparison never re-invokes the simulation or analytics engines.**  
> It reads exclusively from the stored artifacts of both runs.

### Inputs

- `baseline_run_id` — a `Completed` run
- `candidate_run_id` — a `Completed` run

### Process

1. Platform reads `analysis_response.json` and `simulation_result.json` from both run directories.
2. Delta metrics are computed in-process (Platform or Analytics CLI in comparison mode):
   - `throughput_delta`, `lead_time_delta`, `wip_delta`
   - `footprint_delta`, `throughput_per_m2_delta`
   - `roi_delta`, `payback_delta`
3. Failure modes from both runs are compared to identify newly introduced or resolved failure modes.
4. Stage IDs are used as the stable comparison anchor — they are consistent across all scenarios.

### Artifact Requirement

Both runs must have status `Completed` and all required artifacts present before comparison can proceed. If either run is `Failed` or artifacts are missing, comparison is rejected.

---

## 6. Run ID Convention

- **Format:** UUID v4
- **Generated by:** Platform at run creation
- **Used as:** directory name, database primary key, artifact reference
- **Never reused** across runs including failed runs and retries

---

## 7. Database vs Artifact Responsibility

| Data | Stored In | Rationale |
|---|---|---|
| Run lifecycle status, timestamps | Database | Queryable metadata |
| Job status, engine version, timing | Database | Queryable metadata |
| KPI summary values | Database (`run_metrics`) | Fast querying and comparison |
| Recommendation summary | Database (`run_recommendations`) | Fast querying |
| Artifact paths and checksums | Database (`run_artifacts`) | Artifact discovery |
| Process structure, stages, WorkUnits | `canonical_scenario.json` only | Domain execution data — not duplicated in DB |
| BOM definitions | `canonical_scenario.json` only | Domain execution data — not duplicated in DB |
| Stage weights | `canonical_scenario.json` only | Computed artifact — not duplicated in DB |
| WIP per stage, blocking, starvation | `simulation_result.json` only | Simulation artifact is source of truth |
| Footprint metrics | `simulation_result.json` only | Simulation artifact is source of truth |
| Failure mode detections | `analysis_response.json` only | Analytics artifact is source of truth |
| Full recommendations | `recommendation.json` only | Analytics artifact is source of truth |

---

## 8. gitignore Rules

```gitignore
artifacts/*
!artifacts/.gitkeep
```

Artifacts are runtime data. They must never be committed to the repository.
