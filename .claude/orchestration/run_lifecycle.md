# RUN LIFECYCLE

## 1. Overview

PIDSS execution is modeled as a **run-based system**.

A run represents a full execution of:

- scenario construction
- canonicalization
- simulation
- analytics

Each run is:

- uniquely identified by `run_id` (UUID v4)
- immutable
- append-only (artifacts never overwritten)

---

## 2. Run Status Lifecycle

A run transitions through the following states:

```
Created → Queued → Running → Completed / Failed
```

### State Definitions

| State | Description |
|------|--------|
| Created | Run metadata initialized |
| Queued | Waiting for execution slot |
| Running | Execution pipeline in progress |
| Completed | All jobs finished successfully |
| Failed | Execution terminated due to error |

---

## 3. Job Status Lifecycle

Each run consists of multiple jobs.

Job lifecycle:
```
Pending → Running → Completed / Failed
```


### Job Types

- Simulation
- Analytics

---

## 4. State Transition Rules

- Run cannot move to `Running` unless:
  - all required jobs are `Pending`
- Analytics job cannot start unless:
  - Simulation job is `Completed`
- Any critical failure:
  - Run → `Failed`

---

## 5. Immutability Guarantees

- Run metadata is append-only
- Artifacts are immutable
- Re-running requires a new `run_id`

---

## 6. Reproducibility Requirement

A run is reproducible if:

- scenario_snapshot.json exists
- canonical_scenario.json exists
- deterministic seed is present
- all artifacts are preserved
