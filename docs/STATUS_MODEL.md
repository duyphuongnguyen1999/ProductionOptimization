# PIDSS Status Model

<p align="right">
  🇺🇸 <a href="STATUS_MODEL.md">English</a>
  | 🇻🇳 <a href="STATUS_MODEL_VI.md">Tiếng Việt</a>
</p>

**Version:** 1.0.0  
**Phase:** 0 — Repository Foundation & Data-Layer Conventions  
**Status:** Active

---

## 1. Overview

PIDSS uses a **run-based execution model**. Every scenario execution is tracked through two entities:

- **Run** — the top-level execution unit representing one scenario evaluation.
- **Job** — a sub-unit within a run representing one engine invocation (Simulation or Analytics).

---

## 2. Run Status Lifecycle

### States

| Status | Description |
|---|---|
| `Created` | Run record has been created. Snapshot has been written. Validation has not started. |
| `Validating` | Platform is validating the public scenario against the JSON schema. |
| `Queued` | Validation passed. Canonical scenario generated. Waiting for a concurrency slot. |
| `Running` | At least one job is actively executing. |
| `Completed` | All jobs finished successfully. Post-processing and artifact indexing done. |
| `Failed` | One or more jobs failed, or validation failed. No retry is automatic. |
| `Cancelled` | Run was cancelled by a user or operator before completion. (Future: Phase 4+) |

### Transition Diagram

```
Created
  │
  ▼
Validating ──── (validation fails) ──► Failed
  │
  ▼
Queued
  │
  ▼
Running ─────── (job fails) ──────────► Failed
  │
  ▼
Completed
```

### Rules

- A run may only move **forward** in the lifecycle. No backward transitions.
- A `Failed` or `Completed` run is **terminal** — no status updates are allowed.
- A run transitions to `Failed` if **any** job transitions to `Failed`.
- A run transitions to `Completed` only when **all** jobs are `Completed`.
- `Cancelled` is reserved for future implementation and may be treated as `Failed` in early phases.

---

## 3. Job Status Lifecycle

### Job Types

| Job Type | Engine | Input | Outputs |
|---|---|---|---|
| `Simulation` | C++ CLI | `canonical_scenario.json` | `simulation_result.json`, `production_records.csv` |
| `Analytics` | Python CLI | `canonical_scenario.json`, `simulation_result.json`, `production_records.csv` | `analysis_response.json`, `recommendation.json` |

### States

| Status | Description |
|---|---|
| `Pending` | Job has been created but is waiting for its prerequisite job to complete. |
| `Queued` | Job's prerequisites are met. Waiting for executor to pick it up. |
| `Running` | Engine process has been started. |
| `Completed` | Engine exited with code 0. Output artifacts are present and valid. |
| `Failed` | Engine exited with non-zero code, timed out, or output validation failed. |

### Transition Diagram

```
Pending ──► Queued ──► Running ──► Completed
                          │
                          └──────────────► Failed
```

### Job Execution Order

Jobs within a run are executed **sequentially** in this fixed order:

1. `Simulation` (C++)
2. `Analytics` (Python) — depends on `Simulation` being `Completed`

Analytics job moves from `Pending` → `Queued` only after Simulation is `Completed`.  
If Simulation fails, Analytics remains `Pending` and the run transitions to `Failed`.

---

## 4. Status Field Conventions

All status fields use **string enum values** (not integers) for readability in logs and artifacts.

### Run Status in Database

```sql
-- Allowed values for runs.status
CHECK (status IN ('Created', 'Validating', 'Queued', 'Running', 'Completed', 'Failed', 'Cancelled'))
```

### Job Status in Database

```sql
-- Allowed values for jobs.status
CHECK (status IN ('Pending', 'Queued', 'Running', 'Completed', 'Failed'))
```

---

## 5. Timestamps

Every status transition must record a timestamp. The following timestamp fields are required:

### Run Timestamps

| Field | Set When |
|---|---|
| `created_at` | Run record created |
| `queued_at` | Run transitions to `Queued` |
| `started_at` | Run transitions to `Running` |
| `completed_at` | Run transitions to `Completed` or `Failed` |

### Job Timestamps

| Field | Set When |
|---|---|
| `created_at` | Job record created |
| `started_at` | Job transitions to `Running` |
| `completed_at` | Job transitions to `Completed` or `Failed` |

All timestamps are stored in **UTC**, formatted as ISO 8601 (`2025-01-15T08:30:00Z`).

---

## 6. Error Handling Convention

When a run or job fails:

- The `error_message` field is populated with a human-readable summary.
- The `error_detail` field (optional) may contain structured diagnostic data (JSON string).
- Logs are always preserved under `artifacts/{run_id}/logs/` regardless of outcome.
- Partial artifacts (if any) remain on disk under the run directory.

A failed run produces:
- `scenario_snapshot.json` — always present (written at creation)
- `canonical_scenario.json` — present only if validation succeeded
- Engine outputs — present only for jobs that started before failure
- All log files that were written up to the point of failure
