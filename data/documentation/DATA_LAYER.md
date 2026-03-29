# PIDSS Data Layer Documentation

## 1. Overview

The `data/` directory is the central **governance layer** for all data contracts, schemas, validation logic, transformation scripts, lineage policies, and documentation.

It is **source-controlled** and **versioned**.

No data governance logic lives in the application code — all definitions originate here.

> **`data/` defines HOW data should look.**
> **`data_storage/` contains ACTUAL runtime data artifacts produced by the Data Platform.**

---

## 2. Directory Structure

```
data/
├── contracts/          # Versioned example JSON payloads (human-readable references)
├── schemas/            # JSON Schema files (machine-readable validation)
├── validation/         # Validation logic and test scripts
├── transforms/         # Analytical transform definitions
├── lineage/            # Artifact and run metadata policies
└── documentation/      # Versioning, domain model, and governance docs
```

---

## 3. Folder Descriptions

### `contracts/`

Contains versioned example JSON payloads for all public contracts.

Purpose:
- Developer reference
- Integration testing baseline
- Onboarding documentation

Naming: `{entity}.v{N}.example.json`

Examples:
- `scenario.v1.example.json`
- `simulation_result.v1.example.json`
- `analysis_response.v1.example.json`
- `recommendation.v1.example.json`

> The version segment is required in contract file names. Example payloads must match their corresponding versioned schema.

---

### `schemas/`

Contains JSON Schema Draft-07 files for all public contracts.

Purpose:
- Machine-readable contract validation
- Used by Platform Adapter for payload validation
- Versioned alongside contract changes

Naming: `{entity}.v{N}.schema.json`

Examples:
- `scenario.v1.schema.json`
- `simulation_result.v1.schema.json`
- `analysis_response.v1.schema.json`
- `recommendation.v1.schema.json`

Rules:
- All schemas must include `additionalProperties: false`
- All required fields must be declared in `required[]`
- Enum fields must include exhaustive `enum` arrays

---

### `validation/`

Contains validation scripts and test data.

Purpose:
- Verify that example payloads pass their schema
- Regression-test schema changes
- CI/CD integration support

Contents:
- Validation scripts (Python or PowerShell)
- Valid example payloads
- Invalid example payloads (negative tests)

---

### `transforms/`

Contains analytical transform definitions.

Purpose:
- Define how raw simulation outputs are aggregated into KPIs
- Define footprint calculation formulas
- Define WIP estimation formulas

Contents:
- Transform specification documents
- Reference implementations (Python scripts)

---

### `lineage/`

Contains policies governing artifact and run metadata lineage.

Purpose:
- Define how runs, artifacts, and decisions are traceable
- Define append-only and immutability rules
- Define manifest structure

Contents:
- `LINEAGE_POLICY.md`

---

### `documentation/`

Contains domain model explanations and governance documentation.

Purpose:
- Explain core domain concepts for all team members
- Record architectural decisions
- Provide versioning reference

Contents:
- `DATA_DICTIONARY.md` — all domain entities, KPIs, failure modes
- `CANONICAL_MODEL.md` — 15 design principles for the canonical model
- `VERSION_REGISTRY.md` — all public schema versions and lifecycle status
- `DATA_LAYER.md` — this file

---

## 4. Relationship to `data_storage/`

`data/` and `data_storage/` serve distinct, non-overlapping purposes:

| Aspect | `data/` | `data_storage/` |
|---|---|---|
| Purpose | Governance — defines contracts and policies | Runtime — stores actual data artifacts |
| Contents | Schemas, contracts, docs, lineage policies | Feature store, calibration profiles, model store |
| Produced by | Engineering (committed to repo) | Data Platform pipeline (runtime output) |
| Versioned in git | Yes | No (gitignored) |
| Read by | Platform Adapter (for validation) | Platform via DataSources layer |

The Platform's `DataSources` layer (`IFeatureStoreReader`, `ICalibrationProfileProvider`) is the only runtime component permitted to read from `data_storage/`. No engine reads directly from `data_storage/`.

---

## 5. No Adapter Logic in Data Layer

> **Adapter logic belongs exclusively to `platform/`.**

The `data/` layer defines contracts and schemas.
The `platform/` layer implements adaptation from public schema to canonical model.

No transformation code lives inside `data/`.

---

## 6. Schema Lifecycle

1. Schemas are created alongside public contract changes.
2. Minor version bumps add new optional fields only.
3. Major version bumps create a new schema file (e.g. `scenario.v2.schema.json`).
4. Old schema files are never deleted — they remain for backward compatibility support.
5. Platform Adapter supports N and N-1 major versions.
6. All version lifecycle entries must be recorded in `data/documentation/VERSION_REGISTRY.md`.
