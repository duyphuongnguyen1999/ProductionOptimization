# PIDSS Canonical Model Documentation

**Version:** 1.0.0  
**Phase:** 0 — Repository Foundation (Structure + Design Principles)  
**Full Definition:** Phase 1 — Domain & Canonical Model  
**Status:** Active Placeholder

---

## Purpose

This document defines the design principles governing the canonical scenario model — the stable, engine-facing internal execution format consumed exclusively by the C++ Simulator and Python Analytics CLI.

The canonical model is produced by the Platform adapter layer from the public scenario payload. It is **never versioned**, **never contains public-schema branching logic**, and **always contains pre-materialized computed fields**.

Full structural definition (field names, types, nesting, JSON examples) is produced in Phase 1.

---

## What the Canonical Model Covers

The canonical scenario is a complete, unambiguous, self-contained execution specification:

- **Factory-level constraints** — `factory_footprint_limit_m2`, `layout_factor`
- **Multi-process structure** — `processes[]` array, each with stages, work units, and output definition
- **Component and Product definitions** — what each process produces
- **BOM** — top-level `bom[]` linking components to final products with required quantities
- **Stage definitions** — per process: `stage_id`, `order`, `name` only (no execution fields)
- **WorkUnit definitions** — per process: full execution parameters including `covered_stage_ids[]`
- **Stage weights** — pre-materialized `integration.stage_weights` for all multi-stage WorkUnits
- **Flow policy** — `batch_size`, `transfer_delay_sec`, `unit_buffer_area_m2` per stage or process
- **Shift and break calendar** — working days, shift times, break windows
- **Planning demand** — `target_output_qty`, `planning_period`
- **Reliability data** — per WorkUnit (MTBF, MTTR, age, useful life) where provided
- **Financial data** — per WorkUnit (CAPEX, OPEX, useful life) where provided
- **Random seed** — always present

---

## Finalized Design Principles

The following 15 principles are **finalized in Phase 0** and govern the Phase 1 canonical model definition.

### Principle 1 — No Schema Version Field

The canonical model contains no `schema_version` field. It is always the current, stable format. Version handling is exclusively the adapter's responsibility.

### Principle 2 — No OneOf / AnyOf / Nullable Ambiguity

All fields use flat, unambiguous structures. No conditional schemas, no nullable union types. Optional fields have explicit defaults applied by the adapter before serialization.

### Principle 3 — `covered_stage_ids` is Always an Array

WorkUnits link to stages exclusively via `covered_stage_ids[]`. Always present, always an array, minimum one element. No `stage_id` singular field on WorkUnit exists anywhere in the canonical model.

### Principle 4 — Integration is Structural, Not a Type

A WorkUnit with `covered_stage_ids.length > 1` is integrated. No integration type flag. `unit_type` (`manual`, `semi_auto`, `auto`) is orthogonal to integration scope.

### Principle 5 — Stage Weights Always Materialized

When `covered_stage_ids.length > 1`, the canonical WorkUnit **always** contains a pre-computed `integration.stage_weights` map. Engines never compute attribution. The adapter computes, validates (sum = 1.0), and embeds weights before writing `canonical_scenario.json`.

### Principle 6 — Multi-Process Structure at Top Level

The canonical model contains a top-level `processes[]` array. Each process entry contains its own `stages[]`, `work_units[]`, and `output` definition. BOM appears as a separate top-level `bom[]` array.

### Principle 7 — Domain Execution Data Not in Database

BOM, process structure, stage definitions, WorkUnit definitions, and stage weights are stored **only** in `canonical_scenario.json` (and `scenario_snapshot.json`). The relational database stores run metadata, KPI summaries, and artifact index references only.

### Principle 8 — Materialized Timestamps and Normalized Units

All time values in seconds. All timestamps UTC ISO 8601. All capacity values as integers. All transfer delays and batch sizes explicitly stated — never inferred.

### Principle 9 — Explicit Break Behavior

Each WorkUnit explicitly states `requires_operator_presence` (boolean). Engines apply break impact deterministically from this field. No inference required.

### Principle 10 — Reliability Fields are Optional but Structured

When present, reliability follows a defined structure (`mtbf_hours`, `mttr_minutes`, `age_years`, `useful_life_years`, `degradation_model`). When absent, engines treat the WorkUnit as having 100% theoretical availability. Analytics marks reliability data as unavailable but still reports the WorkUnit.

### Principle 11 — Random Seed is Mandatory

Every canonical scenario contains a `random_seed` integer. If the public scenario omits it, the adapter assigns one deterministically (e.g., hash-based or system-generated) and records it. This guarantees full reproducibility for every run.

### Principle 12 — Engines are the Sole Consumers of Canonical

No component other than the C++ Simulator CLI and Python Analytics CLI reads `canonical_scenario.json` for execution. The Platform reads it only for artifact management (indexing, checksum verification). The UI never reads canonical files directly.

### Principle 13 — `factory_footprint_limit_m2` is a Top-Level Factory Field

The hard physical floor space constraint is a factory-level field, not a process or WorkUnit field. It appears at the top level of the canonical scenario. Both the C++ Simulator (for footprint computation) and Python Analytics (for Footprint Constraint Violation detection — FM-08) consume it from this location. The adapter must include it in the canonical model even if the public scenario omits it (by applying a configurable default or requiring it as mandatory in the schema).

### Principle 14 — Flow Policy Fields Required for WIP Estimation

The canonical model must carry all fields required for the C++ Simulator to compute aggregate WIP between stages without discrete-event simulation:

- `batch_size` (per process or stage) — drives batch gating delay and WIP cycle amplitude
- `transfer_delay_sec` (per stage boundary) — contributes to effective wait time
- `unit_buffer_area_m2` (per stage or process) — enables WIP buffer area computation: `WIP_stage × unit_buffer_area_m2`

These fields are explicitly required in the canonical model. The adapter must supply defaults if the public scenario omits them. Engines must not infer flow policy fields from other fields.

### Principle 15 — Simulator Output Scope is Defined by Canonical Input

The canonical model determines the complete scope of what the simulator must produce. Because the canonical model carries footprint fields (`footprint_m2` per WorkUnit, `unit_buffer_area_m2`, `layout_factor`, `factory_footprint_limit_m2`) and flow policy fields (`batch_size`, `transfer_delay_sec`), the simulator is contractually required to output:

- `wip_per_stage`, `total_wip`
- `blocking_time` per stage, `starvation_time` per stage
- `lead_time_estimate` (via Little's Law)
- `machine_area_m2`, `wip_area_m2`, `production_footprint_m2`
- `stage_utilization` per stage
- `effective_availability` per WorkUnit (where reliability data present)

Analytics then consumes these outputs to detect failure modes FM-01 through FM-10, compute `throughput_per_m2`, and evaluate WIP stability.

---

## Cross-Reference

- `DATA_DICTIONARY.md` — entity definitions, field semantics, failure mode definitions, footprint formulas
- `VERSIONING_POLICY.md` — public schema versioning; canonical model has no version
- `ARTIFACT_CONVENTION.md` — where `canonical_scenario.json` is stored; full field inventory of `simulation_result.json` and `analysis_response.json`
- `LINEAGE_POLICY.md` — canonical model's role in the artifact lineage chain; BOM dependency
- `ADR-0002` — why Stage/WorkUnit separation exists; `covered_stage_ids[]` as only linkage
- `ADR-0003` — why adapter owns all computed fields including stage weights and flow policy defaults
- Phase 1 will produce `canonical_scenario.example.json` in `data/contracts/`
