# ADR-0002: Equipment-Centric Execution Model (Stage vs WorkUnit Separation)

**Status:** Accepted  
**Date:** Phase 0  
**Authors:** PIDSS Platform Team  

---

## Context

PIDSS must evaluate multiple automation strategies — manual optimization, semi-automation, full automation, and integrated cells that combine multiple SOP stages into a single equipment unit.

A naive model might represent automation as "converting Stage type from manual to automated," or model an integrated cell as a distinct entity type. However, both approaches cause critical failures:

- **A/B comparisons break** if Stage identity changes between scenarios.
- **SOP traceability breaks** if Stages are deleted or replaced by automation.
- **Bottleneck reporting breaks** if Stages are merged or removed, because the same stage IDs must appear in every scenario for consistent comparison.
- **Attribution becomes ambiguous** if a multi-stage execution unit has no defined attribution map to its covered stages.

---

## Decision

PIDSS uses a **two-layer execution model** with strict separation between SOP identity and execution capacity.

---

### Layer 1: Stage — SOP Identity Layer

- A `Stage` is a stable SOP step. It is the **unit of business traceability and comparability**.
- **Stage identity is permanent.** A Stage is never deleted, renamed, converted, split, or merged — by any automation strategy or scenario change.
- A Stage contains exactly three fields: `stage_id`, `order`, `name`.
- A Stage contains **no execution logic** — no cycle time, no operator count, no automation type, no capacity field.
- Stages are the stable anchor across all scenarios, enabling valid A/B comparison and consistent bottleneck reporting.

---

### Layer 2: WorkUnit — Execution Layer

A `WorkUnit` is a physical or logical execution resource. It defines all execution parameters.

**WorkUnit fields include:**

- `unit_id` (string, slug)
- `unit_type` — automation level: `manual`, `semi_auto`, `auto`
- `covered_stage_ids[]` — **always an array, minimum one element**
- `count` — number of identical units in the pool
- `cycle_time` — fixed value or distribution (mean, stddev)
- `operators_per_unit` — integer
- `requires_operator_presence` — boolean (affects break behavior)
- `reliability` — optional object (see Reliability section)
- `footprint_m2` — optional
- `financial` — optional (CAPEX, OPEX, useful life)

> **Critical rule: There is no `stage_id` singular field on WorkUnit.**  
> Only `covered_stage_ids[]` exists. It is always an array. For a single-stage WorkUnit, it is a one-element array.

---

### Integration — Defined by Coverage, Not by Type

**Integration is not a WorkUnit type. It is a structural condition.**

A WorkUnit is considered **integrated** if and only if `covered_stage_ids.length > 1`.

| Condition | Meaning |
|---|---|
| `covered_stage_ids.length == 1` | Single-stage WorkUnit |
| `covered_stage_ids.length > 1` | Integrated WorkUnit (covers multiple stages) |

Integration is **orthogonal to automation level.** Any `unit_type` may be integrated:

- A `manual` bench covering two stages = integrated manual unit
- A `semi_auto` machine covering pressing + welding = integrated semi-auto unit
- An `auto` machine covering three stages = integrated auto unit

When `covered_stage_ids.length > 1`, the canonical model **must** include an `integration` object containing:

```json
"integration": {
  "stage_weights": {
    "pressing": 0.40,
    "welding": 0.60
  }
}
```

`stage_weights` values must sum to exactly 1.0.

The `integration` object is **computed by the Platform Adapter** — never by engines.

---

### Stage Weights — Attribution for Multi-Stage Units

When a WorkUnit covers multiple stages, the simulation must attribute performance (throughput, utilization, downtime) to each covered stage for reporting purposes.

- `stage_weights` is the normalized attribution map.
- It is materialized in `canonical_scenario.json` by the Platform Adapter before any engine receives the file.
- Engines consume pre-computed weights — they never perform attribution logic themselves.
- Weights ensure that per-stage KPIs, bottleneck rankings, and A/B comparisons remain valid even when execution spans multiple stages.

---

### Reliability and Lifecycle Modeling

Each WorkUnit may include a `reliability` object:

```json
"reliability": {
  "mtbf_hours": 720,
  "mttr_minutes": 45,
  "age_years": 3,
  "useful_life_years": 10,
  "degradation_model": "linear"
}
```

The Simulation engine uses reliability data to compute:
- Expected availability: `mtbf / (mtbf + mttr/60)`
- Effective throughput reduction due to unplanned downtime

The Analytics engine uses reliability data to:
- Rank WorkUnits by replacement priority (age vs useful life)
- Estimate ROI of replacement investments
- Project capacity gain from reliability improvements

---

### Multi-Process Scope

This execution model applies **per process**. A factory scenario may contain multiple processes, each with their own stages and work units. Within each process, the Stage/WorkUnit separation is identical. BOM links processes through Component outputs and Product inputs, but does not affect the within-process execution model.

---

## Consequences

### Positive

- A/B comparison is always valid: Stage IDs are the stable comparison anchor across all scenarios.
- Bottleneck reporting is consistent: the same `stage_id` values appear in every scenario regardless of automation strategy.
- Automation is modeled as a WorkUnit configuration, not a Stage mutation.
- Integration is naturally expressed by array length — no special type, no flag, no branching.
- Any automation level can integrate any number of stages, including mixed-capability cells.
- Reliability and lifecycle data coexist naturally within WorkUnit, enabling investment ROI analysis.
- Engines are shielded from attribution complexity — they consume pre-computed weights.

### Negative / Trade-offs

- The Platform Adapter carries more responsibility: it must compute stage weights and validate coverage consistency.
- Stage weight computation requires a defined policy (time-proportional, equal-share, or user-specified) — this policy is defined in Phase 2 public schema and Phase 4 adapter implementation.
- The two-layer model is slightly more complex than a flat "stage-has-type" approach, but this complexity is paid once in the adapter, not repeatedly in every engine and reporting component.

---

## Alternatives Considered

### Stage-Type Model

Give each Stage a `type` field (`manual`, `semi_auto`, `auto`) and convert types when modeling automation.

**Rejected** because: Stage type mutation destroys A/B comparability. You cannot compare "scenario A with manual Stage X" against "scenario B with auto Stage X" if Stage X's record has been mutated. The comparison anchor disappears.

### IntegratedCell as a Distinct Type

Introduce `integrated_cell` as a `unit_type` value alongside `manual`, `semi_auto`, `auto`.

**Rejected** because: integration scope and automation level are orthogonal dimensions. Conflating them into a single type field prevents modeling an integrated semi-auto unit, an integrated manual cell, or a single-stage auto unit without artificial workarounds. The `covered_stage_ids.length > 1` definition is both simpler and more expressive.

### Singular `stage_id` Field on WorkUnit

Use a single `stage_id` field for single-stage WorkUnits and a separate `covered_stage_ids[]` for multi-stage units.

**Rejected** because: dual-field designs create branching in all consumers (adapters, engines, reporting). A single `covered_stage_ids[]` that is always an array eliminates all branching. Consumers always iterate the array — length 1 or length N is handled identically.

### Engine-Side Stage Weight Computation

Let each engine compute `stage_weights` from coverage rules.

**Rejected** because: weight computation requires business context (SOP step durations, engineering decisions) that engines should not encode. If different engines compute weights differently, KPI reports become inconsistent. Centralizing computation in the adapter ensures all engines work from identical attribution data.

---

## Related Decisions

- ADR-0001: Run-based append-only model
- ADR-0003: Adapter-based versioning and canonical model stability (stage weight computation is an explicit adapter responsibility)
