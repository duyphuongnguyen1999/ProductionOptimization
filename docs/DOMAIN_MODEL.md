# PIDSS Domain Model

<p align="right">
  🇺🇸 <a href="DOMAIN_MODEL.md">English</a>
  | 🇻🇳 <a href="DOMAIN_MODEL_VI.md">Tiếng Việt</a>
</p>

**Version:** 1.2.0  
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
| `bill_of_materials[]` | **required** — list of semi-products and materials with `quantity_required_per_output` |

### 2.2 SemiProduct

The output of a complete Process. Consumed by a FinishedProduct BOM or by another Process (cross-process dependency).

| Field | Rule |
|---|---|
| `product_id` | string slug, stable |
| `type` | always `"semi_product"` |
| `bill_of_materials[]` | **required** — lists all materials and any upstream semi-products consumed to produce 1 unit |

A SemiProduct BOM may reference other SemiProducts when a process consumes the output of a prior process.

Final production quantity constraint:
```
Final Capacity = min over all BOM semi-product entries of
    (semi_product_throughput / quantity_required_per_output)
```

### 2.3 IntermediateProduct

The output of a single Stage within a Process. Consumed only by the next Stage in the same Process.

| Field | Rule |
|---|---|
| `product_id` | string slug, stable |
| `type` | always `"intermediate_product"` |
| `bill_of_materials[]` | **required** — lists inputs (material + prior intermediate_product) with `quantity_required_per_output` |

The BOM of an IntermediateProduct enables the simulator to compute accurate material consumption accounting for defect rates — without it, the engine cannot know how many raw materials are consumed per good output unit.

### 2.4 Material

Raw materials or purchased components that are **not manufactured** within the factory.

| Field | Rule |
|---|---|
| `material_id` | string slug, stable |
| `name` | human-readable label |

Materials appear as inputs to Stages and as BOM entries. They are defined in `materials[]`, **not** in `products[]`, and are never produced by any Stage.

---

## 3. Core Structural Entities

### 3.1 Factory

| Field | Type | Rule |
|---|---|---|
| `footprint_limit_m2` | float | hard physical floor space limit; Analytics detects FM-08 when exceeded |
| `layout_factor` | float | aisle/movement multiplier (default 1.3); range 1.2–1.4 |

### 3.2 Process

| Field | Type | Rule |
|---|---|---|
| `process_id` | string slug | stable, never renamed |
| `output_product_id` | string | must reference a `semi_product` in `products[]` |
| `stages[]` | array | ordered SOP steps |

### 3.3 Stage — SOP Identity Layer

**Stages are the unit of business traceability and A/B comparability.**

| Field | Type | Rule |
|---|---|---|
| `stage_id` | string slug | **immutable** — never renamed, deleted, or replaced |
| `order` | integer | position in the SOP sequence |
| `name` | string | human-readable label |
| `eligible_work_unit_model_ids[]` | string[] | **required** — models that may serve this stage |
| `input[]` | array | materials and products entering this stage |
| `output[]` | array | products produced by this stage |
| `stage_parameters` | object | **required** — defect and rework parameters |
| `wip_model` | object or null | WIP buffer after this stage; null on last stage |

**Critical: A Stage contains NO execution logic.** No cycle time, operator count, or capacity field.

**Stage Identity Preservation Rule:**

> A Stage MUST NEVER be deleted, renamed, or replaced by an automated cell.

#### Stage Parameters

`stage_parameters` is **required** on every Stage:

| Field | Type | Rule |
|---|---|---|
| `defect_rate` | float | fraction of output that is defective (0.0–1.0); serves as baseline — work_unit may override |
| `rework.available` | boolean | whether rework is possible at this stage |
| `rework.rework_rate` | float | fraction of defective units that can be successfully reworked |
| `rework.maximum_rework_cycles` | integer | maximum rework passes allowed per unit |

#### Stage Input / Output

| `type` value | `id` references |
|---|---|
| `material` | `material_id` in `materials[]` |
| `intermediate_product` | `product_id` in `products[]` where type = `intermediate_product` |
| `semi_product` | `product_id` in `products[]` where type = `semi_product` |

First stage: takes materials and/or intermediate products.  
Last stage: produces a `semi_product`.  
Intermediate stages: produce `intermediate_product`, consume prior stage output.

#### Stage WIP Model

```json
"wip_model": {
  "buffer_id": "buf_pressing_to_welding",
  "capacity_units": 1200,
  "initial_wip_units": 0,
  "buffer_policy": { "type": "fifo" }
}
```

Last stage of each process: `"wip_model": null`.

### 3.4 WorkUnitModel — Equipment Class Definition

A WorkUnitModel is a **template** for a class of machines, defines the **class characteristics** shared by all physical machines of the same model, not a physical instance.

| Field | Type | Rule |
|---|---|---|
| `model_id` | string slug | stable |
| `type` | `manual` \| `semi_auto` \| `auto` | automation level |
| `covered_stage_ids[]` | string[] | **always an array, min 1 element** |
| `operators_per_unit` | integer | 0 for fully auto |
| `requires_operator_presence` | boolean | if true, unit stops during breaks |
| `footprint_m2` | float | floor area per physical unit |
| `unit_buffer_area_m2` | float | floor area per WIP unit at this stage's buffer |
| `transfer_delay_sec` | integer | batch handoff delay in seconds |
| `batch_size` | integer | units per transfer batch |
| `cycle_time_default` | object | `mean_sec`, `stddev_sec` |
| `reliability` | object (optional) | unplanned downtime: `mtbf_hours`, `mttr_minutes`, `useful_life_years`, `degradation_model` |
| `financial` | object (optional) | `capex_usd`, `opex_usd_per_year`, `useful_life_years` |
| `integration` | object (conditional) | **required** when `covered_stage_ids.length > 1` |

**No `stage_id` singular field.** Only `covered_stage_ids[]`.  
**No reuse across different processes.**

#### Integration

When `covered_stage_ids.length > 1`:

```json
"integration": {
  "internal_transfer_eliminated": true,
  "stage_weights": { "pressing": 0.46, "welding": 0.54 }
}
```

Stage weights pre-materialized by Platform Adapter. Engines never compute attribution.

### 3.5 WorkUnit — Physical Equipment Instance

| Field | Type | Rule |
|---|---|---|
| `work_unit_id` | string slug | **globally unique** |
| `work_unit_model_id` | string | references `model_id` in `work_unit_models[]` |
| `cycle_time` | object | `mean_sec`, `stddev_sec` — actual (overrides model default) |
| `age_years` | float | current age |
| `work_unit_parameters` | object | **required** — per-machine quality and performance |

#### Work Unit Parameters

| Field | Type | Rule |
|---|---|---|
| `defect_rate` | float | per-machine observed quality from MES; **overrides** `stage_parameters.defect_rate` |
| `operating_rate` | float | OEE Performance component — fraction of time machine runs at intended speed |

---

## 4. OEE Component Model

PIDSS models three OEE components separately:

```
OEE = Availability × Performance × Quality

Availability  ← work_unit_model.reliability (unplanned downtime — MTBF/MTTR)
Performance   ← work_unit.work_unit_parameters.operating_rate (planned/speed loss)
Quality       ← work_unit.work_unit_parameters.defect_rate (defect/rework)
```

### Defect Rate Resolution Logic

The simulator uses the following precedence:

```
effective_defect_rate =
    work_unit.work_unit_parameters.defect_rate   ← primary (per-machine from MES)
    else stage.stage_parameters.defect_rate       ← stage baseline
```

Both fields are **always present** in canonical. The stage baseline is the process-design reference used by analytics for scenario comparison. The work_unit value is the observed per-machine metric extracted by Data Platform from MES history.

---

## 5. Shift and Calendar Model

### 5.1 Shift

| Field | Rule |
|---|---|
| `shift_id` | stable |
| `start_minute_of_day` | minutes since midnight |
| `duration_minutes` | total shift length |
| `net_labor_minutes` | productive minutes after breaks |
| `performance_factor` | 0–1.0; scales throughput |
| `breaks[]` | break list with `coverage_mode`: `all_stop` or `staggered` |

### 5.2 Day

| Field | Rule |
|---|---|
| `day_id` | `weekday`, `weekend`, `holiday`, `special` |
| `shift_ids[]` | active shifts for this day type |
| `day_performance_factor` | multiplier on top of shift performance |
| `labor_cost_factor` | e.g., 2.0 for weekend |

### 5.3 Calendar

Maps actual dates → day types, with overtime and exceptions.

### 5.4 Demand

Lives in `calendar.demand`. **No** top-level `planning_period`.

| Field | Rule |
|---|---|
| `target_output_qty` | total target finished product quantity |
| `planning_unit` | `"shift"` \| `"day"` |
| `periods[]` | `{ period_id, date, shift_id, target_qty }` |

---

## 6. Production Footprint Model

```
Production_Footprint = (Machine_Area + WIP_Area) × Layout_Factor

Machine_Area = Σ (count_of_instances × model.footprint_m2)
WIP_Area     = Σ (WIP_stage × model.unit_buffer_area_m2)
```

FM-08 triggered when `production_footprint_m2 > factory.footprint_limit_m2`.

---

## 7. Canonical Scenario Top-Level Structure

```
canonical_scenario.json
├── meta
│   ├── version
│   └── random_seed                         (always present)
├── factory
│   ├── footprint_limit_m2                  (always present)
│   └── layout_factor
├── shifts[]
├── days[]
├── materials[]
│   └── material_id, name
├── products[]
│   ├── product_id, name, type
│   └── bill_of_materials[]                 (REQUIRED on ALL product types)
│       ├── type, id
│       └── quantity_required_per_output
├── processes[]
│   ├── process_id, name, output_product_id
│   └── stages[]
│       ├── stage_id, order, name
│       ├── eligible_work_unit_model_ids[]  (required)
│       ├── input[], output[]
│       ├── stage_parameters               (required)
│       │   ├── defect_rate
│       │   └── rework { available, rework_rate, maximum_rework_cycles }
│       └── wip_model                       (object | null)
├── work_unit_models[]
│   ├── model_id, name, type
│   ├── covered_stage_ids[]                 (always array, min 1)
│   ├── operators_per_unit, requires_operator_presence
│   ├── footprint_m2, unit_buffer_area_m2
│   ├── transfer_delay_sec, batch_size
│   ├── cycle_time_default
│   ├── integration                         (required if covered_stage_ids.length > 1)
│   │   ├── internal_transfer_eliminated
│   │   └── stage_weights                   (sum = 1.0)
│   ├── reliability                         (optional)
│   └── financial                           (optional)
├── work_units[]
│   ├── work_unit_id                        (globally unique)
│   ├── work_unit_model_id
│   ├── cycle_time
│   ├── age_years
│   └── work_unit_parameters               (required)
│       ├── defect_rate                     (overrides stage baseline)
│       └── operating_rate                  (OEE Performance)
└── calendar
    ├── meta_data, time_horizon
    ├── overtime[], exceptions[]
    └── demand { target_output_qty, planning_unit, periods[] }
```

---

## 8. Canonical Model Invariants

1. No `schema_version` field
2. No `oneOf`, `anyOf`, nullable ambiguity
3. `covered_stage_ids[]` always an array on WorkUnitModel
4. `integration.stage_weights` always pre-materialized when `covered_stage_ids.length > 1`
5. `meta.random_seed` always present
6. `factory.footprint_limit_m2` always present
7. `eligible_work_unit_model_ids[]` always present on every Stage
8. `stage_parameters` always present on every Stage
9. `work_unit_parameters` (defect_rate + operating_rate) always present on every WorkUnit
10. `bill_of_materials[]` required on **all** product types — intermediate, semi, finished
11. All BOM items carry `quantity_required_per_output`
12. No `planning_period` at top level — demand in `calendar.demand`
13. `work_unit_id` globally unique
14. `wip_model` is `null` only on the final stage of each process

---

## 9. Cross-Reference

| Document | Location |
|---|---|
| Data Dictionary | `data/documentation/DATA_DICTIONARY.md` |
| Canonical Model Principles | `data/documentation/CANONICAL_MODEL.md` |
| ADR-0002 Equipment-Centric | `docs/adr/ADR-0002-equipment-centric-execution-model.md` |
| ADR-0003 Adapter Versioning | `docs/adr/ADR-0003-adapter-based-versioning.md` |
| Canonical Example | `data/contracts/canonical_scenario.example.json` |
