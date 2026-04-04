# PIDSS Phase 0 — Deliverables Index

**Phase:** 0 — Repository Foundation & Data-Layer Conventions  
**Version:** 1.1.0  
**Status:** Complete — Updated Phase 1

---

## Purpose

Phase 0 establishes all structural, naming, policy, and governance foundations for the PIDSS system. Phase 1 extended the domain model; this index reflects the combined finalized decisions.

---

## Deliverables

### Documentation [`docs/`](docs/)

| File | Version | Status | Purpose |
|---|---|---|---|
| [`docs/REPOSITORY_CONVENTIONS.md`](docs/REPOSITORY_CONVENTIONS.md) | 1.0.0 | Complete | Repository structure, naming conventions, folder ownership |
| [`docs/STATUS_MODEL.md`](docs/STATUS_MODEL.md) | 1.0.0 | Complete | Run and Job lifecycle states, transitions, timestamp conventions |
| [`docs/VERSIONING_POLICY.md`](docs/VERSIONING_POLICY.md) | 1.0.0 | Complete | Public schema versioning rules, adapter strategy |
| [`docs/ARTIFACT_CONVENTION.md`](docs/ARTIFACT_CONVENTION.md) | 1.1.0 | Complete | Artifact directory layout, file definitions, append-only policy |
| [`docs/EXECUTION_MODEL.md`](docs/EXECUTION_MODEL.md) | 1.1.0 | Complete | Three-layer execution model: Stage / WorkUnitModel / WorkUnit |
| [`docs/NAMING_CONVENTIONS.md`](docs/NAMING_CONVENTIONS.md) | 1.0.0 | Complete | Naming conventions for all layers |
| [`docs/DOMAIN_MODEL.md`](docs/DOMAIN_MODEL.md) | 1.2.0 | Complete | Full domain model: products, BOM, stages, OEE, defect/rework |
| [`docs/PHASE0_INDEX.md`](docs/PHASE0_INDEX.md) | 1.1.0 | Complete | This file |

### Architecture Decision Records [`docs/adr/`](docs/adr/)

| File | Version | Status | Decision |
|---|---|---|---|
| [`docs/adr/ADR-0001-run-based-append-only-model.md`](docs/adr/ADR-0001-run-based-append-only-model.md) | 1.0.0 | Complete | Run-based execution; all artifacts immutable and append-only |
| [`docs/adr/ADR-0002-equipment-centric-execution-model.md`](docs/adr/ADR-0002-equipment-centric-execution-model.md) | 1.1.0 | Complete | Three-layer model: Stage / WorkUnitModel / WorkUnit; OEE decomposition |
| [`docs/adr/ADR-0003-adapter-based-versioning.md`](docs/adr/ADR-0003-adapter-based-versioning.md) | 1.1.0 | Complete | Adapter owns all canonical preparation including BOM, stage_parameters, work_unit_parameters |

### Data Governance [`data/documentation/`](data/documentation/)

| File | Version | Status | Purpose |
|---|---|---|---|
| [`data/documentation/DATA_DICTIONARY.md`](data/documentation/DATA_DICTIONARY.md) | 1.1.0 | Complete | All domain entities, KPIs, failure modes, OEE fields |
| [`data/documentation/VERSION_REGISTRY.md`](data/documentation/VERSION_REGISTRY.md) | 1.0.0 | Complete | Registry of all public schema versions |
| [`data/documentation/CANONICAL_MODEL.md`](data/documentation/CANONICAL_MODEL.md) | 1.1.0 | Complete | 18 design principles governing the canonical scenario model |
| [`data/documentation/DATA_LAYER.md`](data/documentation/DATA_LAYER.md) | 1.0.0 | Complete | Purpose and structure of the `data/` governance layer |

### Lineage Policy [`data/lineage/`](data/lineage/)

| File | Version | Status | Purpose |
|---|---|---|---|
| [`data/lineage/LINEAGE_POLICY.md`](data/lineage/LINEAGE_POLICY.md) | 1.1.0 | Complete | Artifact lineage, BOM flow, OEE flow, quality flow |

### Canonical Example [`data/contracts/`](data/contracts/)

| File | Version | Status | Purpose |
|---|---|---|---|
| [`data/contracts/canonical_scenario.example.json`](data/contracts/canonical_scenario.example.json) | 1.0.0 | Complete | Full reference example of canonical scenario format |

---

## Key Decisions Finalized

1. **Run = UUID + append-only artifact directory** — every execution is immutable and traceable.
2. **Stage identity is permanent** — never deleted, renamed, or converted. SOP traceability preserved.
3. **Three-layer execution model** — Stage (SOP identity) / WorkUnitModel (class template) / WorkUnit (physical instance).
4. **`covered_stage_ids[]` is the only WorkUnitModel-to-Stage linkage** — always an array, minimum one element. No `stage_id` singular field.
5. **Integration = `covered_stage_ids.length > 1`** — structural condition, not a type. Orthogonal to automation level.
6. **Stage weights computed by Adapter, always pre-materialized** — engines never compute attribution.
7. **Canonical model is stable and unversioned** — engines consume canonical only, never public schema.
8. **BOM is embedded within each product object** — all three product types (intermediate_product, semi_product, finished_product) carry `bill_of_materials[]` with `quantity_required_per_output`. There is no separate top-level `bom[]` array.
9. **SemiProduct may reference other SemiProducts in BOM** — reflecting real cross-process dependencies.
10. **Multi-process structure at canonical top level** — `processes[]` array; each process has its own stages and WorkUnitModel associations.
11. **Domain execution data not in database** — process structure, product definitions, BOM, WorkUnitModel definitions, WorkUnit instances, stage weights, stage parameters, work_unit_parameters in JSON artifacts only.
12. **OEE fully decomposed into three components** — Availability (reliability on model), Performance (operating_rate on instance), Quality (defect_rate on instance overriding stage baseline).
13. **Defect rate at two levels** — `stage_parameters.defect_rate` is process-design baseline; `work_unit_parameters.defect_rate` is per-machine observed (from MES); work_unit value overrides stage baseline in simulation.
14. **Rework modeled at stage level** — `stage_parameters.rework`: available, rework_rate, maximum_rework_cycles. Rework units re-enter the stage and consume additional cycle time.
15. **WIP model per Stage** — each Stage (except the last in a process) carries a `wip_model` defining buffer capacity, initial WIP, and flow policy. Last stage has `wip_model: null`.
16. **`eligible_work_unit_model_ids[]` required on every Stage** — defines which models may serve this stage; validated by Adapter.
17. **`factory.footprint_limit_m2` is a top-level factory field** — both simulator and analytics consume it.
18. **Flow policy fields required for WIP estimation** — `batch_size`, `transfer_delay_sec`, `unit_buffer_area_m2` on every WorkUnitModel.
19. **Demand in `calendar.demand`** — no top-level `planning_period` field; all demand lives in `calendar.demand` with `planning_unit` and `periods[]`.
20. **WorkUnitModel does not reuse across processes** — a model is bound to a specific set of stages within one process.
21. **work_unit_parameters sourced from MES via Data Platform** — feature engineering extracts per-machine defect_rate and operating_rate from MES history; these flow into canonical via the public scenario submission.
22. **10 system-level failure modes are first-class domain concepts** — FM-01 through FM-10; Analytics v1 must detect all of them.
23. **A/B comparison is artifact-only** — never re-invokes engines; reads from stored artifacts of both runs.
24. **Repository paths** — `platform/`, `engines/`, `data_platform/`, `presentation/`, `data_storage/`.

---

## What This Index Does NOT Cover (Future Phases)

| Item | Phase |
|---|---|
| JSON Schema files | Phase 2 |
| Database migration scripts | Phase 3 |
| Adapter implementation (C#) | Phase 4 |
| ScenarioBuilder implementation (C#) | Phase 4 |
| C++ simulation engine | Phase 5 |
| Python analytics with failure mode detection | Phase 6 |
| React UI | Phase 7 |
