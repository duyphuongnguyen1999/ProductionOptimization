# Production Intelligence & Decision Support System (PIDSS)

# 1. CORE BUSINESS PROBLEM

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

- CAPEX / OPEX / Useful life / ROI / Payback period / Footprint impact

---

# 2. REAL FACTORY CONTEXT (FINALIZED DOMAIN REALITY)

## 2.1. Product Hierarchy

PIDSS distinguishes three types of manufactured products and one type of non-manufactured input.

### FinishedProduct
- Final saleable output
- Has `bill_of_materials[]` referencing semi_products and materials
- Produced at the final stage of the final process

### SemiProduct
- Output of a complete Process
- May consume materials AND other semi_products (cross-process dependency)
- Has `bill_of_materials[]` — material-only or material + semi_product
- Produced at the final Stage of its Process

### IntermediateProduct
- Output of a single Stage within a Process
- Consumed only by the next Stage in the same Process
- Has `bill_of_materials[]` with `quantity_required_per_output` for each input
- Must be declared in `products[]`

### Material
- Raw materials or purchased components NOT manufactured in the factory
- Appear as Stage inputs and BOM entries
- Never produced by any Stage
- Defined in `materials[]`, NOT in `products[]`

## 2.2. BOM Rules

ALL product types (intermediate, semi, finished) MUST have `bill_of_materials[]`.

BOM items contain:
- `type`: material | intermediate_product | semi_product
- `id`: references material_id or product_id
- `quantity_required_per_output`: float — units of this input per 1 good output

Purpose: enables Analytics to compute:
- Material consumption per good unit (accounting for defect_rate)
- Cross-stage and cross-process capacity constraints
- WIP buffer sizing accuracy

## 2.3. Process

Transforms inputs → SemiProduct.

`output_product_id` must reference a `semi_product` in `products[]`.

## 2.4. Stage

A stable SOP step within a Process.

Stage contains:

- stage_id (immutable)
- order
- name
- eligible_work_unit_model_ids[] (REQUIRED)
- input[] (materials and products)
- output[] (products)
- stage_parameters (REQUIRED)
- wip_model (object on all but last stage; null on last)

Critical rules:

> Stage identity MUST NEVER be deleted or replaced by automation.
> Stage contains NO execution logic.
> Stage has defect_rate as BASELINE — work_unit may override per-machine.

## 2.5. Stage Parameters

`stage_parameters` is REQUIRED on every Stage:

```json
{
  "defect_rate": 0.012,
  "rework": {
    "available": true,
    "rework_rate": 0.85,
    "maximum_rework_cycles": 2
  }
}
```

- `defect_rate`: fraction of output that is defective (baseline; overridable per work_unit)
- `rework.available`: whether rework is possible at this stage
- `rework.rework_rate`: fraction of defects that can be successfully reworked
- `rework.maximum_rework_cycles`: max number of rework passes allowed

## 2.6. WIP Model (on Stage)

Each Stage except the last carries a `wip_model`:

- buffer_id (unique)
- capacity_units
- initial_wip_units
- buffer_policy (type: "fifo")

Last stage of each Process: `wip_model: null`.

## 2.7. Work Unit Model (Dòng máy)

Template for a class of machines. NOT a physical instance.

Each WorkUnitModel defines:

- model_id
- type (manual / semi_auto / auto)
- covered_stage_ids[] (ALWAYS array, min 1)
- operators_per_unit
- requires_operator_presence
- footprint_m2, unit_buffer_area_m2
- transfer_delay_sec, batch_size
- cycle_time_default
- reliability (optional) — unplanned downtime: mtbf_hours, mttr_minutes, useful_life_years
- financial (optional)
- integration (REQUIRED when covered_stage_ids.length > 1)

A WorkUnitModel is bound to specific stages. Does NOT reuse across different processes.

## 2.8. Work Unit (Máy vật lý cụ thể)

A specific physical machine on the production floor.

Each WorkUnit defines:

- work_unit_id (globally unique)
- work_unit_model_id (reference to model)
- cycle_time (actual — may differ from model default)
- age_years
- work_unit_parameters (REQUIRED)
  - defect_rate: per-machine observed quality from MES
  - operating_rate: OEE Performance component (planned/speed loss)

OEE mapping:
- Availability ← reliability (unplanned downtime, on model)
- Performance  ← operating_rate (planned/speed loss, on work_unit instance)
- Quality      ← defect_rate (on work_unit instance; overrides stage baseline)

## 2.9. Defect Rate Resolution

Simulator resolves effective_defect_rate per work_unit:

```
effective_defect_rate =
    work_unit.work_unit_parameters.defect_rate   (primary — per machine from MES)
    else stage.stage_parameters.defect_rate      (stage baseline)
```

Both fields are ALWAYS present in canonical. The stage baseline serves as process-design
reference for analytics comparison, not as simulation fallback.

## 2.10. Integration Concept

Integration defined by `covered_stage_ids.length > 1` on WorkUnitModel.

- `integration` object REQUIRED when integrated
- `integration.internal_transfer_eliminated`: boolean
- `stage_weights` MUST be pre-materialized by Adapter (sum = 1.0)

## 2.11. Shift, Day, Calendar, Demand

- Shift: working time window with breaks (all_stop | staggered coverage_mode)
- Day: associates day types (weekday/weekend/holiday/special) with shifts + multipliers
- Calendar: maps actual dates to day types, overtime, exceptions
- Demand: in `calendar.demand` with per-period targets

CRITICAL: There is NO top-level `planning_period` field.

## 2.12. Line vs Stage Capacity Reality

7 lines. Pressing & Welding: 7 machines each. Manual stages: more workstations than lines.

Capacity modeled at stage resource pool level (WorkUnits), NOT at fixed line mapping.

## 2.13. Critical Rule — Integrated Automated Cell

When automation covers multiple stages:

- DO NOT delete original stage identity
- Model as WorkUnitModel with covered_stage_ids.length > 1
- Adapter computes and materializes stage_weights
