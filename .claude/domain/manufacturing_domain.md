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

- CAPEX
- OPEX
- Useful life
- ROI
- Payback period
- Footprint reduction impact

---

# 2. REAL FACTORY CONTEXT (FINALIZED DOMAIN REALITY)

## 2.1. Product Hierarchy

PIDSS distinguishes three types of manufactured products and one type of non-manufactured input.

### FinishedProduct
- The final saleable output of the factory
- Has a `bill_of_materials[]` referencing semi-products and materials
- `bill_of_materials` appears ONLY on finished_product

### SemiProduct
- Output of a complete Process
- Consumed by FinishedProduct BOM or by another Process
- Produced at the final Stage of its Process

### IntermediateProduct
- Output of a single Stage within a Process
- Consumed only by the next Stage in the same Process
- Must be declared in `products[]`

### Material
- Raw materials or purchased components NOT manufactured in the factory
- Appear as Stage inputs and FinishedProduct BOM entries
- Never produced by any Stage

## 2.2. Process

Transforms raw materials and intermediate products → SemiProduct.

Defined by SOP. Rarely changes.

A factory may contain multiple processes.

`output_product_id` must reference a `semi_product` in `products[]`.

## 2.3. Stage

A stable SOP step within a Process.

Stage represents business traceability and comparability.

Stage contains:

- stage_id
- order
- name
- eligible_work_unit_model_ids[] (REQUIRED — models that may serve this stage)
- input[] (materials and products)
- output[] (products)
- wip_model (buffer after this stage; null on last stage)

Critical rules:

> Stage identity MUST NEVER be deleted
> Stage identity MUST NEVER be replaced by automation.
> Stage contains NO execution logic.

## 2.4. WIP Model (on Stage)

Each Stage (except the last in a Process) carries a `wip_model` defining the buffer between it and the next Stage:

- buffer_id
- capacity_units
- initial_wip_units
- buffer_policy (type: "fifo")

The final Stage of each Process has `wip_model: null`.

## 2.5. Work Unit Model (Dòng máy)

Defines the class characteristics shared by all physical machines of the same model.

This is a TEMPLATE, not a physical instance.

Each WorkUnitModel defines:

- model_id
- type (manual / semi_auto / auto)
- covered_stage_ids[] (minItems = 1, ALWAYS array)
- operators_per_unit
- requires_operator_presence
- footprint_m2
- unit_buffer_area_m2
- transfer_delay_sec
- batch_size
- cycle_time_default (mean_sec, stddev_sec)
- reliability (optional)
- financial (optional)
- integration (REQUIRED when covered_stage_ids.length > 1)

A WorkUnitModel is bound to specific stages. It does NOT reuse across different processes.

## 2.6. Work Unit (Máy vật lý cụ thể)

A specific physical machine on the production floor.

Each WorkUnit defines:

- work_unit_id (globally unique)
- work_unit_model_id (reference to model)
- cycle_time (actual — may differ from model default due to age/wear)
- age_years

Machines of the same model may have different actual cycle times.

## 2.7. Integration Concept

Integrated cell is NOT a separate type.

Integration is defined by:

- `covered_stage_ids.length` > 1 on the WorkUnitModel

If multiple stages are covered, then:

- An `integration` object must exist on the WorkUnitModel
- `integration.internal_transfer_eliminated` (boolean)
- Adapter MUST compute `stage_weights`
- `stage_weights` MUST be explicitly materialized in canonical
- `stage_weights` values must sum to exactly 1.0

## 2.8. Shift

A working time window within a day.

Each Shift defines:

- shift_id
- start_minute_of_day
- duration_minutes
- net_labor_minutes
- performance_factor (0–1.0, scales throughput)
- breaks[] with coverage_mode (all_stop | staggered) and min_coverage_ratio

## 2.9. Day

Associates a day type with shift configuration and multipliers.

Day types: weekday, weekend, holiday, special

Each Day defines:

- day_id
- shift_ids[] (which shifts run)
- day_performance_factor
- labor_cost_factor
- min/max_coverage_ratio

## 2.10. Calendar and Demand

Calendar maps actual dates to day types and contains the demand plan.

CRITICAL: There is NO top-level `planning_period` field.
Demand lives exclusively in `calendar.demand`.

`calendar.demand` contains:

- target_output_qty
- planning_unit (shift | day)
- periods[] with per-shift or per-day targets

## 2.11. Line vs Stage Capacity Reality

There are 7 lines.

However:

- Pressing & Welding: exactly 7 machines each
- Manual stages: more workstations than lines

Therefore:

> Capacity constraints must be modeled at stage resource pools (WorkUnits),
> NOT at fixed line mapping.

## 2.12. Batch Flow Reality

Current production flow is batch-gated:

- Batch size: 600 pieces
- Transfer only after full batch completion
- Transfer delay: 3–5 minutes (checksheet/confirmation)

Automation goal includes:

- Reducing transfer delay
- Reducing labor
- Reducing footprint
- Increasing throughput

## 2.13. Critical Rule — Integrated Automated Cell

When one automated cell integrates multiple stages:

- DO NOT create new SOP stages
- DO NOT delete original stage identity
- Model automation as WorkUnitModel with covered_stage_ids.length > 1
- Preserve stage-level comparability for A/B analysis

This ensures:

- A/B comparison validity
- SOP traceability
- Bottleneck reporting consistency
