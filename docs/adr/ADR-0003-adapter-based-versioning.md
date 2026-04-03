# ADR-0003: Adapter-Based Versioning and Canonical Model Stability

**Status:** Accepted  
**Date:** Phase 0 — Updated Phase 1  
**Authors:** PIDSS Platform Team

---

## Context

PIDSS has multiple components that evolve at different rates. The canonical model requires computed fields — `stage_weights` for integrated WorkUnits — and must normalize a richer domain model that includes per-machine quality and performance parameters, BOM on all product types, and stage-level quality baselines.

The question is: where does version handling live, where does derived-field computation live, and how do we prevent engine complexity from accumulating as the system evolves?

---

## Decision

**All version handling and all canonical model preparation belong exclusively in the Platform adapter layer.**

---

### Rules

1. Every public scenario payload includes a `schema_version` field.
2. The Platform validates the payload against the matching JSON schema in `data/schemas/`.
3. The Platform selects the appropriate adapter class based on `schema_version`.
4. The adapter performs **all** of the following transformations:
   - Translates public field names and structures to canonical equivalents
   - Handles default values for optional public fields
   - Normalizes multi-process structure into canonical array format
   - **Validates BOM consistency across all product types** — intermediate_product, semi_product, and finished_product must all carry `bill_of_materials[]` with `quantity_required_per_output`; BOM references must resolve to declared products or materials
   - **Validates `eligible_work_unit_model_ids[]`** — each referenced model must exist in `work_unit_models[]` and must have `covered_stage_ids` consistent with the stage it is eligible for
   - **Computes `stage_weights`** for all WorkUnitModels where `covered_stage_ids.length > 1`; materializes `integration.stage_weights` into the canonical WorkUnitModel
   - **Validates `work_unit_parameters`** — ensures `defect_rate` (0–1) and `operating_rate` (0–1) are present and valid on every WorkUnit instance
   - **Validates `stage_parameters`** — ensures `defect_rate` and `rework` fields are present and valid on every Stage
   - Applies defaults for optional fields (e.g., `operating_rate = 1.0` if not supplied)
   - Reduces or eliminates transfer delay between stages covered by an integrated WorkUnitModel
5. The canonical model has **no `schema_version` field.**
6. Engines receive **only** the canonical model. They never see the public payload.
7. Engines must **never** contain schema version branching logic.
8. Engines must **never** compute `stage_weights` or any other attribution or OEE logic. They consume pre-materialized values.

---

### Stage Weight Computation — Adapter Responsibility

When a public scenario includes a WorkUnitModel with `covered_stage_ids.length > 1`, the adapter must:

1. Determine the weight attribution policy (time-proportional, equal-share, or user-specified).
2. Compute the `stage_weights` map.
3. Validate that weights sum to 1.0.
4. Embed the `integration.stage_weights` object into the canonical WorkUnitModel.

**Why this belongs in the adapter and not the engines:** Weight computation requires business context. Centralizing computation ensures all engines work from identical, pre-validated attribution data.

---

### Adapter Location

```
platform/Pidss.Platform/Adapters/
├─ IScenarioAdapter.cs
├─ ScenarioAdapterV1.cs     # handles schema_version = "1.0"
└─ ScenarioAdapterV2.cs     # (future)
```

---

### Canonical Model Stability

The canonical model is the **stable contract** between Platform and Engines.

- Changes require simultaneous updates to all engine parsers.
- Canonical model never contains a `schema_version` field.
- Canonical model never contains `oneOf`, `anyOf`, or nullable ambiguities.

---

## Adapter Responsibility Summary

| Responsibility | Owner |
|---|---|
| Schema version routing | Platform Adapter |
| Field name translation (public → canonical) | Platform Adapter |
| Default value injection for optional fields | Platform Adapter |
| Multi-process structure normalization | Platform Adapter |
| BOM consistency validation (all product types) | Platform Adapter |
| `quantity_required_per_output` presence validation | Platform Adapter |
| `eligible_work_unit_model_ids[]` validation | Platform Adapter |
| `stage_weights` computation and materialization | Platform Adapter |
| `stage_parameters` validation and default application | Platform Adapter |
| `work_unit_parameters` validation (defect_rate, operating_rate) | Platform Adapter |
| Transfer delay adjustment for integrated WorkUnitModels | Platform Adapter |
| `canonical_scenario.json` serialization | Platform Adapter |
| Canonical model parsing | Engines (C++, Python) |
| Simulation execution | C++ Engine |
| KPI computation and recommendation | Python Engine |

---

## Consequences

### Positive

- Engines are completely isolated from API versioning complexity.
- Engines are completely isolated from attribution computation and OEE decomposition.
- Adding a new schema version requires only a new adapter class — no engine changes.
- All engines work from pre-validated, pre-computed canonical data.
- Clean lineage: Data Platform → `work_unit_parameters` → canonical → simulation.

### Negative / Trade-offs

- Adapter carries more responsibility: validation, adaptation, weight computation, BOM consistency, OEE parameter validation.
- Canonical model changes require coordinated updates to all engine parsers simultaneously.

---

## Related Decisions

- ADR-0001: Run-based append-only model
- ADR-0002: Equipment-centric execution model
