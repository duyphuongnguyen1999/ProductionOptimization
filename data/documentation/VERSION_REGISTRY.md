# PIDSS Version Registry

**Version:** 1.0.0  
**Phase:** 0 — Repository Foundation & Data-Layer Conventions  
**Status:** Active

---

## Purpose

This registry tracks the lifecycle of all public schema versions in PIDSS.

All schema version entries must be registered here when:
- A new schema version is introduced
- A version is marked deprecated
- A version is sunset (no longer accepted)

---

## Input Schemas

### `scenario`

| Version | Status | Introduced | Deprecated | Sunset | Adapter Class |
|---|---|---|---|---|---|
| `1.0` | Active | Phase 2 | — | — | `ScenarioAdapterV1` |

---

## Output Schemas

### `simulation_result`

| Version | Status | Introduced | Notes |
|---|---|---|---|
| `1.0` | Active | Phase 5 | Written by C++ Simulator |

### `production_records` (CSV)

| Version | Status | Introduced | Notes |
|---|---|---|---|
| `1.0` | Active | Phase 5 | Written by C++ Simulator |

### `analysis_response`

| Version | Status | Introduced | Notes |
|---|---|---|---|
| `1.0` | Active | Phase 6 | Written by Python Analytics |

### `recommendation`

| Version | Status | Introduced | Notes |
|---|---|---|---|
| `1.0` | Active | Phase 6 | Written by Python Analytics |

---

## Status Definitions

| Status | Meaning |
|---|---|
| `Active` | Currently accepted and supported. |
| `Deprecated` | Still accepted but will be sunset. Clients should migrate. |
| `Sunset` | No longer accepted. Platform returns HTTP 400. |

---

## Notes

- Canonical model is NOT versioned and is NOT listed in this registry.
- Internal DTO versions are managed via C# and Python code, not this registry.
- This file must be updated in the same PR as any schema file change.
