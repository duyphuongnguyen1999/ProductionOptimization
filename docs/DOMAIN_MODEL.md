# PIDSS Domain Model

<p align="right">
  🇺🇸 <a href="DOMAIN_MODEL.md">English</a>
  | 🇻🇳 <a href="DOMAIN_MODEL_VI.md">Tiếng Việt</a>
</p>

**Version:** 1.0.0  
**Phase:** 1 — Domain & Canonical Model  
**Status:** Active

---

## 1. Purpose

This document defines the PIDSS domain model — the stable set of concepts, rules, and structures that govern how a manufacturing production system is represented for simulation and analytics.

The domain model is the foundation of the **canonical scenario**: the engine-facing execution format consumed by the C++ Simulator and Python Analytics CLI.

---

## 2. Core Domain Entities

### 2.1 Factory

The top-level container for a production scenario. A factory has a hard physical floor space limit (`factory_footprint_limit_m2`) that all automation scenarios must respect.

A factory contains one or more **Processes**, connected via a **BOM**.

---

### 2.2 Process

A defined manufacturing workflow that transforms inputs into a **Component** or **Product**.

| Field | Type | Rule |
|---|---|---|
| `process_id` | string slug | stable, never renamed |
| `output.type` | `component` or `product` | defines what this process produces |
| `output.component_id` / `output.product_id` | string slug | consumed by BOM |
| `stages[]` | array | ordered SOP steps |
| `work_units[]` | array | execution resources |
| `batch_size` | integer | units transferred per batch |
| `transfer_delay_sec` | integer | inter-stage handoff delay in seconds |
| `unit_buffer_area_m2` | float | floor area per WIP unit at stage boundaries |

Multiple processes may run in parallel. Final product capacity is constrained by BOM availability across all upstream processes.

---

### 2.3 Component

The output of a process that produces semi-finished goods. Consumed by a downstream process via the BOM.

- Aggregate only — no WIP tracking per unit
- Identified by `component_id` (string slug)
- Not tracked per serial/lot

---

### 2.4 Product (Final Product)

The final manufactured output assembled or packaged from one or more Components.

Final production quantity = `min over all BOM entries of (component_throughput / qty_required_per_product)`

---

### 2.5 BOM (Bill of Materials)

Defines the Components required to produce one unit of a Product.

```json
{
  "product_id": "finished_product_a",
  "component_id": "sub_assembly_unit",
  "quantity_required_per_product": 1
}
```

BOM is stored at the top level of the canonical scenario. It is **never** stored in the relational database.

Analytics uses BOM to identify the binding upstream bottleneck across processes.

---

### 2.6 Stage — SOP Identity Layer

A Stage is a single, stable SOP step within a Process.

**Stages are the unit of business traceability and A/B comparability.**

| Field | Type | Rule |
|---|---|---|
| `stage_id` | string slug | immutable — never renamed, deleted, or replaced |
| `order` | integer | position in the SOP sequence |
| `name` | string | human-readable label |

**Critical rule: A Stage contains NO execution logic.**

There is no cycle time, operator count, automation type, or capacity field on a Stage. All execution is defined by WorkUnits.

Stages are never deleted, renamed to an automation label, or replaced when automation is introduced. Automation changes the WorkUnit configuration, not the Stage definition.

---

### 2.7 WorkUnit — Execution Layer

A WorkUnit is a physical or logical execution resource assigned to one or more Stages.

| Field | Type | Rule |
|---|---|---|
| `unit_id` | string slug | stable identifier |
| `unit_type` | `manual` \| `semi_auto` \| `auto` | automation level |
| `covered_stage_ids[]` | string[] | **always an array, minimum one element** |
| `count` | integer | number of identical units in the pool |
| `cycle_time.mean_sec` | float | mean processing time in seconds |
| `cycle_time.stddev_sec` | float | standard deviation in seconds |
| `operators_per_unit` | integer | operators required per unit (0 for fully auto) |
| `requires_operator_presence` | boolean | if true, unit stops during breaks |
| `reliability` | object (optional) | MTBF, MTTR, age, useful life |
| `footprint_m2` | float (optional) | floor area per unit |
| `financial` | object (optional) | CAPEX, OPEX, useful life for ROI |

> **Critical: There is no `stage_id` singular field on WorkUnit.**  
> Only `covered_stage_ids[]` exists. It is always an array.  
> For a single-stage WorkUnit, it is a one-element array: `["pressing"]`

---

### 2.8 Integration — Multi-Stage Coverage

Integration is **not a WorkUnit type**. It is a **structural condition**.

| Condition | Meaning |
|---|---|
| `covered_stage_ids.length == 1` | Single-stage WorkUnit |
| `covered_stage_ids.length > 1` | Integrated WorkUnit |

Integration is **orthogonal to automation level**:
- A `manual` bench covering two stages = integrated manual unit
- A `semi_auto` machine covering pressing + welding = integrated semi-auto unit
- An `auto` machine covering three stages = integrated auto unit

When `covered_stage_ids.length > 1`, the canonical model **must** include an `integration` object:

```json
"integration": {
  "stage_weights": {
    "packaging_insert": 0.55,
    "packaging_seal": 0.45
  }
}
```

`stage_weights` values must sum to exactly `1.0`.

**Stage weights are computed and materialized by the Platform Adapter** — never by engines.

---

### 2.9 Stage Weights

Stage weights are the normalized attribution map distributing a WorkUnit's execution contribution across its covered Stages.

**Purpose:**
- Bottleneck reporting per stage (engines attribute output to each stage via weights)
- A/B comparison validity (consistent per-stage KPIs across scenarios)
- SOP traceability (execution always traces back to a stage)

**Rules:**
- Required when `covered_stage_ids.length > 1`
- Must be pre-materialized in `canonical_scenario.json` before engines receive it
- Engines never compute attribution — they consume pre-computed weights
- Values must be positive and sum to exactly 1.0

---

## 3. Execution Modeling

### 3.1 Automation Levels

| `unit_type` | Behavior |
|---|---|
| `manual` | Human-operated; stops during breaks |
| `semi_auto` | Machine-human coupling; stops during breaks |
| `auto` | Fully automatic; may continue through breaks if `requires_operator_presence = false` |

### 3.2 Batch Flow

Current production flow is batch-gated:

- `batch_size`: number of units transferred per batch
- `transfer_delay_sec`: delay per inter-stage transfer (checksheet, confirmation)
- Transfer occurs only after a full batch is completed

WIP accumulation at stage boundaries is estimated as:

```
WIP_stage ≈ Throughput × Effective_Wait_Time
```

Where effective wait time includes batch gating delay + transfer delay + downstream congestion.

Average WIP at a batch-gated boundary ≈ `batch_size / 2` in steady state.

### 3.3 Reliability

Each WorkUnit may carry reliability data for downtime modeling and investment ROI analysis:

| Field | Unit | Purpose |
|---|---|---|
| `mtbf_hours` | hours | Mean Time Between Failures |
| `mttr_minutes` | minutes | Mean Time To Repair |
| `age_years` | years | current equipment age |
| `useful_life_years` | years | manufacturer-defined service life |
| `degradation_model` | enum/null | availability decay model |

Derived availability: `mtbf / (mtbf + mttr/60)`

When reliability data is absent, engines treat the WorkUnit as having 100% theoretical availability.

### 3.4 Break Behavior

Break impact is determined entirely by `requires_operator_presence` on each WorkUnit:
- `true` → unit stops during all breaks (manual, semi_auto, and attended auto)
- `false` → unit continues through breaks (unattended auto only)

Engines apply break impact deterministically from this field. No inference required.

### 3.5 Determinism

Every canonical scenario contains a `random_seed` integer. This guarantees:

```
Same canonical scenario + same seed → identical simulation outputs
```

The Platform Adapter assigns a seed if the public scenario omits it.

---

## 4. Production Footprint Model

The canonical model carries all fields required to compute production floor area:

```
Production_Footprint = (Machine_Area + WIP_Area) × Layout_Factor
```

| Component | Formula | Source |
|---|---|---|
| `machine_area_m2` | `Σ (unit.count × unit.footprint_m2)` | canonical WorkUnit fields |
| `wip_area_m2` | `Σ (WIP_stage × unit_buffer_area_m2)` | simulation WIP + canonical field |
| `production_footprint_m2` | `(machine_area + wip_area) × layout_factor` | computed by simulator |

`factory_footprint_limit_m2` is the hard constraint. Analytics detects FM-08 (Footprint Constraint Violation) when `production_footprint_m2 > factory_footprint_limit_m2`.

---

## 5. Canonical Scenario Structure

The canonical scenario is the **stable internal execution contract** between the Platform adapter and all engines.

```
canonical_scenario.json
├── factory_footprint_limit_m2      (float)      hard space constraint
├── layout_factor                   (float)      aisle/movement multiplier (default 1.3)
├── random_seed                     (integer)    always present
├── planning_period
│   ├── start_time                  (ISO 8601 UTC)
│   ├── end_time                    (ISO 8601 UTC)
│   └── target_output_qty           (integer)
├── shift_calendar
│   ├── shifts[]
│   └── breaks[]
├── processes[]
│   ├── process_id                  (string slug)
│   ├── output
│   │   ├── type                    ("component" | "product")
│   │   └── component_id / product_id
│   ├── stages[]
│   │   ├── stage_id                (string slug, immutable)
│   │   ├── order                   (integer)
│   │   └── name                    (string)
│   ├── work_units[]
│   │   ├── unit_id                 (string slug)
│   │   ├── unit_type               ("manual" | "semi_auto" | "auto")
│   │   ├── covered_stage_ids[]     (string[], minItems=1, ALWAYS array)
│   │   ├── count                   (integer)
│   │   ├── cycle_time
│   │   │   ├── mean_sec            (float)
│   │   │   └── stddev_sec          (float)
│   │   ├── operators_per_unit      (integer)
│   │   ├── requires_operator_presence (boolean)
│   │   ├── integration             (object, REQUIRED if covered_stage_ids.length > 1)
│   │   │   └── stage_weights       ({ stage_id: float }, sum = 1.0)
│   │   ├── reliability             (optional)
│   │   │   ├── mtbf_hours
│   │   │   ├── mttr_minutes
│   │   │   ├── age_years
│   │   │   ├── useful_life_years
│   │   │   └── degradation_model
│   │   ├── footprint_m2            (optional float)
│   │   └── financial               (optional)
│   │       ├── capex_usd
│   │       ├── opex_usd_per_year
│   │       └── useful_life_years
│   ├── batch_size                  (integer)
│   ├── transfer_delay_sec          (integer)
│   └── unit_buffer_area_m2         (float)
└── bom[]
    ├── product_id
    ├── component_id
    └── quantity_required_per_product
```

### Canonical Model Invariants

1. No `schema_version` field — the canonical model is always current
2. No `oneOf`, `anyOf`, or nullable ambiguity — all fields are flat and unambiguous
3. `covered_stage_ids[]` is always an array on every WorkUnit — no singular `stage_id` field exists
4. `integration.stage_weights` is always pre-materialized when `covered_stage_ids.length > 1`
5. `random_seed` is always present
6. All time values in seconds; all timestamps in UTC ISO 8601
7. `factory_footprint_limit_m2` is always present at the top level
8. All flow policy fields (`batch_size`, `transfer_delay_sec`, `unit_buffer_area_m2`) are always explicit

---

## 6. Stage Identity Preservation Rule

> **A Stage MUST NEVER be deleted, renamed to an automation label, or replaced by an automated cell.**

When modeling automation that covers Stage `manual_assembly` and `manual_connection`:

**Incorrect:** delete stages and create a new `integrated_auto_stage`

**Correct:**
```json
"stages": [
  { "stage_id": "manual_assembly",   "order": 3, "name": "Manual Assembly" },
  { "stage_id": "manual_connection", "order": 4, "name": "Manual Connection" }
],
"work_units": [
  {
    "unit_id": "integrated_cell_01",
    "unit_type": "auto",
    "covered_stage_ids": ["manual_assembly", "manual_connection"],
    "integration": {
      "stage_weights": {
        "manual_assembly": 0.45,
        "manual_connection": 0.55
      }
    }
  }
]
```

The Stages remain. The WorkUnit changes. Comparability is preserved.

---

## 7. Multi-Process and BOM

A factory scenario may contain multiple Processes running in parallel. Each process produces either a Component or a Product.

The top-level `bom[]` array defines how Components are consumed to produce a final Product:

```
Process A → sub_assembly_unit (component)
                    ↓
               BOM: 1 × sub_assembly_unit → finished_product_a
                    ↓
Process B → finished_product_a (product, final assembly/packaging)
```

The C++ Simulator computes throughput per process. The Python Analytics CLI uses the BOM to compute final product capacity:

```
Final Capacity = min over all BOM entries of
    (component_throughput / qty_required_per_product)
```

---

## 8. Cross-Reference

| Document | Location |
|---|---|
| Data Dictionary (entity definitions) | `data/documentation/DATA_DICTIONARY.md` |
| Canonical Model Principles | `data/documentation/CANONICAL_MODEL.md` |
| Equipment-Centric Execution (ADR) | `docs/adr/ADR-0002-equipment-centric-execution-model.md` |
| Adapter-Based Versioning (ADR) | `docs/adr/ADR-0003-adapter-based-versioning.md` |
| Versioning Policy | `docs/VERSIONING_POLICY.md` |
| Artifact Convention | `docs/ARTIFACT_CONVENTION.md` |
| Canonical Example | `data/contracts/canonical_scenario.example.json` |
| Phase 2 Output | `data/schemas/scenario.v1.schema.json` (Phase 2) |
