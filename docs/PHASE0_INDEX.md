# PIDSS Phase 0 — Deliverables Index

**Phase:** 0 — Repository Foundation & Data-Layer Conventions
**Version:** 1.0.0
**Status:** Complete

---

## Purpose

Phase 0 establishes all structural, naming, policy, and governance foundations for the PIDSS system. No business logic is implemented. All deliverables are documentation, conventions, and structural definitions that all subsequent phases build upon.

---

## Deliverables

### Documentation [(`docs/`)](docs/)

| File | Version | Status | Purpose |
|---|---|---|---|
| [`docs/REPOSITORY_CONVENTIONS.md`](docs/REPOSITORY_CONVENTIONS.md) | 1.0.0 | Complete | Repository structure, naming conventions, folder ownership, branch/commit rules |
| [`docs/STATUS_MODEL.md`](docs/STATUS_MODEL.md) | 1.0.0 | Complete | Run and Job lifecycle states, transitions, timestamp conventions |
| [`docs/VERSIONING_POLICY.md`](docs/VERSIONING_POLICY.md) | 1.0.0 | Complete | Public schema versioning rules, adapter strategy, compatibility and deprecation policy |
| [`docs/ARTIFACT_CONVENTION.md`](docs/ARTIFACT_CONVENTION.md) | 1.0.0 | Complete | Artifact directory layout, file definitions, append-only policy, scenario comparison policy, manifest format with engine versions |
| [`docs/EXECUTION_MODEL.md`](docs/EXECUTION_MODEL.md) | 1.0.0 | Complete | Equipment-centric execution model, Stage vs WorkUnit separation, integrated cell modeling rules |
| [`docs/NAMING_CONVENTIONS.md`](docs/NAMING_CONVENTIONS.md) | 1.0.0 | Complete | Naming conventions for all layers: JSON fields, enums, files, identifiers, C#, C++, Python, SQL |
| [`docs/PHASE0_INDEX.md`](docs/PHASE0_INDEX.md) | 1.0.0 | Complete | This file — index of all Phase 0 deliverables and finalized decisions |

### Architecture Decision Records [(`docs/adr/`)](docs/adr/)

| File | Version | Status | Decision |
|---|---|---|---|
| [`docs/adr/ADR-0001-run-based-append-only-model.md`](docs/adr/ADR-0001-run-based-append-only-model.md) | 1.0.0 | Complete | Run-based execution; all artifacts immutable and append-only |
| [`docs/adr/ADR-0002-equipment-centric-execution-model.md`](docs/adr/ADR-0002-equipment-centric-execution-model.md) | 1.0.0 | Complete | Stage = SOP identity; WorkUnit = execution via `covered_stage_ids[]` only; integration structural; stage weights by adapter; reliability fields in WorkUnit |
| [`docs/adr/ADR-0003-adapter-based-versioning.md`](docs/adr/ADR-0003-adapter-based-versioning.md) | 1.0.0 | Complete | All version handling and stage weight computation in Platform Adapter; engines consume canonical only |

### Data Governance [(`data/documentation/`)](data/documentation/)

| File | Version | Status | Purpose |
|---|---|---|---|
| [`data/documentation/DATA_DICTIONARY.md`](data/documentation/DATA_DICTIONARY.md) | 1.0.0 | Complete | All domain entities, flow model concepts, footprint model, KPIs, failure modes FM-01–FM-10, reliability fields, status enumerations |
| [`data/documentation/VERSION_REGISTRY.md`](data/documentation/VERSION_REGISTRY.md) | 1.0.0 | Complete | Registry of all public schema versions and lifecycle status |
| [`data/documentation/CANONICAL_MODEL.md`](data/documentation/CANONICAL_MODEL.md) | 1.0.0 | Complete | 15 design principles governing the canonical scenario model |
| [`data/documentation/DATA_LAYER.md`](data/documentation/DATA_LAYER.md) | 1.0.0 | Complete | Purpose and structure of the `data/` governance layer |

### Lineage Policy [(`data/lineage/`)](data/lineage/)

| File | Version | Status | Purpose |
|---|---|---|---|
| [`data/lineage/LINEAGE_POLICY.md`](data/lineage/LINEAGE_POLICY.md) | 1.0.0 | Complete | Artifact lineage, dependency threads, run regeneration requirements, artifact-only A/B comparison policy |

---

## Key Decisions Finalized in Phase 0

1. **Run = UUID + append-only artifact directory** — every execution is immutable and traceable.
2. **Stage identity is permanent** — never deleted, renamed, or converted. SOP traceability preserved across all scenarios.
3. **`covered_stage_ids[]` is the only WorkUnit-to-Stage linkage** — always an array, minimum one element. No `stage_id` singular field exists.
4. **Integration = `covered_stage_ids.length > 1`** — structural condition, not a type. Orthogonal to automation level.
5. **Stage weights computed by Adapter, always pre-materialized in canonical** — engines never compute attribution. Adapter computes weights unconditionally when `covered_stage_ids.length > 1`, validating explicit client-supplied weights if present.
6. **Canonical model is stable and unversioned** — engines consume canonical only, never public schema.
7. **Adapter owns all translation, computation, and normalization** — version handling, stage weights, BOM validation, flow policy defaults, factory constraint fields.
8. **Multi-process structure at canonical top level** — `processes[]` array with own stages/work_units; top-level `bom[]`.
9. **Domain execution data not in database** — process structure, BOM, WorkUnit definitions, stage weights, flow policy in JSON artifacts only.
10. **BOM governs final product capacity** — Analytics evaluates `min(component_throughput / qty_required)` across all BOM entries.
11. **Reliability fields in WorkUnit** — MTBF, MTTR, age, useful life support investment ROI and FM-06/FM-07 detection.
12. **`factory_footprint_limit_m2` is a mandatory top-level canonical field** — used by both simulator (footprint computation) and analytics (FM-08 detection).
13. **Flow policy fields required for WIP estimation** — `batch_size`, `transfer_delay_sec`, `unit_buffer_area_m2` must be present in canonical for WIP and footprint computation.
14. **Simulator must output WIP, blocking, starvation, and footprint metrics** — these are contractually required by the canonical input fields; analytics reads them from artifacts.
15. **10 system-level failure modes are first-class domain concepts** — FM-01 through FM-10 defined in Data Dictionary; Analytics v1 must detect all of them.
16. **A/B comparison is artifact-only** — never re-invokes engines; reads from stored `analysis_response.json` and `simulation_result.json` of both runs.
17. **All timestamps UTC ISO 8601, all times in seconds** — no mixed units.
18. **Repository uses `platform/`, `engines/`, `data_platform/`, `presentation/`, `data_storage/`** — not the legacy `platform_dotnet/`, `simulator_cpp/`, `analytics/` paths.
19. **Primary UI is React (web); WinForms is future desktop client** — `presentation/web/Pidss.Web.React/` is the active UI project.
20. **Data Platform runs offline before the execution pipeline** — `data_platform/` never consumes `canonical_scenario.json`.
21. **ScenarioBuilder is the only component that merges user input + feature store + calibration** — it outputs a public-schema-compliant snapshot, not a canonical model.
22. **DataSources are read-only abstractions** — `IFeatureStoreReader`, `ICalibrationProfileProvider`; no logic, no transformation.

---

## What Phase 0 Does NOT Include

| Item | Phase |
|---|---|
| JSON Schema files (`scenario.v1.schema.json`, etc.) | Phase 2 |
| Canonical scenario example JSON (`canonical_scenario.example.json`) | Phase 1 |
| Domain concept diagram | Phase 1 |
| Database migration scripts | Phase 3 |
| Adapter implementation (C#) | Phase 4 |
| ScenarioBuilder implementation (C#) | Phase 4 |
| C++ simulation engine with WIP/footprint computation | Phase 5 |
| Python analytics with failure mode detection | Phase 6 |
| React UI | Phase 7 |
