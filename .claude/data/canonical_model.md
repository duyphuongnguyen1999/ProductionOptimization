# Production Intelligence & Decision Support System (PIDSS)

## Canonical Model

### 1. Canonical Scenario (Internal Execution Model)

Platform must:

- Validate public scenario against schema
- Adapt version → canonical model
- Compute stage_weights if needed
- Output canonical_scenario.json

Canonical Scenario:

- No version ambiguity
- No oneOf / anyOf / nullable ambiguity
- covered_stage_ids always array on WorkUnitModel
- Explicit integration object when multi-stage
- Engines consume canonical only

Engines (C++ & Python) only read canonical format.

### 2. Canonical Top-Level Structure

```
meta
  version
  random_seed                  ← ALWAYS present

factory
  footprint_limit_m2           ← ALWAYS present
  layout_factor

shifts[]
  shift_id, name
  start_minute_of_day, duration_minutes, net_labor_minutes
  performance_factor
  breaks[]
    start_minute_from_shift_start, duration_minutes
    type: meeting | rest | meal
    coverage_mode: all_stop | staggered
    min_coverage_ratio

days[]
  day_id, name, shift_ids[]
  day_performance_factor, labor_cost_factor
  min_coverage_ratio, max_coverage_ratio

materials[]
  material_id, name

products[]
  product_id, name
  type: "intermediate_product" | "semi_product" | "finished_product"
  bill_of_materials[]          ← REQUIRED on ALL product types
    type: material | intermediate_product | semi_product
    id
    quantity_required_per_output

processes[]
  process_id, name, output_product_id
  stages[]
    stage_id, order, name
    eligible_work_unit_model_ids[]   ← REQUIRED, always present
    input[]  (type + id)
    output[] (type + id)
    stage_parameters
      defect_rate              ← baseline; work_unit may override
      rework
        available              (boolean)
        rework_rate            (fraction of defects that can be reworked)
        maximum_rework_cycles  (integer)
    wip_model                  ← object on all stages except last; null on last
      buffer_id
      capacity_units
      initial_wip_units
      buffer_policy.type: fifo

work_unit_models[]
  model_id, name
  type: manual | semi_auto | auto
  covered_stage_ids[]          ← ALWAYS array, min 1
  operators_per_unit, requires_operator_presence
  footprint_m2, unit_buffer_area_m2
  transfer_delay_sec, batch_size
  cycle_time_default
  integration                  ← REQUIRED when covered_stage_ids.length > 1
    internal_transfer_eliminated
    stage_weights              ← pre-materialized, sum = 1.0
  reliability                  ← optional (unplanned downtime)
    mtbf_hours, mttr_minutes
    useful_life_years, degradation_model
  financial                    ← optional

work_units[]
  work_unit_id                 ← globally unique
  work_unit_model_id           ← references model_id
  cycle_time                   ← actual (may differ from model default)
  age_years
  work_unit_parameters
    defect_rate                ← per-machine observed quality; overrides stage baseline
    operating_rate             ← OEE Performance component (planned/speed loss)

calendar
  meta_data (timezone, aggregation_interval_minutes)
  time_horizon (start_time, end_time)
  overtime[]
  exceptions[]
  demand
    target_output_qty
    planning_unit
    periods[]
```

### 3. Critical Invariants

- NO `schema_version` field
- NO `planning_period` at top level → demand is in `calendar.demand`
- `bill_of_materials[]` REQUIRED on ALL product types (intermediate, semi, finished)
- BOM items carry `quantity_required_per_output`
- `covered_stage_ids[]` ALWAYS array on WorkUnitModel — no `stage_id` singular
- `stage_weights` ALWAYS pre-materialized when `covered_stage_ids.length > 1`
- `eligible_work_unit_model_ids[]` ALWAYS present on every Stage
- `stage_parameters` ALWAYS present on every Stage (defect_rate + rework)
- `work_unit_parameters` ALWAYS present on every WorkUnit (defect_rate + operating_rate)
- `work_unit_id` globally unique
- `wip_model` is null ONLY on the last stage of each process
- `random_seed` ALWAYS present in `meta`
- `factory.footprint_limit_m2` ALWAYS present

### 4. Product Type Rules

- `intermediate_product`: output of a single Stage; consumed by the next Stage only; has BOM listing input materials + prior intermediate product
- `semi_product`: output of a complete Process; referenced in downstream BOM; has BOM listing all consumed materials + intermediate products; may reference another semi_product as input (cross-process dependency)
- `finished_product`: has BOM referencing semi_products + materials; final saleable output
- `material`: defined in materials[]; NOT in products[]; never produced by any Stage

### 5. Defect & Quality Resolution Logic

Simulator uses defect rate with the following precedence:

```
effective_defect_rate =
    work_unit.work_unit_parameters.defect_rate   (if present — per-machine observed)
    else stage.stage_parameters.defect_rate      (stage baseline)
```

Both are ALWAYS present in canonical. The work_unit value is the primary source
(extracted by Data Platform from MES per machine). Stage baseline serves as the
process-design reference and fallback for analytics comparison.

### 6. OEE Component Mapping

PIDSS models three OEE components separately:

```
OEE = Availability × Performance × Quality

Availability  ← work_unit_model.reliability (mtbf/mttr) — unplanned downtime
Performance   ← work_unit.work_unit_parameters.operating_rate — speed/planned loss
Quality       ← work_unit.work_unit_parameters.defect_rate — defect/rework
```

### 7. Adapter Strategy

Only Platform (.NET) handles:

- schema validation
- version adaptation
- canonicalization
- Stage weight computation
- Normalization
- eligible_work_unit_model_ids validation (stage ↔ model compatibility)
- BOM consistency validation across all product types

Simulator & Analytics:

- NEVER parse public schema versions
- NEVER handle version branching
- NEVER compute stage_weights
