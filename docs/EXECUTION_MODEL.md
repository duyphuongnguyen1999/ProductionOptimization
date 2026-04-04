# PIDSS Equipment-Centric Execution Model

## 1. Core Principle

> **Execution in PIDSS is equipment-centric, not stage-centric.**

Stages define **SOP identity** — they are stable business traceability anchors.  
WorkUnitModels define **equipment class characteristics**.  
WorkUnit instances represent **physical machines** with per-machine observed parameters.

---

## 2. Stage vs WorkUnitModel vs WorkUnit

| Aspect | Stage | WorkUnitModel | WorkUnit |
|---|---|---|---|
| Purpose | SOP identity & traceability | Equipment class template | Physical machine instance |
| Contains | `stage_id`, `order`, `name` | Cycle time default, automation type, reliability, footprint, financial | Actual cycle time, age, per-machine quality & performance |
| Deleted on automation? | **Never** | May be replaced | May be replaced |
| A/B comparison anchor? | Yes | No | No |
| Reuse across processes? | N/A | **No** | N/A |

---

## 3. Stage Identity Preservation Rule

> **A Stage MUST NEVER be deleted, renamed to an automation label, or replaced by an automated cell.**

Stages represent SOP steps that remain valid for comparison across scenarios. Automation changes **execution** (WorkUnits), not **process identity** (Stages).

**Correct modeling when automating Stages 3 and 4:**

```
Stages (unchanged):
  stage_id: "manual_assembly"     ← still exists
  stage_id: "manual_connection"   ← still exists

WorkUnitModel (new):
  model_id: "integrated_cell_model"
  type: "auto"
  covered_stage_ids: ["manual_assembly", "manual_connection"]
  integration:
    stage_weights:
      manual_assembly: 0.45
      manual_connection: 0.55

WorkUnit instances (new):
  work_unit_id: "integrated_cell_01"
  work_unit_model_id: "integrated_cell_model"
  work_unit_parameters:
    defect_rate: 0.003
    operating_rate: 0.94
```

---

## 4. WorkUnit Types

### Manual

- Operated entirely by human labor
- Stops during breaks
- `requires_operator_presence`: always `true`

### Semi-Auto

- Machine-human coupling; stops during breaks
- `requires_operator_presence`: typically `true`

### Auto

- Machine operates independently
- `requires_operator_presence: false` → may continue during breaks
- `requires_operator_presence: true` → stops during breaks

---

## 5. Integrated Cell Modeling

When a single automated cell covers multiple SOP stages:

1. Do **not** create new stage definitions
2. Do **not** delete existing stage definitions
3. Create a WorkUnitModel with `covered_stage_ids` containing all affected stages
4. The Platform Adapter computes and embeds `integration.stage_weights`

### stage_weights

Rules:
- All weights must be positive
- Weights must sum to exactly `1.0`
- The Platform Adapter **always** computes and embeds weights before engines receive the file
- Engines never compute attribution — they consume pre-materialized weights

---

## 6. OEE Component Model

PIDSS models all three OEE components explicitly:

```
OEE = Availability × Performance × Quality

Availability  ← WorkUnitModel.reliability (mtbf_hours, mttr_minutes)
                Unplanned downtime — equipment failure and repair
                Derived: availability = mtbf / (mtbf + mttr/60)

Performance   ← WorkUnit.work_unit_parameters.operating_rate
                OEE Performance component — fraction of time running at intended speed
                Planned losses, speed losses, minor stoppages
                Source: observed from MES history per machine

Quality       ← WorkUnit.work_unit_parameters.defect_rate
                Per-machine observed defect rate from MES history
                Overrides stage_parameters.defect_rate (stage baseline)
```

### Defect Rate Resolution

```
effective_defect_rate =
    work_unit.work_unit_parameters.defect_rate   ← primary (per-machine from MES)
    else stage.stage_parameters.defect_rate       ← stage baseline
```

Both fields are **always present** in the canonical model.

---

## 7. Stage Parameters

Every Stage carries `stage_parameters`:

```json
"stage_parameters": {
  "defect_rate": 0.012,
  "rework": {
    "available": true,
    "rework_rate": 0.85,
    "maximum_rework_cycles": 2
  }
}
```

- `defect_rate`: process-design baseline; analytics uses this for scenario comparison
- `rework.available`: whether rework is physically possible at this stage
- `rework.rework_rate`: fraction of defective units that can be successfully reworked
- `rework.maximum_rework_cycles`: maximum rework passes before scrapping

Rework units re-enter the stage buffer and consume additional cycle time. They are tracked separately in simulation output (`rework_units_per_stage`).

---

## 8. WorkUnit Instance Parameters

Every WorkUnit carries `work_unit_parameters`:

```json
"work_unit_parameters": {
  "defect_rate": 0.009,
  "operating_rate": 0.88
}
```

These are **observed per-machine metrics** extracted by the Data Platform from MES history via feature engineering. They reflect the actual historical behavior of each physical machine, as opposed to the process-design assumptions in `stage_parameters`.

---

## 9. Stage Resource Pool Model

A factory may have N lines, but resources are modeled at the **stage resource pool level**, not at a fixed line-unit mapping.

Example:
- 7 lines
- Pressing stage: 7 press machines → pool of 7
- Manual assembly stage: 11 benches → pool of 11

Simulation models capacity at the pool level. A WorkUnit with `count` on its model serves as the pool size — but in the canonical model, individual machines are explicit instances in `work_units[]` grouped by `work_unit_model_id`.

---

## 10. Public vs Canonical Model

The **public contract** (submitted by client) may use a simplified representation.

The **canonical model** (engine-facing, produced by Platform Adapter) always contains:
- `covered_stage_ids[]` as explicit array (never singular string)
- `integration.stage_weights` pre-materialized when multi-stage
- `stage_parameters` on every stage with explicit defect and rework fields
- `work_unit_parameters` on every work_unit instance with defect_rate and operating_rate
- All optional fields resolved to explicit defaults
- No version branching, no nullable ambiguity

The Platform Adapter is the sole authority for all transformations. Engines never parse public schema versions and never compute derived fields.
