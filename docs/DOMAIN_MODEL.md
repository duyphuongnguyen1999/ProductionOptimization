# PIDSS Domain Model

<p align="right">
  🇺🇸 <a href="DOMAIN_MODEL.md">English</a>
  | 🇻🇳 <a href="DOMAIN_MODEL_VI.md">Tiếng Việt</a>
</p>

**Version:** 1.1.0  
**Phase:** 1 — Domain & Canonical Model  
**Status:** Active

---

## 1. Purpose

This document defines the PIDSS domain model — the stable set of concepts, rules, and structures that govern how a manufacturing production system is represented for simulation and analytics.

The domain model is the foundation of the **canonical scenario**: the engine-facing execution format consumed by the C++ Simulator and Python Analytics CLI.

---

## 2. Product Hierarchy

PIDSS distinguishes three types of products manufactured within the factory, plus materials that are not manufactured:

### 2.1 FinishedProduct

The final saleable output of the factory. Assembled or packaged from semi-products and materials.

| Field | Rule |
|---|---|
| `product_id` | string slug, stable |
| `type` | always `"finished_product"` |
| `bill_of_materials[]` | **required** — list of semi-products and materials with quantities |

`bill_of_materials` appears **only** on `finished_product`. It must not appear on `semi_product` or `intermediate_product`.

Final production quantity is constrained by BOM availability:
```
Final Capacity = min over all BOM semi-product entries of
    (semi_product_throughput / quantity_required_per_product)
```

### 2.2 SemiProduct

The output of a complete Process. Consumed by a FinishedProduct BOM or by another Process.

| Field | Rule |
|---|---|
| `product_id` | string slug, stable |
| `type` | always `"semi_product"` |

A SemiProduct is the final output of a Process — it is produced at the last Stage of that Process.

### 2.3 IntermediateProduct

The output of a single Stage within a Process. Consumed only by the next Stage in the same Process.

| Field | Rule |
|---|---|
| `product_id` | string slug, stable |
| `type` | always `"intermediate_product"` |

IntermediateProducts represent in-process WIP between consecutive stages. They must be declared in `products[]` and referenced in stage `input[]` and `output[]`.

### 2.4 Material

Raw materials or purchased components that are **not manufactured** within the factory.

| Field | Rule |
|---|---|
| `material_id` | string slug, stable |
| `name` | human-readable label |

Materials appear as inputs to Stages and as BOM entries in FinishedProducts. They are never produced by any Stage.

---

## 3. Core Structural Entities

### 3.1 Factory

The top-level container for a production scenario.

| Field | Type | Rule |
|---|---|---|
| `footprint_limit_m2` | float | hard physical floor space limit; Analytics detects FM-08 when exceeded |
| `layout_factor` | float | aisle/movement multiplier (default 1.3); range 1.2–1.4 |

### 3.2 Process

A defined manufacturing workflow that transforms inputs into a SemiProduct.

| Field | Type | Rule |
|---|---|---|
| `process_id` | string slug | stable, never renamed |
| `output_product_id` | string | must reference a `semi_product` in `products[]` |
| `stages[]` | array | ordered SOP steps — see Stage definition |

Processes may run in parallel. Final product capacity is constrained by BOM availability across all upstream processes.

### 3.3 Stage — SOP Identity Layer

A Stage is a single, stable SOP step within a Process. **Stages are the unit of business traceability and A/B comparability.**

| Field | Type | Rule |
|---|---|---|
| `stage_id` | string slug | **immutable** — never renamed, deleted, or replaced |
| `order` | integer | position in the SOP sequence |
| `name` | string | human-readable label |
| `eligible_work_unit_model_ids[]` | string[] | **required** — models that may serve this stage |
| `input[]` | array | materials and products entering this stage |
| `output[]` | array | products produced by this stage |
| `wip_model` | object or null | WIP buffer configuration after this stage |

**Critical rule: A Stage contains NO execution logic.**

There is no cycle time, operator count, automation type, or capacity field on a Stage. All execution is defined by WorkUnitModels and WorkUnits.

**Stage Identity Preservation Rule:**

> A Stage MUST NEVER be deleted, renamed to an automation label, or replaced by an automated cell.

Automation changes the WorkUnit configuration, not the Stage definition. Stages are the stable anchor for A/B comparison and bottleneck reporting across all scenarios.

#### Stage Input / Output

Each stage item has `type` and `id`:

| `type` value | `id` references |
|---|---|
| `material` | a `material_id` in `materials[]` |
| `intermediate_product` | a `product_id` in `products[]` where `type == "intermediate_product"` |
| `semi_product` | a `product_id` in `products[]` where `type == "semi_product"` |

The first stage of a process takes materials and/or intermediate products as input.  
The last stage of a process produces a `semi_product`.  
All intermediate stages produce `intermediate_product` and consume the prior stage's output.

#### Stage WIP Model

The `wip_model` defines the WIP buffer **downstream** of this stage (i.e., between this stage and the next):

```json
"wip_model": {
  "buffer_id": "buf_pressing_to_welding",
  "capacity_units": 1200,
  "initial_wip_units": 0,
  "buffer_policy": { "type": "fifo" }
}
```

| Field | Rule |
|---|---|
| `buffer_id` | unique identifier for this buffer |
| `capacity_units` | maximum WIP units the buffer can hold |
| `initial_wip_units` | WIP present at simulation start (typically 0) |
| `buffer_policy.type` | flow discipline: `"fifo"` (default) |

The last stage of a process has `"wip_model": null` — there is no buffer after the final stage output.

### 3.4 WorkUnitModel — Equipment Class Definition

A WorkUnitModel defines the **class characteristics** shared by all physical machines of the same model (dòng máy). It is a template — not a physical instance.

| Field | Type | Rule |
|---|---|---|
| `model_id` | string slug | stable identifier for this equipment class |
| `name` | string | human-readable model name |
| `type` | `manual` \| `semi_auto` \| `auto` | automation level |
| `covered_stage_ids[]` | string[] | **always an array, min 1 element** — stages this model serves |
| `operators_per_unit` | integer | operators required per unit (0 for fully auto) |
| `requires_operator_presence` | boolean | if true, unit stops during breaks |
| `footprint_m2` | float | floor area per physical unit |
| `unit_buffer_area_m2` | float | floor area per WIP unit at this stage's buffer |
| `transfer_delay_sec` | integer | batch handoff delay in seconds |
| `batch_size` | integer | units per transfer batch |
| `cycle_time_default` | object | `mean_sec`, `stddev_sec` — default cycle time for this model |
| `reliability` | object (optional) | `mtbf_hours`, `mttr_minutes`, `useful_life_years`, `degradation_model` |
| `financial` | object (optional) | `capex_usd`, `opex_usd_per_year`, `useful_life_years` |
| `integration` | object (conditional) | **required** when `covered_stage_ids.length > 1` |

**Critical: There is no `stage_id` singular field on WorkUnitModel.**  
Only `covered_stage_ids[]` exists. It is always an array.

A WorkUnitModel is bound to a specific set of stages. It does **not** reuse across different processes.

#### Integration on WorkUnitModel

When `covered_stage_ids.length > 1`, the model **must** carry an `integration` object:

```json
"integration": {
  "internal_transfer_eliminated": true,
  "stage_weights": {
    "pressing": 0.46,
    "welding": 0.54
  }
}
```

| Field | Rule |
|---|---|
| `internal_transfer_eliminated` | boolean — if true, no transfer delay between covered stages |
| `stage_weights` | map of `{ stage_id: float }` — must sum to exactly 1.0 |

Stage weights are **pre-materialized by the Platform Adapter** in the canonical model. Engines never compute attribution.

### 3.5 WorkUnit — Physical Equipment Instance

A WorkUnit represents a **specific physical machine** on the production floor.

| Field | Type | Rule |
|---|---|---|
| `work_unit_id` | string slug | **globally unique** across all work units |
| `work_unit_model_id` | string | reference to a `model_id` in `work_unit_models[]` |
| `cycle_time` | object | `mean_sec`, `stddev_sec` — actual cycle time for this machine (may differ from model default due to wear or calibration) |
| `age_years` | float | current age of this specific machine |

Machines of the same model type may have different actual cycle times due to age, wear, or individual calibration. The `cycle_time` on a WorkUnit overrides `cycle_time_default` from its model.

---

## 4. Shift and Calendar Model

### 4.1 Shift

A Shift defines a working time window within a day.

| Field | Rule |
|---|---|
| `shift_id` | stable identifier |
| `start_minute_of_day` | minutes since midnight (e.g., 360 = 06:00) |
| `duration_minutes` | total shift length |
| `net_labor_minutes` | actual productive minutes after all breaks |
| `performance_factor` | float 0–1.0; scales throughput for this shift (e.g., 0.95 for evening shift) |
| `breaks[]` | list of break definitions within this shift |

#### Break Definition

| Field | Rule |
|---|---|
| `start_minute_from_shift_start` | offset from shift start |
| `duration_minutes` | break length |
| `type` | `"meeting"` \| `"rest"` \| `"meal"` |
| `coverage_mode` | `"all_stop"` — all units stop; `"staggered"` — operators rotate through break |
| `min_coverage_ratio` | minimum fraction of operators that must remain active during staggered break |

Break impact on WorkUnits is determined by `requires_operator_presence`:
- `true` → unit stops during the break window
- `false` → unit continues through breaks (unattended auto only)

### 4.2 Day

A Day definition specifies which shifts run and applies day-level multipliers.

| Field | Rule |
|---|---|
| `day_id` | `weekday`, `weekend`, `holiday`, `special` |
| `shift_ids[]` | which shifts are active on this day type |
| `day_performance_factor` | float 0–1.0; applied on top of shift performance factor |
| `labor_cost_factor` | multiplier for labor cost (e.g., 2.0 for weekend) |
| `min_coverage_ratio` | minimum fraction of workforce that must be present |
| `max_coverage_ratio` | maximum fraction of workforce available |

### 4.3 Calendar

The calendar maps actual dates to day types, declares exceptions and overtime, and contains the demand plan.

| Field | Rule |
|---|---|
| `meta_data.timezone` | IANA timezone string |
| `meta_data.aggregation_interval_minutes` | simulation time step |
| `time_horizon.start_time` | ISO 8601 UTC |
| `time_horizon.end_time` | ISO 8601 UTC |
| `overtime[]` | list of `{ date, type }` — working days that override default type |
| `exceptions[]` | list of `{ date, type, note }` — holidays, shutdowns, special days |

### 4.4 Demand

Demand is contained within `calendar.demand`. There is **no** top-level `planning_period` field.

| Field | Rule |
|---|---|
| `target_output_qty` | total target finished product quantity for the time horizon |
| `planning_unit` | granularity of demand: `"shift"` \| `"day"` |
| `periods[]` | list of `{ period_id, date, shift_id, target_qty }` |

---

## 5. Execution Modeling

### 5.1 Automation Levels

| `type` | Behavior |
|---|---|
| `manual` | Human-operated; stops during breaks |
| `semi_auto` | Machine-human coupling; stops during breaks |
| `auto` | Fully automatic; continues through breaks if `requires_operator_presence = false` |

### 5.2 Batch Flow

- `batch_size`: units transferred per batch per stage boundary
- `transfer_delay_sec`: confirmation/checksheet delay per batch transfer
- Transfer occurs only after a full batch is accumulated

Average WIP at a batch-gated boundary ≈ `batch_size / 2` in steady state.

```
WIP_stage ≈ Throughput × Effective_Wait_Time
Effective_Wait_Time = batch_gating_delay + transfer_delay + downstream_congestion
```

### 5.3 Reliability

| Field | Unit |
|---|---|
| `mtbf_hours` | hours |
| `mttr_minutes` | minutes |
| `useful_life_years` | years |
| `degradation_model` | `"linear"` or null |

Derived: `availability = mtbf / (mtbf + mttr/60)`

When absent, engines treat the WorkUnit as having 100% theoretical availability.

### 5.4 Determinism

`meta.random_seed` is **always present** in the canonical scenario:

```
Same canonical scenario + same seed → identical simulation outputs
```

---

## 6. Production Footprint Model

```
Production_Footprint = (Machine_Area + WIP_Area) × Layout_Factor
```

| Component | Formula |
|---|---|
| `machine_area_m2` | `Σ (count_of_model_instances × model.footprint_m2)` |
| `wip_area_m2` | `Σ (WIP_stage × model.unit_buffer_area_m2)` per stage buffer |
| `production_footprint_m2` | `(machine_area + wip_area) × layout_factor` |

`factory.footprint_limit_m2` is the hard constraint. FM-08 is triggered when `production_footprint_m2 > footprint_limit_m2`.

---

## 7. Canonical Scenario Top-Level Structure

```
canonical_scenario.json
├── meta
│   ├── version                      string
│   └── random_seed                  integer    (always present)
├── factory
│   ├── footprint_limit_m2           float
│   └── layout_factor                float
├── shifts[]
│   ├── shift_id, name
│   ├── start_minute_of_day, duration_minutes, net_labor_minutes
│   ├── performance_factor
│   └── breaks[]
├── days[]
│   ├── day_id, name, shift_ids[]
│   ├── day_performance_factor, labor_cost_factor
│   └── min/max_coverage_ratio
├── materials[]
│   └── material_id, name
├── products[]
│   ├── product_id, name, type       ("intermediate_product" | "semi_product" | "finished_product")
│   └── bill_of_materials[]          (only on finished_product)
├── processes[]
│   ├── process_id, name, output_product_id
│   └── stages[]
│       ├── stage_id, order, name
│       ├── eligible_work_unit_model_ids[]     (required)
│       ├── input[]                            (type + id)
│       ├── output[]                           (type + id)
│       └── wip_model                          (object | null)
├── work_unit_models[]
│   ├── model_id, name, type
│   ├── covered_stage_ids[]                    (always array, min 1)
│   ├── operators_per_unit, requires_operator_presence
│   ├── footprint_m2, unit_buffer_area_m2
│   ├── transfer_delay_sec, batch_size
│   ├── cycle_time_default
│   ├── integration                            (required if covered_stage_ids.length > 1)
│   │   ├── internal_transfer_eliminated
│   │   └── stage_weights                      ({ stage_id: float }, sum = 1.0)
│   ├── reliability                            (optional)
│   └── financial                              (optional)
├── work_units[]
│   ├── work_unit_id                           (globally unique)
│   ├── work_unit_model_id                     (references model_id)
│   ├── cycle_time                             (actual, may differ from model default)
│   └── age_years
└── calendar
    ├── meta_data
    ├── time_horizon
    ├── overtime[]
    ├── exceptions[]
    └── demand
        ├── target_output_qty
        ├── planning_unit
        └── periods[]
```

---

## 8. Canonical Model Invariants

1. No `schema_version` field — canonical is always current
2. No `oneOf`, `anyOf`, nullable ambiguity — all fields flat and unambiguous
3. `covered_stage_ids[]` always an array on every WorkUnitModel — no singular `stage_id`
4. `integration.stage_weights` always pre-materialized when `covered_stage_ids.length > 1`
5. `meta.random_seed` always present
6. All timestamps in UTC ISO 8601; all time durations in minutes or seconds as specified
7. `factory.footprint_limit_m2` always present
8. `eligible_work_unit_model_ids[]` always present on every Stage
9. `bill_of_materials[]` only on `finished_product` — never on `semi_product` or `intermediate_product`
10. No `planning_period` at top level — demand lives in `calendar.demand`
11. `work_unit_id` is globally unique across all work_units
12. Every `work_unit.work_unit_model_id` references a valid entry in `work_unit_models[]`
13. `wip_model` is `null` on the final stage of each process; non-null on all other stages

---

## 9. Cross-Reference

| Document | Location |
|---|---|
| Data Dictionary | `data/documentation/DATA_DICTIONARY.md` |
| Canonical Model Principles | `data/documentation/CANONICAL_MODEL.md` |
| ADR-0002 Equipment-Centric | `docs/adr/ADR-0002-equipment-centric-execution-model.md` |
| ADR-0003 Adapter Versioning | `docs/adr/ADR-0003-adapter-based-versioning.md` |
| Canonical Example | `data/contracts/canonical_scenario.example.json` |
