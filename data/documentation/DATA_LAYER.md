# PIDSS Data Layer Documentation

## 1. Overview

The `data/` directory is the central governance layer for all data contracts, schemas, validation logic, transformation scripts, lineage policies, and documentation.

It is **source-controlled** and **versioned**.

No data governance logic lives in the application code — all definitions originate here.

---

## 2. Directory Structure

```
data/
├── contracts/          # Example JSON payloads (human-readable references)
├── schemas/            # JSON Schema files (machine-readable validation)
├── validation/         # Validation logic and test scripts
├── transforms/         # Analytical transform definitions
├── lineage/            # Artifact and run metadata policies
└── documentation/      # Versioning, domain model, and governance docs
```

---

## 3. Folder Descriptions

### `contracts/`

Contains example JSON payloads for all public contracts.

Purpose:
- Developer reference
- Integration testing baseline
- Onboarding documentation

Naming: `{entity}.example.json`

Examples:
- `scenario.example.json`
- `simulation_result.example.json`
- `analysis_response.example.json`
- `recommendation.example.json`

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
- `LINEAGE_POLICY.md` / `LINEAGE_POLICY_VI.md`

---

### `documentation/`

Contains domain model explanations and governance documentation.

Purpose:
- Explain core domain concepts for all team members
- Record architectural decisions
- Provide versioning reference

Contents:
- Domain model overview
- Stage-vs-WorkUnit explanation
- Integration modeling guide
- Versioning policy reference

---

## 4. No Adapter Logic in Data Layer

> **Adapter logic belongs exclusively to `platform_dotnet/`.**

The `data/` layer defines contracts and schemas.
The `platform_dotnet/` layer implements adaptation from public schema to canonical model.

No transformation code lives inside `data/`.

---

## 5. Schema Lifecycle

1. Schemas are created alongside public contract changes.
2. Minor version bumps add new optional fields only.
3. Major version bumps create a new schema file (e.g. `scenario.v2.schema.json`).
4. Old schema files are never deleted — they remain for backward compatibility support.
5. Platform Adapter supports N and N-1 major versions.
