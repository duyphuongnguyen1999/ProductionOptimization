# ADR-0002: Equipment-Centric Execution Model (Stage vs WorkUnit Separation)

**Status:** Accepted  
**Date:** Phase 0 — Updated Phase 1  
**Authors:** PIDSS Platform Team  

---

## Context

PIDSS must evaluate multiple automation strategies — manual optimization, semi-automation, full automation, and integrated cells that combine multiple SOP stages into a single equipment unit.

A naive model might represent automation as "converting Stage type from manual to automated," or model an integrated cell as a distinct entity type. However, both approaches cause critical failures:

- **A/B comparisons break** if Stage identity changes between scenarios.
- **SOP traceability breaks** if Stages are deleted or replaced by automation.
- **Bottleneck reporting breaks** if Stages are merged or removed, because the same stage IDs must appear in every scenario for consistent comparison.
- **Attribution becomes ambiguous** if a multi-stage execution unit has no defined attribution map to its covered stages.

Additionally, PIDSS must model real-world equipment quality and performance variability: machines of the same model class may produce different defect rates based on their age, maintenance history, and individual calibration. This requires separating equipment class definition from physical instance observation.

---

## Decision

PIDSS uses a **three-layer execution model** with strict separation between SOP identity, equipment class definition, and physical machine observation.

---

### Layer 1: Stage — SOP Identity Layer

- A `Stage` is a stable SOP step. It is the **unit of business traceability and comparability**.
- **Stage identity is permanent.** A Stage is never deleted, renamed, converted, split, or merged.
- A Stage contains: `stage_id`, `order`, `name`, `eligible_work_unit_model_ids[]`, `input[]`, `output[]`, `stage_parameters`, `wip_model`.
- A Stage contains **no execution capacity fields** — no cycle time, no operator count, no automation type.

**Stage Parameters (required on every Stage):**

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

`defect_rate` on Stage is the **process-design baseline** — it represents the designed quality expectation for this SOP step. Physical machines may deviate from this baseline due to age and wear; that deviation is captured at the WorkUnit instance level.

**WIP Model (required on every Stage except last):**

```json
"wip_model": {
  "buffer_id": "buf_pressing_to_welding",
  "capacity_units": 1200,
  "initial_wip_units": 0,
  "buffer_policy": { "type": "fifo" }
}
```

---

### Layer 2: WorkUnitModel — Equipment Class Layer

A `WorkUnitModel` is a **template** representing a class of machines (dòng máy). It defines all characteristics shared by physical machines of this type.

**WorkUnitModel fields include:**

- `model_id` (string, slug)
- `type` — automation level: `manual`, `semi_auto`, `auto`
- `covered_stage_ids[]` — **always an array, minimum one element**
- `operators_per_unit`, `requires_operator_presence`
- `footprint_m2`, `unit_buffer_area_m2`
- `transfer_delay_sec`, `batch_size`
- `cycle_time_default` — `mean_sec`, `stddev_sec`
- `reliability` (optional): `mtbf_hours`, `mttr_minutes`, `useful_life_years`, `degradation_model` — models **Availability** component of OEE (unplanned downtime)
- `financial` (optional): `capex_usd`, `opex_usd_per_year`, `useful_life_years`
- `integration` (required when `covered_stage_ids.length > 1`): `internal_transfer_eliminated`, `stage_weights`

> **Critical rule: There is no `stage_id` singular field on WorkUnitModel.**  
> Only `covered_stage_ids[]` exists. It is always an array.

A WorkUnitModel is bound to a specific set of stages. It does **not** reuse across different processes.

---

### Layer 3: WorkUnit — Physical Machine Instance Layer

A `WorkUnit` represents a **specific physical machine** on the production floor. It carries per-machine observed parameters that may differ from the class defaults.

**WorkUnit fields include:**

- `work_unit_id` (string, slug) — **globally unique** across all work units
- `work_unit_model_id` — references a `model_id` in `work_unit_models[]`
- `cycle_time` — actual cycle time; may differ from `cycle_time_default` on the model due to age or wear
- `age_years` — current age of this specific machine
- `work_unit_parameters` — **required**:
  - `defect_rate`: per-machine observed quality rate from MES history; **overrides** `stage_parameters.defect_rate` for simulation
  - `operating_rate`: OEE Performance component — fraction of time machine runs at intended speed (planned losses, speed losses, minor stoppages)

**Source of work_unit_parameters:** These values are extracted by the Data Platform from MES synthetic database via feature engineering. They reflect the actual historical behavior of each physical machine.

---

### OEE Component Mapping

PIDSS models all three OEE components explicitly:

```
OEE = Availability × Performance × Quality

Availability  ← WorkUnitModel.reliability (unplanned downtime)
Performance   ← WorkUnit.work_unit_parameters.operating_rate
Quality       ← WorkUnit.work_unit_parameters.defect_rate
```

**Defect Rate Resolution (simulator):**

```
effective_defect_rate =
    work_unit.work_unit_parameters.defect_rate   (primary — per-machine observed)
    else stage.stage_parameters.defect_rate       (stage baseline)
```

Both fields are always present in the canonical model. The stage baseline is the process-design reference used by analytics for scenario comparison. The work_unit value is the primary simulation input.

---

### Integration — Defined by Coverage, Not by Type

**Integration is not a WorkUnitModel type. It is a structural condition.**

A WorkUnitModel is considered **integrated** if and only if `covered_stage_ids.length > 1`.

| Condition | Meaning |
|---|---|
| `covered_stage_ids.length == 1` | Single-stage WorkUnitModel |
| `covered_stage_ids.length > 1` | Integrated WorkUnitModel (covers multiple stages) |

Integration is **orthogonal to automation level.** Any `type` may be integrated.

When `covered_stage_ids.length > 1`, the canonical model **must** include an `integration` object:

```json
"integration": {
  "internal_transfer_eliminated": true,
  "stage_weights": {
    "pressing": 0.46,
    "welding": 0.54
  }
}
```

`stage_weights` values must sum to exactly 1.0. The `integration` object is **computed by the Platform Adapter** — never by engines.

---

### Reliability and Lifecycle Modeling

Each WorkUnitModel may include a `reliability` object:

```json
"reliability": {
  "mtbf_hours": 720,
  "mttr_minutes": 45,
  "useful_life_years": 10,
  "degradation_model": "linear"
}
```

The Simulation engine uses reliability data to compute effective availability:
- `availability = mtbf / (mtbf + mttr/60)`

The Analytics engine uses reliability data to:
- Rank WorkUnits by replacement priority
- Estimate ROI of replacement investments
- Project capacity gain from reliability improvements

---

## Consequences

### Positive

- A/B comparison is always valid: Stage IDs are the stable comparison anchor.
- Integration is naturally expressed by array length — no special type, no flag.
- OEE is fully decomposed: reliability (unplanned), operating_rate (planned), defect_rate (quality).
- Per-machine variability is captured at the instance level without polluting the class template.
- Engines are shielded from attribution and OEE decomposition complexity.
- Data Platform MES extraction feeds directly into `work_unit_parameters` — clean lineage.

### Negative / Trade-offs

- The Platform Adapter carries more responsibility: stage weights, work_unit_parameters validation, BOM consistency across all product types.
- The three-layer model is more complex than a flat approach, but complexity is paid once in the adapter.

---

## Alternatives Considered

### Stage-Type Model

Give each Stage a `type` field and convert types when modeling automation.

**Rejected:** Stage type mutation destroys A/B comparability.

### IntegratedCell as a Distinct Type

Introduce `integrated_cell` as a `type` value.

**Rejected:** Integration scope and automation level are orthogonal dimensions.

### Singular `stage_id` Field on WorkUnitModel

Use a single `stage_id` for single-stage and `covered_stage_ids[]` for multi-stage.

**Rejected:** Dual-field designs create branching in all consumers.

### Engine-Side Stage Weight Computation

Let each engine compute `stage_weights` from coverage rules.

**Rejected:** Weight computation requires business context that engines should not encode.

### Single defect_rate Location

Put defect_rate only at stage level or only at work_unit level.

**Rejected:** Stage-only misses per-machine variability observed from MES. WorkUnit-only loses the process-design baseline needed for scenario comparison. Both are required.

---

## Related Decisions

- ADR-0001: Run-based append-only model
- ADR-0003: Adapter-based versioning and canonical model stability
