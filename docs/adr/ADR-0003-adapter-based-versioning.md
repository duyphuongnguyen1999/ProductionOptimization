# ADR-0003: Adapter-Based Versioning and Canonical Model Stability

**Status:** Accepted  
**Date:** Phase 0 — Revised under Master Prompt Realignment  
**Authors:** PIDSS Platform Team  

---

## Context

PIDSS has multiple components that evolve at different rates:
- **Public API contracts** — evolve as new features and fields are added across phases
- **Simulation engine (C++)** — performance-critical, must remain stable
- **Analytics engine (Python)** — data science logic, must be decoupled from API evolution

Additionally, the canonical model produced for engines requires computed fields — specifically `stage_weights` for multi-stage WorkUnits — that are derived from public input data but are not directly submitted by the client.

The question is: where does version handling live, where does derived-field computation live, and how do we prevent engine complexity from accumulating as the system evolves?

---

## Decision

**All version handling and all canonical model preparation belong exclusively in the Platform adapter layer.**

---

### Rules

1. Every public scenario payload includes a `schema_version` field (e.g., `"1.0"`).
2. The Platform validates the payload against the matching JSON schema in `data/schemas/`.
3. The Platform selects the appropriate adapter class based on `schema_version`.
4. The adapter performs **all** of the following transformations:
   - Translates public field names and structures to canonical equivalents
   - Handles default values for optional public fields
   - Normalizes multi-process structure into canonical array format
   - Validates BOM consistency (component IDs match producing processes, quantities are positive)
   - **Computes `stage_weights` for all WorkUnits where `covered_stage_ids.length > 1`**
   - Materializes `integration.stage_weights` into the canonical WorkUnit before writing `canonical_scenario.json`
   - Reduces or eliminates transfer delay between stages covered by an integrated WorkUnit (per integration policy)
5. The canonical model has **no `schema_version` field.** It is always the current, stable format.
6. Engines (C++ and Python) receive **only** the canonical model. They never see the public payload.
7. Engines must **never** contain schema version branching logic.
8. Engines must **never** compute `stage_weights` or any other attribution logic. They consume pre-materialized values.

---

### Stage Weight Computation — Adapter Responsibility

This is an explicitly named adapter responsibility because it has significant downstream impact.

When a public scenario includes a WorkUnit with `covered_stage_ids.length > 1`, the adapter must:

1. Determine the weight attribution policy (options: time-proportional, equal-share, user-specified — defined in Phase 2 schema).
2. Compute the `stage_weights` map.
3. Validate that weights sum to 1.0 (within floating-point tolerance).
4. Embed the `integration.stage_weights` object into the canonical WorkUnit.

If the public scenario provides explicit weights, the adapter validates and passes them through. If not, the adapter applies the default policy (defined in Phase 2).

**Why this belongs in the adapter and not the engines:**
- Weight computation requires business context (SOP timing data, engineering policy) that engines should not encode.
- If engines computed weights independently, KPI reports from C++ and Python could disagree — breaking consistency.
- Centralizing computation ensures all engines always work from identical, pre-validated attribution data.

---

### Adapter Location

```
platform_dotnet/Pidss.Platform.Api/Adapters/
├─ IScenarioAdapter.cs
├─ ScenarioAdapterV1.cs     # handles schema_version = "1.0"
└─ ScenarioAdapterV2.cs     # (future) handles schema_version = "2.0"
```

---

### Canonical Model Stability

The canonical model is the **stable contract** between Platform and Engines.

- Changes to the canonical model are rare, architectural, and require simultaneous updates to all engine parsers.
- The canonical model is documented in `data/documentation/CANONICAL_MODEL.md`.
- Canonical model changes require an explicit ADR and PR review from the Platform architect.
- The canonical model never contains a `schema_version` field.
- The canonical model never contains `oneOf`, `anyOf`, or nullable ambiguities.

---

## Adapter Responsibility Summary

| Responsibility | Owner |
|---|---|
| Schema version routing | Platform Adapter |
| Field name translation (public → canonical) | Platform Adapter |
| Default value injection for optional fields | Platform Adapter |
| Multi-process structure normalization | Platform Adapter |
| BOM consistency validation | Platform Adapter |
| `stage_weights` computation and materialization | Platform Adapter |
| Transfer delay adjustment for integrated WorkUnits | Platform Adapter |
| `canonical_scenario.json` serialization | Platform Adapter |
| Canonical model parsing | Engines (C++, Python) |
| Simulation execution | C++ Engine |
| KPI computation and recommendation | Python Engine |

---

## Consequences

### Positive

- Engines are completely isolated from API versioning complexity.
- Engines are completely isolated from attribution computation complexity.
- Adding a new schema version requires only a new adapter class — no engine changes.
- All engines work from pre-validated, pre-computed canonical data — no risk of divergent attribution results.
- Engine code remains clean, focused, and free of version-branching or computation logic.
- Public API can evolve rapidly without affecting engine contracts.

### Negative / Trade-offs

- Platform carries more responsibility: validation, adaptation, weight computation, orchestration, artifact management.
- Adapter layer must be well-tested — it is the single point of translation for all inputs.
- Canonical model changes, while rare, require coordinated updates to all engine parsers simultaneously.

---

## Alternatives Considered

### Engines Handle Versioning

Each engine reads `schema_version` and branches internally.

**Rejected:** Contaminates engine code with API evolution logic. Creates unbounded maintenance burden as versions accumulate.

### Engines Compute Stage Weights

Each engine derives `stage_weights` from coverage rules at runtime.

**Rejected:** Engines could compute weights differently, producing inconsistent KPIs. Business context for weight policies belongs in the adapter, not spread across multiple engines. It also creates duplication — the same computation logic would need to exist in both C++ and Python.

### Stage Weights Stored in Public Schema Only

Require clients to always supply explicit `stage_weights` in the public payload.

**Rejected:** This is an implementation detail that clients should not need to manage. It also creates invalid payloads when clients forget or supply incorrect weights. The adapter's default computation policy provides a safe fallback and removes client burden.

---

## Related Decisions

- ADR-0001: Run-based append-only model
- ADR-0002: Equipment-centric execution model (defines why stage weights are needed and what they represent)
