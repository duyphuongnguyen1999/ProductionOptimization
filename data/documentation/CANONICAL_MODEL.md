# PIDSS Canonical Model Documentation

**Version:** 1.0.0  
**Phase:** 1 — Domain & Canonical Model  
**Status:** Active

---

## Purpose

This document defines the design principles governing the canonical scenario model — the stable, engine-facing internal execution format consumed exclusively by the C++ Simulator and Python Analytics CLI.

The canonical model is produced by the Platform adapter layer from the public scenario payload. It is **never versioned**, **never contains public-schema branching logic**, and **always contains pre-materialized computed fields**.

---

## What the Canonical Model Covers

The canonical scenario is a complete, unambiguous, self-contained execution specification:

- **Factory-level constraints** — `factory.footprint_limit_m2`, `factory.layout_factor`
- **Shift and break calendar** — `shifts[]`, `days[]`
- **Materials** — `materials[]` — all non-manufactured inputs
- **Product definitions** — `products[]` — all intermediate, semi, and finished products with their BOM
- **Multi-process structure** — `processes[]` array, each with stages, stage_parameters, wip_models
- **WorkUnitModel definitions** — `work_unit_models[]` — equipment class templates
- **WorkUnit instances** — `work_units[]` — physical machines with per-machine quality and performance
- **Reliability data** — per WorkUnitModel (MTBF, MTTR, age, useful life)
- **Stage parameters** — per Stage: `defect_rate` (baseline), `rework` (available, rate, max cycles)
- **Work unit parameters** — per WorkUnit instance: `defect_rate` (override), `operating_rate`
- **Financial data** — per WorkUnitModel (CAPEX, OPEX, useful life)
- **Demand** — in `calendar.demand` with per-period targets
- **Random seed** — always present in `meta`

---

## Finalized Design Principles

### Principle 1 — No Schema Version Field

The canonical model contains no `schema_version` field. It is always the current, stable format.

### Principle 2 — No OneOf / AnyOf / Nullable Ambiguity

All fields use flat, unambiguous structures. Optional fields have explicit defaults applied by the adapter.

### Principle 3 — `covered_stage_ids` is Always an Array

WorkUnitModels link to stages exclusively via `covered_stage_ids[]`. Always an array, minimum one element. No `stage_id` singular field exists.

### Principle 4 — Integration is Structural, Not a Type

A WorkUnitModel with `covered_stage_ids.length > 1` is integrated. No integration type flag.

### Principle 5 — Stage Weights Always Materialized

When `covered_stage_ids.length > 1`, the canonical WorkUnitModel **always** contains pre-computed `integration.stage_weights`. Engines never compute attribution.

### Principle 6 — BOM is Embedded in Product Definitions

`bill_of_materials[]` is embedded within each product object in `products[]`. It is **not** a separate top-level array. All three product types — `intermediate_product`, `semi_product`, and `finished_product` — carry a `bill_of_materials[]` with `quantity_required_per_output` on every item.

This enables the simulator to:
- Compute accurate material consumption accounting for per-stage defect rates
- Track intermediate product flows between stages
- Model cross-process semi-product dependencies

### Principle 7 — Domain Execution Data Not in Database

BOM, process structure, stage definitions, WorkUnitModel definitions, stage weights, work_unit_parameters are stored **only** in `canonical_scenario.json`. The relational database stores run metadata, KPI summaries, and artifact index references only.

### Principle 8 — Materialized Timestamps and Normalized Units

All time values in seconds. All timestamps UTC ISO 8601. Transfer delays and batch sizes explicitly stated.

### Principle 9 — Explicit Break Behavior

Each WorkUnitModel explicitly states `requires_operator_presence`. Engines apply break impact deterministically.

### Principle 10 — Reliability Fields are Optional but Structured

When present: `mtbf_hours`, `mttr_minutes`, `age_years`, `useful_life_years`, `degradation_model`. Models the Availability component of OEE (unplanned downtime). When absent, engines treat unit as 100% available.

### Principle 11 — Random Seed is Mandatory

Every canonical scenario contains a `meta.random_seed` integer.

### Principle 12 — Engines are the Sole Consumers of Canonical

No component other than the C++ Simulator CLI and Python Analytics CLI reads `canonical_scenario.json` for execution.

### Principle 13 — `factory.footprint_limit_m2` is a Top-Level Factory Field

Hard physical floor space constraint. Both engines consume it from `factory.footprint_limit_m2`.

### Principle 14 — Demand Lives in `calendar.demand`

There is **no** top-level `planning_period` field. All demand information — including `target_output_qty`, `planning_unit`, and per-period targets — lives in `calendar.demand`. The calendar also carries `time_horizon`, `overtime`, and `exceptions`.

### Principle 15 — WorkUnit Model/Instance Separation

Equipment is modeled at two levels:

- **WorkUnitModel** (`work_unit_models[]`): class template — automation type, covered stages, cycle time default, reliability, financial, footprint. Does not reuse across different processes.
- **WorkUnit** (`work_units[]`): physical instance — references its model, carries actual `cycle_time`, `age_years`, and `work_unit_parameters`.

`work_unit_parameters` is **required** on every WorkUnit instance:
- `defect_rate`: per-machine observed quality from MES history; overrides `stage_parameters.defect_rate`
- `operating_rate`: OEE Performance component — fraction of time running at intended speed

### Principle 16 — Stage Parameters are Required on Every Stage

Every stage carries `stage_parameters`:
- `defect_rate`: process-design baseline; used by analytics for comparison; simulator uses work_unit override if present
- `rework`: `available`, `rework_rate`, `maximum_rework_cycles`

### Principle 17 — OEE Components are Explicitly Modeled

```
OEE = Availability × Performance × Quality

Availability  ← work_unit_model.reliability (unplanned downtime)
Performance   ← work_unit.work_unit_parameters.operating_rate
Quality       ← work_unit.work_unit_parameters.defect_rate
```

Defect rate resolution (simulator):
```
effective = work_unit.work_unit_parameters.defect_rate  (primary — per machine)
         ?? stage.stage_parameters.defect_rate           (stage baseline)
```

### Principle 18 — Simulator Output Scope is Defined by Canonical Input

The canonical model determines what the simulator must produce. Both defect/rework and OEE fields are present, so the simulator is contractually required to output quality-adjusted throughput, rework WIP load, and effective availability per WorkUnit.

---

## Cross-Reference

- `DOMAIN_MODEL.md` — entity definitions, field semantics, invariants
- `DATA_DICTIONARY.md` — terminology and KPI definitions
- `VERSIONING_POLICY.md` — public schema versioning; canonical has no version
- `ARTIFACT_CONVENTION.md` — where `canonical_scenario.json` is stored
- `LINEAGE_POLICY.md` — canonical model's role in artifact lineage
- `ADR-0002` — Stage/WorkUnit separation; `covered_stage_ids[]`
- `ADR-0003` — adapter owns all computed fields
- `data/contracts/canonical_scenario.example.json` — full example
