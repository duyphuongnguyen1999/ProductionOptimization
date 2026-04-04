# PIDSS — Phase 1: Domain & Canonical Model

> **Document Version:** 1.0.0  
> **Status:** Draft  
> **Date:** 2026-02-21

---

## Table of Contents

1. [Purpose](#1-purpose)
2. [Core Domain Concepts](#2-core-domain-concepts)
3. [The Stage Identity Rule](#3-the-stage-identity-rule)
4. [Equipment-Centric Execution Model](#4-equipment-centric-execution-model)
5. [Reference Assembly Process (7 Stages)](#5-reference-assembly-process-7-stages)
6. [Batch Flow Model](#6-batch-flow-model)
7. [Break Handling & Operator Presence](#7-break-handling--operator-presence)
8. [Canonical Scenario Structure](#8-canonical-scenario-structure)
9. [Run Artifact Structure](#9-run-artifact-structure)
10. [Design Rules Summary](#10-design-rules-summary)
11. [Phase 1 Deliverables](#11-phase-1-deliverables)

---

## 1. Purpose

This document defines the PIDSS Phase 1 deliverables: the stable internal **Domain Model** and the **Canonical Scenario** execution format.

It establishes the conceptual vocabulary and structural contracts that all downstream components must adhere to:

- **C++ Simulator** — reads `canonical_scenario.json`, produces simulation output
- **Python Analytics** — reads simulation output, produces KPIs and recommendations
- **Platform (.NET)** — owns validation, versioning, adapter, and orchestration

> The Canonical Scenario is the **single source of truth** consumed by engines.  
> It is **never** derived directly from a public-facing API schema version.  
> The Platform adapter layer handles all schema versioning and translation.

---

## 2. Core Domain Concepts

| Concept | Identity | Role in PIDSS | Key Rule |
|---|---|---|---|
| **Process** | SOP-defined transformation | Container for Stage sequence | Rarely changes |
| **Stage** | SOP step within a process | Unit of KPI, bottleneck & traceability | **NEVER deleted or replaced — immutable identity** |
| **Work Unit** | Physical execution resource | Capacity modeling unit | Equipment-centric; may cover 1..N stages |
| **Integrated Cell** | Automated multi-stage unit | Automation evaluation unit | References `covered_stage_ids[]`; preserves all stage identities |
| **Line** | Logical process replication | Demand & capacity framing | Resources NOT necessarily 1:1 with lines |
| **Scenario** | Hypothetical production config | What-if evaluation input | Versioned, includes `random_seed` |
| **Run** | One execution of a Scenario | Immutable artifact bundle | Append-only; identified by UUID |

---

## 3. The Stage Identity Rule

### 3.1 Why Stages Are Immutable

Stages represent SOP steps — they are the unit of **business traceability, A/B comparison, and bottleneck reporting**.

If a stage were deleted or renamed when automation is introduced, all historical comparison data would become invalid.

> **Critical Rule:**  
> A Stage identity MUST NEVER be deleted, merged, or replaced —  
> even when an automated cell covers multiple stages.

### 3.2 Integrated Cell — Correct Implementation

When an automated cell covers multiple stages (e.g., Pressing + Welding), the model uses an **IntegratedCell Work Unit** that references the existing stage IDs:

```json
"covered_stage_ids": ["stg-01-pressing", "stg-02-welding"]
```

The original stages remain in the `process.stages` definition **unchanged**.

For KPI and bottleneck reporting, cycle time is attributed back to individual stages using the `attribution.policy` field (e.g., `proportional_by_original_cycle_time`). This preserves comparability between baseline and automation scenarios.

#### ✅ Correct — Automation Scenario

```
process.stages:
  stg-01-pressing    ← PRESERVED
  stg-02-welding     ← PRESERVED
  stg-03-manual-assembly
  ...

integrated_cells:
  ic-pressing-welding-auto:
    covered_stage_ids: [stg-01-pressing, stg-02-welding]  ← references, not replaces
```

#### ❌ Wrong — Never Do This

```
process.stages:
  stg-auto-pressing-welding  ← NEW stage created — FORBIDDEN
  stg-03-manual-assembly
  ...
  // stg-01-pressing DELETED — FORBIDDEN
  // stg-02-welding  DELETED — FORBIDDEN
```

---

## 4. Equipment-Centric Execution Model

### 4.1 Work Unit Types

| unit_type | Description | Operator Presence | Stops on Break? | Example |
|---|---|---|---|---|
| `manual` | Fully human-operated bench | Required (≥1) | Yes | Assembly, Inspection |
| `semi_auto` | Machine + human coupled | Required (1) | Yes | Pressing, Welding |
| `full_auto` | Autonomous machine / integrated cell | None (0) | **No** (continues) | Auto Pressing-Welding Cell |

### 4.2 Work Unit Fields

Each Work Unit in the canonical model carries:

```
unit_id                    — unique identifier
name                       — human-readable label
unit_type                  — manual | semi_auto | full_auto
stage_id                   — references one stage (single-stage unit)
covered_stage_ids[]        — references multiple stages (integrated cell only)
count                      — number of physical units in the resource pool
cycle_time                 — distribution (fixed | uniform | normal | triangular)
  .mean_sec                — expected cycle time
  .min_sec / max_sec       — range (for uniform/triangular)
  .std_sec                 — standard deviation (for normal)
operators_per_unit         — headcount required per unit
requires_operator_presence — drives break impact logic
reliability                — optional MTBF / MTTR
  .mtbf_hours
  .mttr_minutes
```

### 4.3 Capacity at Stage Resource Pool Level

Lines are logical replications of the process. Resource pools (Work Units) are **NOT** constrained to be exactly one-per-line.

In the reference factory:

| Stage | Work Unit Count | Notes |
|---|---|---|
| Pressing | 7 machines | Matches line count |
| Welding | 7 machines | Matches line count |
| Manual Assembly | 10 benches | More than line count |
| Manual Connection | 10 benches | More than line count |
| Manual Coating | 10 benches | More than line count |
| Silicon Processing | 9 machines | Between line and manual count |
| Visual Inspection | 8 stations | More than line count |

The simulator models each stage as an **independent resource pool**, not a fixed line-to-station mapping.

---

## 5. Reference Assembly Process (7 Stages)

| # | Stage Name | `stage_id` | Type | Cycle Time | Operators/Unit |
|---|---|---|---|---|---|
| 1 | Pressing | `stg-01-pressing` | semi_auto | 6–7 sec | 1 |
| 2 | Welding | `stg-02-welding` | semi_auto | 7–8 sec | 1 |
| 3 | Manual Assembly | `stg-03-manual-assembly` | manual | ~12 sec | 2 per bench |
| 4 | Manual Connection | `stg-04-connection` | manual | 15–17 sec | 2 per bench |
| 5 | Manual Coating | `stg-05-coating` | manual | 15–17 sec | 2 per bench |
| 6 | Silicon Processing | `stg-06-silicon` | semi_auto | ~20 sec | 1 |
| 7 | Visual Inspection | `stg-07-inspection` | manual | ~12 sec | 1 |

> Stage IDs are **stable identifiers** used across all scenarios, runs, and KPI reports.  
> They must not be renamed between scenario versions.

---

## 6. Batch Flow Model

### 6.1 Aggregate Simulation — Not Discrete Event

PIDSS uses an **aggregate simulation model**. There is no per-product WIP routing, no discrete-event queue, and no lot-level tracking. The unit of simulation is a **batch**.

### 6.2 Batch Flow Policy

| Parameter | Baseline Value | Notes |
|---|---|---|
| `batch_size` | 600 pieces | Configurable per scenario |
| `transfer_trigger` | `full_batch_completion` | Batch transfers only after all pieces complete the current stage |
| `transfer_delay_sec` | 240 sec (4 min) | Checksheet + confirmation time |
| `wip_tracking` | `false` | No individual unit routing |

### 6.3 Automation Impact on Flow

When an IntegratedCell covers multiple stages:

- **Internal transfer is eliminated** (`internal_transfer_eliminated: true`) between covered stages
- The combined cycle time replaces the sum of individual stage times
- **Inter-stage transfer to the next non-integrated stage** still applies

```
Baseline:              [Pressing 6.5s] → [transfer 240s] → [Welding 7.5s] → [transfer 240s] → ...
Integrated Cell:       [Pressing+Welding combined 10s, no internal transfer] → [transfer 60s] → ...
```

---

## 7. Break Handling & Operator Presence

The `requires_operator_presence` flag on each Work Unit drives break impact:

| `unit_type` | `requires_operator_presence` | Stops During Break? |
|---|---|---|
| `manual` | `true` | ✅ Yes — always stops |
| `semi_auto` | `true` | ✅ Yes — always stops |
| `full_auto` (integrated cell) | `false` | ❌ No — continues running |

This is a key throughput advantage modeled for automation scenarios: full_auto cells accumulate production during break windows that manual/semi-auto lines cannot.

### 7.1 Break Behavior Config in Canonical

```json
"break_behavior": {
  "manual_stops_during_break":    true,
  "semi_auto_stops_during_break": true,
  "auto_continues_during_break":  true
}
```

---

## 8. Canonical Scenario Structure

### 8.1 Top-Level Fields

```
canonical_scenario.json
│
├── _meta                        Canonical version, run_id, generator info
├── scenario_id                  Unique scenario identifier
├── scenario_name                Human-readable name
├── scenario_description         Optional description
├── random_seed                  Integer seed for reproducibility
│
├── process                      Immutable stage list
│   └── stages[]
│       ├── stage_id             Stable unique identifier
│       ├── order                Sequence position
│       └── name                 Display name
│
├── work_units[]                 Equipment pools (single-stage units)
│   ├── unit_id
│   ├── unit_type                manual | semi_auto | full_auto
│   ├── stage_id                 References one stage
│   ├── count                    Resource pool size
│   ├── cycle_time               Distribution + parameters
│   ├── operators_per_unit
│   ├── requires_operator_presence
│   └── reliability              Optional MTBF/MTTR
│
├── integrated_cells[]           Automation units covering multiple stages
│   ├── unit_id
│   ├── covered_stage_ids[]      References existing stage_ids
│   ├── combined_cycle_time      Single cycle time for all covered stages
│   ├── internal_transfer_eliminated
│   ├── attribution              How to split KPIs back to individual stages
│   │   ├── policy               proportional_by_original_cycle_time | equal_split | manual_weights
│   │   └── stage_weights[]      { stage_id, weight } — must sum to 1.0
│   └── investment               Optional CAPEX/OPEX for ROI calculation
│
├── flow_policy                  Batch size, transfer delay, trigger mode
├── planning_horizon             Materialized start/end timestamps, target qty
├── planning_calendar            Shifts, breaks, working days, break behavior
├── staffing_pool                Total operators and allocation policy
├── quality_policy               Per-stage defect rates and rework policy
└── cost_model                   Labor rates, overhead, CAPEX investment items
```

### 8.2 Design Invariants

The Canonical Scenario must satisfy these invariants at all times:

1. **All timestamps are materialized** — no relative offsets; `start_time` and `end_time` are ISO 8601 datetime strings
2. **No schema branching** — no `oneOf`, `anyOf`, no `schema_version` disambiguation inside the canonical format
3. **No legacy fields** — the adapter absorbs all public-to-canonical translation
4. **Referential integrity** — every `stage_id` referenced in `work_units` or `integrated_cells.covered_stage_ids` must exist in `process.stages`
5. **Attribution weights sum to 1.0** — per each integrated cell
6. **`random_seed` is always present** — guarantees reproducibility

### 8.3 Cycle Time Distribution Types

| `distribution` | Required Fields | Use Case |
|---|---|---|
| `fixed` | `mean_sec` | Fully deterministic (automated machines) |
| `uniform` | `min_sec`, `max_sec`, `mean_sec` | Known range, no preference (manual tasks) |
| `normal` | `mean_sec`, `std_sec` | Normally distributed variability |
| `triangular` | `min_sec`, `max_sec`, `mean_sec` | Skewed, bounded variability |

---

## 9. Run Artifact Structure

Each Run produces an **immutable, append-only** artifact bundle stored under:

```
artifacts/{run_id}/
```

| File | Producer | Description |
|---|---|---|
| `scenario_snapshot.json` | Platform (.NET) | Immutable copy of public input payload |
| `canonical_scenario.json` | Platform (.NET) | Internal execution model (this spec) |
| `simulation_result.json` | C++ Simulator | Aggregate throughput & timing output |
| `production_records.csv` | C++ Simulator | Per-batch/stage production log |
| `analysis_response.json` | Python Analytics | KPI aggregates, bottleneck ranking |
| `recommendation.json` | Python Analytics | Explainable ROI, payback, strategy advice |
| `logs/` | Platform + Engines | Structured execution logs |

> Artifacts are **never overwritten**. Each run has its own directory identified by `run_id` (UUID).

---

## 10. Design Rules Summary

| Rule | Rationale |
|---|---|
| **Stage identity is immutable** | Ensures A/B comparison validity and SOP traceability across all scenarios |
| **Execution is equipment-centric** | Work units (not stages) define capacity; one stage may have multiple unit types simultaneously |
| **Integrated cell preserves all stage IDs** | `covered_stage_ids[]` links to existing `stage_id`s; no new stages created, none deleted |
| **Canonical format is stable** | Engines never parse public schema versions; the adapter handles all translation |
| **Aggregate simulation only** | Batch-level flow; no discrete-event WIP tracking per product |
| **Break stops operator-dependent units** | `requires_operator_presence` drives break impact; `full_auto` units continue |
| **Random seed is mandatory** | Guarantees reproducibility of every run; stored in canonical and snapshot |
| **Adapter owns versioning** | Public schema may evolve; canonical must not; backward compatibility lives in the adapter layer |
| **Artifacts are append-only** | No run result is ever overwritten; full audit trail is always preserved |
| **Engines consume canonical only** | C++ and Python never branch on `schema_version`; they receive a single, stable format |

---

## 11. Phase 1 Deliverables

| File | Location | Description |
|---|---|---|
| `canonical_scenario.example.json` | `data/contracts/` | Baseline scenario — 7 lines, manual + semi-auto |
| `canonical_scenario_automation.example.json` | `data/contracts/` | Automation scenario — integrated Pressing+Welding cell |
| `canonical_scenario.schema.json` | `data/schemas/` | JSON Schema Draft-07 for canonical model validation |
| `phase1-domain-model.docx` | `docs/` | Full domain model document (Word format) |
| `PHASE1_DOMAIN.md` | `docs/` | This document |

### What Comes Next — Phase 2

Phase 2 defines the **public-facing JSON contracts** (the schemas that external callers and the UI submit to the Platform API):

- `scenario.schema.json` — public scenario input (versioned, `schema_version` field)
- `simulation_result.schema.json` — public simulation output
- `analysis_response.schema.json` — public KPI/analytics output
- `recommendation.schema.json` — public recommendation output

The Platform adapter will translate public `scenario.schema.json → canonical_scenario.json`.  
Engines will remain unchanged.

---

*PIDSS — Production Intelligence & Decision Support System*  
*MIT License*
