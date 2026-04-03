# PIDSS Lineage Policy

**Version:** 1.1.0  
**Phase:** 0 — Repository Foundation & Data-Layer Conventions  
**Status:** Active

---

## 1. Purpose

This document defines the data lineage model for PIDSS — how artifacts are traced from their source (submitted scenario) through all transformation and computation steps to their final outputs.

Lineage enables:
- **Reproducibility:** Given a `run_id`, any result can be re-derived from its artifact directory alone.
- **Auditability:** Every decision recommendation is permanently traceable to its exact inputs.
- **Debugging:** Failed or unexpected results can be traced to root cause without ambiguity.

---

## 2. Lineage Chain

```
User Submission (public scenario payload, schema_version = "N.M")
        │
        │  [written immediately as]
        ▼
scenario_snapshot.json
        │  Immutable source of record (public format, includes schema_version)
        │
        │  [Platform: validated against JSON schema]
        │  [Platform Adapter: version translation]
        │  [Platform Adapter: BOM validation across all product types]
        │  [Platform Adapter: stage_weights computed and materialized]
        │  [Platform Adapter: multi-process normalization]
        │  [Platform Adapter: factory.footprint_limit_m2 and layout_factor set]
        │  [Platform Adapter: unit_buffer_area_m2 and flow policy defaults applied]
        │  [Platform Adapter: stage_parameters defaults applied if absent]
        │  [Platform Adapter: work_unit_parameters validated]
        │  [Platform Adapter: random_seed assigned if absent]
        ▼
canonical_scenario.json
        │  Immutable canonical execution input
        │  Contains:
        │    meta (version, random_seed)
        │    factory (footprint_limit_m2, layout_factor)
        │    shifts[], days[]
        │    materials[]
        │    products[] — all types with bill_of_materials[] and quantity_required_per_output
        │    processes[] — stages with stage_parameters (defect_rate, rework) and wip_models
        │    work_unit_models[] — templates with reliability, financial, integration
        │    work_units[] — instances with cycle_time, age_years, work_unit_parameters
        │                   (defect_rate override, operating_rate)
        │    calendar — time_horizon, overtime, exceptions, demand
        │
        ├──────────────────────────────────────────────────────────────────┐
        │  [consumed by C++ Simulator]                                     │
        ▼                                                                  │
simulation_result.json                                                     │
  - throughput (quality-adjusted good units, per stage and total)          │
  - wip_per_stage, total_wip                                               │
  - lead_time_estimate (Little's Law)                                      │
  - blocking_time (per stage)                                              │
  - starvation_time (per stage)                                            │
  - stage_utilization (per stage)                                          │
  - defect_units_per_stage, rework_units_per_stage, scrap_units_per_stage  │
  - effective_availability (per work_unit, from MTBF/MTTR)                │
  - effective_oee (per work_unit)                                          │
  - machine_area_m2, wip_area_m2, production_footprint_m2                 │
production_records.csv                                                     │
  - per-stage, per-period records including quality columns                │
  - attributed via pre-materialized stage_weights for integrated units     │
        │                                                                  │
        └──────────────────────┬───────────────────────────────────────────┘
                               │  [consumed by Python Analytics]
                               │  Inputs: canonical_scenario.json +
                               │          simulation_result.json +
                               │          production_records.csv
                               ▼
                    analysis_response.json
                      - Core KPIs (throughput, utilization, capacity)
                      - Quality KPIs (defect_rate, rework_rate, scrap_rate, OEE per unit)
                      - Footprint KPIs (throughput_per_m2, wip_ratio, constraint_status)
                      - WIP stability (wip_stability, wip_growth_rate)
                      - Bottleneck stage identification
                      - Operator utilization (per stage)
                      - failure_modes[] (FM-01 through FM-10)
                    recommendation.json
                      - Ranked recommendations
                      - ROI and payback per recommendation
                      - Linked failure mode codes
                               │
                               │  [Platform post-processing]
                               ▼
                    run_metrics (DB)            ← KPI summary (queryable)
                    run_recommendations (DB)    ← recommendation summary
                    run_artifacts (DB)          ← artifact index + checksums
                    artifact_manifest.json      ← integrity index + engine versions
```

---

## 3. Key Lineage Dependencies

### 3.1 Canonical Scenario → Both Engines

`canonical_scenario.json` is consumed by **both** the C++ Simulator and Python Analytics:

**C++ Simulator** reads:
- Process structure, stage definitions, wip_models
- WorkUnitModel definitions (footprint, flow policy, reliability)
- WorkUnit instances (actual cycle_time, age_years, work_unit_parameters)
- Stage parameters (defect_rate baseline, rework policy)
- Work unit parameters (defect_rate override, operating_rate)
- Calendar and demand

**Python Analytics** reads:
- Product definitions and BOM — to compute final product capacity constraints and material consumption
- `factory.footprint_limit_m2` — for FM-08 Footprint Constraint Violation detection
- WorkUnitModel reliability and financial data — for ROI and replacement analysis
- Stage parameters and work_unit_parameters — for quality analysis and OEE comparison

Analytics cannot run without `canonical_scenario.json` — simulator outputs alone are insufficient.

### 3.2 BOM → Capacity and Material Consumption

BOM-based computation is an Analytics responsibility. All product types carry BOM:

```
canonical_scenario.json (products[].bill_of_materials[])
        +
simulation_result.json (throughput per stage, defect_units_per_stage)
        │
        ▼ Python Analytics computes:
  For intermediate_product:
    material_consumption = throughput × quantity_required_per_output / (1 - defect_rate)

  For semi_product:
    semi_capacity = min(stage_throughputs along the process)
    cross_process_dependency resolved via BOM semi_product references

  For finished_product:
    final_capacity = min over BOM semi_product entries of
        (semi_product_throughput / quantity_required_per_output)
    binding_upstream_constraint identified
        │
        ▼ reported in:
analysis_response.json (binding_component, cross_process_bottleneck_stage,
                        material_consumption_per_unit)
```

### 3.3 Quality Flow

Quality metrics originate from canonical inputs and flow through simulation:

```
canonical_scenario.json
  (stage_parameters.defect_rate — baseline)
  (work_unit.work_unit_parameters.defect_rate — per-machine override)
  (stage_parameters.rework — available, rate, max_cycles)
        │
        ▼ C++ Simulator resolves effective_defect_rate per work_unit:
          effective = work_unit.defect_rate ?? stage.defect_rate
          computes defect_units, rework_units, scrap_units per stage
        │
        ▼ simulation_result.json:
  defect_units_per_stage, rework_units_per_stage, scrap_units_per_stage
        │
        ▼ Python Analytics uses for:
  quality KPIs, OEE per unit, FM detection (FM-06 Reliability Dominance)
  material_consumption adjusted for defect
```

### 3.4 OEE Component Flow

```
canonical_scenario.json
  work_unit_model.reliability (mtbf_hours, mttr_minutes) → Availability
  work_unit.work_unit_parameters.operating_rate           → Performance
  work_unit.work_unit_parameters.defect_rate              → Quality
        │
        ▼ C++ Simulator:
  effective_availability = mtbf / (mtbf + mttr/60)
  effective_oee = availability × operating_rate × (1 - defect_rate)
        │
        ▼ simulation_result.json: effective_availability, effective_oee per unit
        │
        ▼ Python Analytics:
  OEE analysis, FM-06 detection, investment ROI recommendations
```

### 3.5 Footprint KPI Flow

```
canonical_scenario.json
  (footprint_m2 per WorkUnitModel, unit_buffer_area_m2, layout_factor)
        │
        ▼ C++ Simulator computes:
simulation_result.json
  (machine_area_m2, wip_area_m2, production_footprint_m2)
        │
        ▼ Python Analytics reads and derives:
analysis_response.json
  (throughput_per_m2, footprint_constraint_status, footprint_delta in comparison)
```

### 3.6 WIP, Blocking, and Starvation Flow

```
canonical_scenario.json
  (batch_size, transfer_delay_sec, stage capacities, reliability, wip_model per stage)
        │
        ▼ C++ Simulator computes:
simulation_result.json
  (wip_per_stage, total_wip, blocking_time, starvation_time, lead_time_estimate)
        │
        ▼ Python Analytics uses for:
  FM-01 Downstream Blocking, FM-02 Upstream Starvation
  FM-03 Batch Size Mismatch, FM-05 WIP Explosion
  wip_stability, wip_growth_rate
```

---

## 4. Immutability Constraints by Step

| Step | Written By | Immutable After |
|---|---|---|
| `scenario_snapshot.json` | Platform | Immediately on write |
| `canonical_scenario.json` | Platform Adapter | Immediately on write |
| `simulation_result.json` | C++ CLI | On job completion |
| `production_records.csv` | C++ CLI | On job completion |
| `analysis_response.json` | Python CLI | On job completion |
| `recommendation.json` | Python CLI | On job completion |
| `artifact_manifest.json` | Platform | On run completion |
| DB: `run_metrics` | Platform | On run completion |
| DB: `run_recommendations` | Platform | On run completion |
| DB: `run_artifacts` | Platform | On run completion |

---

## 5. Run Reproducibility Requirements

A run is **fully reproducible** if all of the following are preserved:

1. `canonical_scenario.json` — exact engine input (includes pre-computed stage weights, BOM with quantities, stage parameters, work_unit_parameters, random seed)
2. `scenario_snapshot.json` — original public input (for audit)
3. `meta.random_seed` embedded in `canonical_scenario.json`
4. C++ simulator binary version (tracked in `jobs.engine_version` and `artifact_manifest.json`)
5. Python analytics CLI version (tracked in `jobs.engine_version` and `artifact_manifest.json`)

### Engine Version Tracking

Both engine CLIs emit their version string as the first stdout line:

```
PIDSS-Simulator 1.0.0
PIDSS-Analytics 1.0.0
```

---

## 6. A/B Scenario Comparison Lineage

> **Comparison reads exclusively from stored artifacts. It never re-invokes engines.**

```
baseline run_id ──► artifacts/{baseline_run_id}/
                      analysis_response.json
                      simulation_result.json
                              │
                              ├── [Platform or Analytics comparison mode]
                              │
candidate run_id ──► artifacts/{candidate_run_id}/
                      analysis_response.json
                      simulation_result.json
                              │
                              ▼
                    Delta metrics:
                      throughput_delta, lead_time_delta, wip_delta,
                      footprint_delta, throughput_per_m2_delta,
                      roi_delta, payback_delta, quality_delta
                    Failure mode diff:
                      newly_detected[], resolved[]
```

Stage IDs are the stable comparison anchor across all scenarios.

---

## 7. Artifact Integrity

Each artifact in `artifact_manifest.json` includes a SHA-256 checksum computed at write time.

---

## 8. What Lineage Does NOT Cover

- **Observed data import** (Phase 10)
- **ML model training lineage** (Phase 9)
- **UI session or user action history**
- **Per-unit product routing or WIP traceability** — PIDSS models production in aggregate only
