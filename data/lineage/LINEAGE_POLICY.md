# PIDSS Lineage Policy

**Version:** 1.0.0  
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
        │  [Platform Adapter: version translation, BOM validation]
        │  [Platform Adapter: stage_weights computed and materialized]
        │  [Platform Adapter: multi-process normalization]
        │  [Platform Adapter: factory_footprint_limit_m2 and layout_factor set]
        │  [Platform Adapter: unit_buffer_area_m2 and flow policy defaults applied]
        │  [Platform Adapter: transfer delay adjusted for integrated WorkUnits]
        │  [Platform Adapter: random_seed assigned if absent]
        ▼
canonical_scenario.json
        │  Immutable canonical execution input
        │  Contains: processes[], bom[], factory_footprint_limit_m2,
        │            layout_factor, stage_weights (pre-materialized),
        │            batch_size, transfer_delay_sec, unit_buffer_area_m2,
        │            reliability data, financial data, random_seed
        │
        ├──────────────────────────────────────────────────────────────────┐
        │  [consumed by C++ Simulator]                                     │
        ▼                                                                  │
simulation_result.json                                                     │
  - throughput (per stage, per process)                                    │
  - wip_per_stage, total_wip                                               │
  - lead_time_estimate (Little's Law)                                      │
  - blocking_time (per stage)                                              │
  - starvation_time (per stage)                                            │
  - stage_utilization (per stage)                                          │
  - machine_area_m2                                                        │
  - wip_area_m2                                                            │
  - production_footprint_m2                                                │
  - effective_availability (per WorkUnit)                                  │
production_records.csv                                                     │
  - per-stage, per-period records                                          │
  - attributed via pre-materialized stage_weights                          │
        │                                                                  │
        └──────────────────────┬───────────────────────────────────────────┘
                               │  [consumed by Python Analytics]
                               │  Inputs: canonical_scenario.json +
                               │          simulation_result.json +
                               │          production_records.csv
                               ▼
                    analysis_response.json
                      - Core KPIs (throughput, utilization, capacity)
                      - Footprint KPIs (throughput_per_m2, wip_ratio,
                        footprint_constraint_status)
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

- **C++ Simulator** reads: process structure, WorkUnit definitions, flow policy (`batch_size`, `transfer_delay_sec`, `unit_buffer_area_m2`), reliability data, footprint fields (`footprint_m2`, `layout_factor`, `factory_footprint_limit_m2`), and `random_seed`.
- **Python Analytics** reads: BOM (to compute final product capacity constraints from upstream process throughputs), `factory_footprint_limit_m2` (for FM-08 Footprint Constraint Violation detection), and WorkUnit reliability/financial data (for ROI and replacement analysis).

Analytics cannot run without `canonical_scenario.json` — simulator outputs alone are insufficient.

### 3.2 Footprint KPI Flow

Footprint metrics originate in the C++ Simulator and flow through to Analytics:

```
canonical_scenario.json
  (footprint_m2 per WorkUnit, unit_buffer_area_m2, layout_factor)
        │
        ▼ C++ Simulator computes:
simulation_result.json
  (machine_area_m2, wip_area_m2, production_footprint_m2)
        │
        ▼ Python Analytics reads and derives:
analysis_response.json
  (throughput_per_m2, footprint_constraint_status, footprint_delta in comparison)
```

Analytics does **not** recompute raw footprint values — it reads them from `simulation_result.json` and derives higher-level metrics.

### 3.3 WIP, Blocking, and Starvation Flow

Flow model metrics originate in the C++ Simulator:

```
canonical_scenario.json
  (batch_size, transfer_delay_sec, stage capacities, reliability)
        │
        ▼ C++ Simulator computes:
simulation_result.json
  (wip_per_stage, total_wip, blocking_time, starvation_time, lead_time_estimate)
        │
        ▼ Python Analytics uses for:
analysis_response.json
  (FM-01 Downstream Blocking ← blocking_time)
  (FM-02 Upstream Starvation ← starvation_time)
  (FM-03 Batch Size Mismatch ← batch policy from canonical + wip accumulation)
  (FM-05 WIP Explosion ← total_wip, wip_ratio, lead_time_estimate)
  (wip_stability, wip_growth_rate)
```

### 3.4 BOM → Final Product Capacity

BOM-based capacity computation is an Analytics responsibility:

```
canonical_scenario.json (bom[])
        +
simulation_result.json (throughput per process/component)
        │
        ▼ Python Analytics computes:
  Final product capacity = min over BOM entries of
      (component_throughput / qty_required_per_product)
  Binding upstream constraint (cross-process bottleneck) identified
        │
        ▼ reported in:
analysis_response.json (binding_component, cross_process_bottleneck_stage)
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

1. `canonical_scenario.json` — exact engine input (includes pre-computed stage weights, BOM, flow policy, factory constraints, random seed)
2. `scenario_snapshot.json` — original public input (for audit and potential re-adaptation)
3. `random_seed` embedded in `canonical_scenario.json`
4. C++ simulator binary version (tracked in `jobs.engine_version` and `artifact_manifest.json`)
5. Python analytics CLI version (tracked in `jobs.engine_version` and `artifact_manifest.json`)

> **Note:** `scenario_snapshot.json` alone is not sufficient for reproduction. Re-adapting through an updated adapter could produce a different canonical model (e.g., different default stage weights). The canonical model is the definitive reproducibility artifact.

### Engine Version Tracking

Both engine CLIs emit their version string as the first stdout line at startup:

```
PIDSS-Simulator 1.0.0
PIDSS-Analytics 1.0.0
```

The Platform captures these strings, stores them in `jobs.engine_version`, and writes them into `artifact_manifest.json` under `engine_versions`.

---

## 6. A/B Scenario Comparison Lineage

### Policy

> **Scenario comparison reads exclusively from stored artifacts of both runs. It never re-invokes engines.**

### Lineage

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
                    Comparison delta metrics:
                      throughput_delta, lead_time_delta, wip_delta,
                      footprint_delta, throughput_per_m2_delta,
                      roi_delta, payback_delta
                    Failure mode diff:
                      newly_detected[], resolved[]
```

Stage IDs are the stable comparison anchor — they are consistent across all scenarios, enabling per-stage delta computation.

---

## 7. Artifact Integrity

Each artifact in `artifact_manifest.json` includes a SHA-256 checksum computed at write time. The Platform can verify artifact integrity on demand by recomputing checksums and comparing against the manifest. Any mismatch indicates a violation of the append-only policy.

---

## 8. What Lineage Does NOT Cover

- **Observed data import** (Phase 10) — lineage for observed CSV imports defined separately
- **ML model training lineage** (Phase 9) — model versioning and training run lineage defined separately
- **UI session or user action history** — out of scope
- **Per-unit product routing or WIP traceability** — PIDSS models production in aggregate only; no serial/lot lineage
