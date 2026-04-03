# PIDSS Artifact Convention

**Version:** 1.1.0  
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
- **Purpose:** Immutable source of record for audit and reproducibility.
- **Schema:** Matches `scenario.vN.schema.json` at submission time.

---

### `canonical_scenario.json`

- **Written by:** Platform adapter, after successful validation and adaptation.
- **Content:** Complete, engine-facing canonical execution model. No `schema_version` field.

The canonical scenario contains:

**Top-level factory fields:**
- `meta` — `version`, `random_seed` (always present)
- `factory` — `footprint_limit_m2` (hard space constraint), `layout_factor`

**Shift and calendar:**
- `shifts[]` — shift definitions with breaks (`coverage_mode`: `all_stop` | `staggered`)
- `days[]` — day type definitions with shift assignments and performance/cost multipliers
- `calendar` — `time_horizon`, `overtime[]`, `exceptions[]`, `demand` (with `target_output_qty`, `planning_unit`, `periods[]`)

> **Note:** There is no `planning_period` at the top level of the canonical model. All demand information lives in `calendar.demand`.

**Domain entities:**
- `materials[]` — raw materials and purchased components not manufactured in the factory
- `products[]` — all products (intermediate, semi, finished), each with:
  - `type`: `"intermediate_product"` | `"semi_product"` | `"finished_product"`
  - `bill_of_materials[]` — **required on all product types**; each item carries `type`, `id`, and `quantity_required_per_output`
- `processes[]` — array of process definitions, each containing:
  - `process_id`, `output_product_id`
  - `stages[]` — ordered SOP steps, each containing:
    - `stage_id`, `order`, `name`
    - `eligible_work_unit_model_ids[]` — **required**; models permitted to serve this stage
    - `input[]`, `output[]` — with `type` and `id` per item
    - `stage_parameters` — **required**: `defect_rate` (baseline), `rework` (`available`, `rework_rate`, `maximum_rework_cycles`)
    - `wip_model` — WIP buffer after this stage; `null` on the last stage of each process

**Equipment definitions:**
- `work_unit_models[]` — equipment class templates, each containing:
  - `model_id`, `name`, `type` (`manual` | `semi_auto` | `auto`)
  - `covered_stage_ids[]` — always an array, minimum one element
  - `operators_per_unit`, `requires_operator_presence`
  - `footprint_m2`, `unit_buffer_area_m2`, `transfer_delay_sec`, `batch_size`
  - `cycle_time_default`
  - `integration.stage_weights` — **always present and pre-materialized** when `covered_stage_ids.length > 1`
  - `reliability` (optional): `mtbf_hours`, `mttr_minutes`, `useful_life_years`, `degradation_model`
  - `financial` (optional): `capex_usd`, `opex_usd_per_year`, `useful_life_years`
- `work_units[]` — physical machine instances, each containing:
  - `work_unit_id` — globally unique
  - `work_unit_model_id` — references a `model_id` in `work_unit_models[]`
  - `cycle_time` — actual cycle time (may differ from model default due to age/wear)
  - `age_years`
  - `work_unit_parameters` — **required**: `defect_rate` (per-machine observed, overrides stage baseline), `operating_rate` (OEE Performance component)

- **Purpose:** Stable input consumed by both C++ Simulator and Python Analytics.
- **Critical rule:** Domain execution data (process structure, product definitions, BOM, WorkUnitModel definitions, WorkUnit instances, stage weights, stage parameters, work unit parameters) exists **only** in this file and `scenario_snapshot.json`. It is **not** stored in the relational database.

---

### `simulation_result.json`

- **Written by:** C++ Simulator CLI.
- **Content:** Aggregate simulation summary. Must include all of the following field groups:

**Throughput & Flow:**
- `throughput` — good units completed per hour (quality-adjusted, after defect)
- `lead_time_estimate` — estimated flow time via Little's Law (`total_wip / throughput`)
- `total_wip` — aggregate WIP across all stage boundaries
- `wip_per_stage` — map of `{ stage_id: float }` — average WIP at each stage boundary

**Stage-Level Performance:**
- `stage_utilization` — map of `{ stage_id: float }` — fraction of available time actively processing
- `blocking_time` — map of `{ stage_id: float }` — cumulative time blocked per stage (seconds)
- `starvation_time` — map of `{ stage_id: float }` — cumulative time starved per stage (seconds)

**Quality:**
- `defect_units_per_stage` — map of `{ stage_id: float }` — defective units generated per stage
- `rework_units_per_stage` — map of `{ stage_id: float }` — units sent to rework per stage
- `scrap_units_per_stage` — map of `{ stage_id: float }` — units scrapped (defect − rework) per stage

**Footprint:**
- `machine_area_m2` — total floor area of all WorkUnit instances: `Σ(count × footprint_m2)`
- `wip_area_m2` — total floor area of WIP buffers: `Σ(wip_per_stage × unit_buffer_area_m2)`
- `production_footprint_m2` — `(machine_area_m2 + wip_area_m2) × layout_factor`

**Reliability & OEE:**
- `effective_availability` — map of `{ unit_id: float }` — from MTBF/MTTR where present
- `effective_oee` — map of `{ unit_id: float }` — `availability × operating_rate × (1 − defect_rate)`

- **Schema:** `data/schemas/simulation_result.v1.schema.json` (defined in Phase 2)

---

### `production_records.csv`

- **Written by:** C++ Simulator CLI.
- **Content:** Per-stage, per-period records. Columns include `stage_id`, `period`, `good_units_produced`, `defect_units`, `rework_units`, `scrap_units`, `utilization`, `blocking_time`, `starvation_time`, `wip_at_boundary`.
- Stage output attributed via pre-materialized `stage_weights` for integrated WorkUnits.
- **Schema:** `data/schemas/production_records.v1.schema.json` (defined in Phase 2)

---

### `analysis_response.json`

- **Written by:** Python Analytics CLI.
- **Content:** Full KPI and diagnostic analysis. Must include all of the following field groups:

**Core KPIs:**
- `throughput` — quality-adjusted good units per hour
- `lead_time_estimate` — from simulation; validated against Little's Law
- `total_wip`, `wip_per_stage`
- `bottleneck_stage` — stage_id with highest utilization / longest blocking time
- `capacity_utilization` — actual vs max theoretical throughput
- `operator_utilization` — per stage or process

**Quality KPIs:**
- `defect_rate_per_stage` — effective defect rate per stage (from simulation)
- `rework_rate_per_stage` — fraction of defects reworked per stage
- `scrap_rate_per_stage` — fraction of defects scrapped per stage
- `oee_per_unit` — OEE breakdown (availability, performance, quality) per work unit

**Footprint KPIs:**
- `machine_area_m2`, `wip_area_m2`, `production_footprint_m2`
- `throughput_per_m2` — `throughput / production_footprint_m2`
- `wip_ratio` — `total_wip / baseline_wip`
- `footprint_constraint_status` — `within_limit` or `violation`

**Failure Mode Detection:**
- `failure_modes[]` — FM-01 through FM-10

**WIP Stability:**
- `wip_stability` — `stable`, `accumulating`, or `unstable`
- `wip_growth_rate` — units per hour

- **Schema:** `data/schemas/analysis_response.v1.schema.json` (defined in Phase 2)

---

### `recommendation.json`

- **Written by:** Python Analytics CLI.
- **Content:** Ranked recommendations with quantified impact:
  - `rank`, `type`, `target_stage_ids[]`, `rationale`
  - `linked_failure_modes[]`
  - `estimated_impact` — `throughput_delta`, `footprint_delta`, `roi_percent`, `payback_years`
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
      "path": "artifacts/a3f2b1c4-.../scenario_snapshot.json",
      "size_bytes": 4096,
      "sha256": "e3b0c44298fc1c...",
      "written_at": "2025-01-15T08:30:01Z"
    }
  ]
}
```

**`engine_versions` is required.** Both `simulator_version` and `analytics_version` must be recorded.

---

### Log Files

- `logs/platform.log` — structured JSON; all pipeline steps, status transitions, adapter decisions
- `logs/simulator.log` — C++ CLI stdout + stderr; first line is engine version string
- `logs/analytics.log` — Python CLI stdout + stderr; first line is engine version string

---

## 5. Scenario Comparison Policy

> **Comparison never re-invokes engines. It reads exclusively from stored artifacts.**

### Inputs
- `baseline_run_id` — a `Completed` run
- `candidate_run_id` — a `Completed` run

### Process
1. Platform reads `analysis_response.json` and `simulation_result.json` from both run directories.
2. Delta metrics computed in-process: `throughput_delta`, `lead_time_delta`, `wip_delta`, `footprint_delta`, `throughput_per_m2_delta`, `roi_delta`, `payback_delta`, `quality_delta`.
3. Failure modes compared: `newly_detected[]`, `resolved[]`.
4. Stage IDs are the stable comparison anchor across all scenarios.

Both runs must be `Completed` with all required artifacts present.

---

## 6. Run ID Convention

- **Format:** UUID v4
- **Generated by:** Platform at run creation
- **Never reused** across runs including failed runs and retries

---

## 7. Database vs Artifact Responsibility

| Data | Stored In | Rationale |
|---|---|---|
| Run lifecycle status, timestamps | Database | Queryable metadata |
| Job status, engine version, timing | Database | Queryable metadata |
| KPI summary values | Database (`run_metrics`) | Fast querying |
| Recommendation summary | Database (`run_recommendations`) | Fast querying |
| Artifact paths and checksums | Database (`run_artifacts`) | Artifact discovery |
| Process structure, stages | `canonical_scenario.json` only | Domain data |
| Product definitions and BOM | `canonical_scenario.json` only | Domain data |
| WorkUnitModel definitions | `canonical_scenario.json` only | Domain data |
| WorkUnit instances and parameters | `canonical_scenario.json` only | Domain data |
| Stage weights | `canonical_scenario.json` only | Computed artifact |
| Stage parameters (defect, rework) | `canonical_scenario.json` only | Domain data |
| Work unit parameters (defect, operating_rate) | `canonical_scenario.json` only | Domain data |
| WIP per stage, blocking, starvation | `simulation_result.json` only | Simulation artifact |
| Quality metrics (defect, rework, scrap) | `simulation_result.json` only | Simulation artifact |
| OEE per unit | `simulation_result.json` only | Simulation artifact |
| Footprint metrics | `simulation_result.json` only | Simulation artifact |
| Failure mode detections | `analysis_response.json` only | Analytics artifact |
| Full recommendations | `recommendation.json` only | Analytics artifact |

---

## 8. gitignore Rules

```gitignore
artifacts/*
!artifacts/.gitkeep
```

Artifacts are runtime data. They must never be committed to the repository.
