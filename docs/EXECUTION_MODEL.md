# PIDSS Equipment-Centric Execution Model

## 1. Core Principle

> **Execution in PIDSS is equipment-centric, not stage-centric.**

Stages define **SOP identity** — they are stable business traceability anchors.
WorkUnits (equipment units) define **execution capacity** — they actually perform the work.

This separation is fundamental to the design.

---

## 2. Stage vs WorkUnit

| Aspect | Stage | WorkUnit |
|---|---|---|
| Purpose | SOP identity & traceability | Execution & capacity |
| Contains | `stage_id`, `order`, `name` | Cycle time, operators, reliability, footprint, financials |
| Deleted on automation? | **Never** | May be replaced |
| Used in bottleneck reporting? | Yes | Indirectly (via covered_stage_ids) |
| A/B comparison anchor? | Yes | No |

---

## 3. Stage Identity Preservation Rule

> **A Stage MUST NEVER be deleted, renamed to an automation label, or replaced by an automated cell.**

Rationale:
- Stages represent SOP steps that remain valid for comparison across scenarios.
- Automation changes **execution** (WorkUnits), not **process identity** (Stages).
- Deleting stages would break A/B comparability and historical KPI traceability.

**Correct modeling when automating Stage 3:**

```
Stages (unchanged):
  stage_id: "manual_assembly"     ← still exists
  stage_id: "manual_connection"   ← still exists

WorkUnit (new):
  unit_id: "integrated_cell_01"
  unit_type: "auto"
  covered_stage_ids: ["manual_assembly", "manual_connection"]
  integration:
    stage_weights:
      manual_assembly: 0.45
      manual_connection: 0.55
```

---

## 4. WorkUnit Types

### Manual

- Operated entirely by human labor
- Stops during breaks
- `operators_per_unit`: typically 1–4
- `requires_operator_presence`: always `true`

### Semi-Auto

- Machine-human coupling
- Machine may wait for operator (or operator may wait for machine)
- Stops during breaks
- `requires_operator_presence`: typically `true`

### Auto

- Machine operates independently
- `requires_operator_presence`: `false` → may continue during breaks
- `requires_operator_presence`: `true` → stops during breaks

---

## 5. Integrated Cell Modeling

When a single automated cell covers multiple SOP stages:

1. Do **not** create new stage definitions
2. Do **not** delete existing stage definitions
3. Create a WorkUnit with `covered_stage_ids` containing all affected stages
4. Provide an `integration` object with `stage_weights`

### stage_weights

Stage weights distribute the unit's cycle time attribution across the covered stages.

Rules:
- All weights must be positive
- Weights must sum to exactly `1.0`
- Platform Adapter computes weights if not provided in the public contract
- The canonical model always contains explicit weights

Purpose of weights:
- Bottleneck reporting per stage
- KPI attribution
- A/B comparison validity

### Example

A cell integrating Pressing (60% time) and Welding (40% time):

```json
{
  "unit_id": "press_weld_cell_01",
  "unit_type": "auto",
  "covered_stage_ids": ["pressing", "welding"],
  "count": 1,
  "cycle_time_mean_sec": 14,
  "operators_per_unit": 0,
  "requires_operator_presence": false,
  "integration": {
    "description": "Integrated press-weld cell",
    "stage_weights": {
      "pressing": 0.60,
      "welding": 0.40
    }
  }
}
```

---

## 6. Line vs Stage Resource Pool

A factory may have N lines, but this does **not** mean each stage has exactly N resources.

Example:
- 7 lines
- Pressing stage: exactly 7 machines
- Manual assembly stage: 14 workbenches

Simulation must model capacity at the **stage resource pool level**, not at a fixed line-unit mapping.

---

## 7. Public vs Canonical Model

The **public contract** may use a simplified representation.

The **canonical model** (engine-facing) always materializes:
- `covered_stage_ids` as an explicit array (never a single string)
- `integration.stage_weights` explicitly when multi-stage
- No version branching or nullable ambiguity

The **Platform Adapter** is responsible for all transformations between public and canonical.
Engines never parse public schema versions.
