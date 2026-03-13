# PIDSS Versioning Policy

**Version:** 1.0.0  
**Phase:** 0 — Repository Foundation & Data-Layer Conventions  
**Status:** Active

---

## 1. Overview

PIDSS maintains two separate models with different versioning strategies:

| Model | Purpose | Versioned? | Owner |
|---|---|---|---|
| **Public Schema** | External API contract | Yes — `schema_version` field | Platform (adapter layer) |
| **Canonical Model** | Internal engine execution model | No — always current | Platform + Engines |

The key architectural rule:

> **Public schemas evolve. Canonical model stays stable.**  
> The Platform adapter layer is responsible for all version translation.

---

## 2. Public Schema Versioning

### Version Identifier

Every public scenario payload must include a `schema_version` field:

```json
{
  "schema_version": "1.0",
  ...
}
```

### Version Format

```
MAJOR.MINOR

MAJOR: Breaking changes (fields removed, types changed, restructured)
MINOR: Backward-compatible additions (new optional fields, new enum values)
```

### Schema File Naming

```
data/schemas/scenario.v1.schema.json
data/schemas/scenario.v2.schema.json
data/contracts/scenario.v1.example.json
data/contracts/scenario.v2.example.json
```

### Compatibility Rules

| Change Type | Version Impact | Adapter Required? |
|---|---|---|
| Add optional field | MINOR bump | Yes — default value handling |
| Add new enum value | MINOR bump | Yes — map to canonical |
| Remove field | MAJOR bump | Yes — migration logic |
| Rename field | MAJOR bump | Yes — field mapping |
| Change field type | MAJOR bump | Yes — type coercion |
| Change required → optional | MINOR bump | Yes — default value |

### Support Window

- Current MAJOR version: **fully supported**
- Previous MAJOR version: **supported until deprecation notice (minimum 2 releases)**
- Older MAJOR versions: **rejected with HTTP 400 and clear error message**

### Deprecation Notice Format

Deprecated versions are signaled via response headers:

```
X-PIDSS-Schema-Deprecated: true
X-PIDSS-Schema-Deprecated-Version: 1.0
X-PIDSS-Schema-Successor-Version: 2.0
X-PIDSS-Schema-Sunset-Date: 2026-01-01
```

---

## 3. Canonical Model Versioning

### Rule

> The canonical model does **not** carry a version field.

The canonical model is the **stable internal contract** between the Platform adapter and the engines (C++ and Python).

### When the Canonical Model Changes

Canonical model changes are rare and **breaking** by definition. If a change is required:

1. The change must be designed to be backward-compatible where possible.
2. All engines must be updated simultaneously.
3. The change is communicated via internal architecture decision record (ADR) under `docs/`.
4. The change does **not** affect public schema versioning.

### Canonical Model Stability Guarantee

- Engines must **never** check or branch on a version field in the canonical model.
- The canonical model schema is documented in `data/documentation/CANONICAL_MODEL.md`.
- Changes to the canonical model require a PR review from the Platform architect.

---

## 4. Adapter Responsibility

The Platform (`platform_dotnet/Pidss.Platform.Api`) is the **only** component that:

- Reads and validates the `schema_version` field.
- Selects the appropriate JSON schema for validation.
- Transforms the public payload to canonical format.
- Handles default values, field renames, and type coercions.
- Sets engine-facing fields that have no direct public equivalent.

```
Public Payload (v1.0) ──► Platform Adapter ──► Canonical Scenario
Public Payload (v2.0) ──► Platform Adapter ──► Canonical Scenario (same structure)
```

### Adapter Location

```
platform_dotnet/Pidss.Platform.Api/
└─ Adapters/
   ├─ ScenarioAdapterV1.cs
   ├─ ScenarioAdapterV2.cs    (when v2 is introduced)
   └─ IScenarioAdapter.cs
```

---

## 5. Output Schema Versioning

Output schemas (`simulation_result`, `analysis_response`, `recommendation`) follow the same versioning strategy as input schemas.

Output payloads include a `schema_version` field to identify the structure:

```json
{
  "schema_version": "1.0",
  "run_id": "...",
  ...
}
```

Clients consuming outputs must handle the `schema_version` field.

---

## 6. Version Registry

A version registry is maintained in `data/documentation/VERSION_REGISTRY.md` to track:

- All published schema versions
- Their status (Active / Deprecated / Sunset)
- Their sunset dates
- The adapter class that handles each version

| Schema | Version | Status | Adapter | Sunset Date |
|---|---|---|---|---|
| `scenario` | `1.0` | Active | `ScenarioAdapterV1` | — |
| `simulation_result` | `1.0` | Active | N/A (output only) | — |
| `analysis_response` | `1.0` | Active | N/A (output only) | — |
| `recommendation` | `1.0` | Active | N/A (output only) | — |
