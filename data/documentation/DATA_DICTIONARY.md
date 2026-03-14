# PIDSS Data Dictionary

**Version:** 1.0.0  
**Phase:** 0 — Repository Foundation & Data-Layer Conventions  
**Status:** Active

---

## 1. Purpose

This document defines the canonical terminology for all domain entities, fields, and concepts used across PIDSS. All teams must use these definitions consistently in code, documentation, schemas, and database design.

---

## 2. Core Domain Entities

### 2.1 Factory

| Attribute | Value |
|---|---|
| **Definition** | The physical manufacturing facility being modeled. Contains one or more Processes. Has a hard floor space constraint. |
| **Scope** | Top-level container. A PIDSS scenario always models one factory. |
| **Key field** | `factory_footprint_limit_m2` — the hard physical floor space limit. Analytics uses this to detect Footprint Constraint Violation. |
| **Rules** | A factory may contain multiple parallel or sequential processes. The total production footprint computed by simulation must not exceed this limit in a valid automation scenario. |

---

### 2.2 Process

| Attribute | Value |
|---|---|
| **Definition** | A defined manufacturing workflow that transforms raw materials into a Component or a final Product. A factory may contain multiple processes. |
| **Identity** | Defined by an SOP. Stable over long periods. |
| **Output** | Either a **Component** (semi-finished, consumed by another process) or a **Product** (final, shipped to customer). |
| **Scope** | Contains one or more Stages in a fixed sequence. Contains one or more WorkUnits as execution resources. |
| **Canonical field** | `process_id` (string, slug) |
| **Example** | `"assembly_a"`, `"silicon_processing"`, `"packaging"` |
| **Rules** | Multiple processes may run in parallel. Final capacity is constrained by BOM availability across all upstream processes. |

---

### 2.3 Component

| Attribute | Value |
|---|---|
| **Definition** | The output of a Process that produces semi-finished goods. Consumed by a downstream Process via the BOM. |
| **Modeling** | Aggregate only. No WIP tracking per unit. Quantity is a count per planning period. |
| **Canonical field** | `component_id` (string, slug) |
| **Example** | `"pressed_housing"`, `"welded_frame"` |
| **Rules** | Linked to exactly one producing Process. May be consumed by one or more Products via BOM. Not tracked per serial/lot. |

---

### 2.4 Product (Final Product)

| Attribute | Value |
|---|---|
| **Definition** | The final manufactured output, assembled or packaged from one or more Components. |
| **Modeling** | Aggregate only. Final production quantity is BOM-constrained. |
| **Canonical field** | `product_id` (string, slug) |
| **Rules** | Final output = `min over all BOM entries of (component_qty / qty_required_per_product)`. Requires a BOM definition. |

---

### 2.5 BOM (Bill of Materials)

| Attribute | Value |
|---|---|
| **Definition** | Defines the Components required to produce one unit of a Product, with their required quantities. |
| **Canonical fields** | `product_id`, `component_id`, `quantity_required_per_product` |
| **Rules** | Stored in canonical scenario only — not in the relational database. Analytics uses BOM to identify the binding upstream bottleneck across processes. |

---

### 2.6 Stage

| Attribute | Value |
|---|---|
| **Definition** | A single, stable SOP step within a Process. The unit of business traceability and comparability. |
| **Identity** | **Immutable.** Never deleted, renamed, split, or merged. Automation does not alter Stage identity. |
| **Content** | `stage_id`, `order`, `name` only. No execution logic. |
| **Canonical field** | `stage_id` (string, slug) |
| **Rules** | Stages are the stable anchor for A/B comparison and bottleneck reporting across all scenarios. |

---

### 2.7 WorkUnit (Execution Unit)

| Attribute | Value |
|---|---|
| **Definition** | A physical or logical unit of execution capacity assigned to one or more Stages. Defines all execution parameters. |
| **Automation levels** | `manual`, `semi_auto`, `auto` |
| **Canonical field** | `unit_id` (string, slug) |
| **Key fields** | `covered_stage_ids[]`, `unit_type`, `count`, `cycle_time`, `operators_per_unit`, `requires_operator_presence`, `reliability` (optional), `footprint_m2` (optional), `financial` (optional) |
| **Rules** | `covered_stage_ids[]` is **always an array** (minimum one element). There is no `stage_id` singular field. Integration is defined by `covered_stage_ids.length > 1`. |

> **Critical:** `stage_id` singular field does NOT exist on WorkUnit. Only `covered_stage_ids[]`.

---

### 2.8 Integration (Multi-Stage Coverage)

| Attribute | Value |
|---|---|
| **Definition** | The condition in which a single WorkUnit covers two or more consecutive Stages. Defined structurally — not a type. |
| **Condition** | `covered_stage_ids.length > 1` |
| **Canonical requirement** | When integrated, an `integration` object with `stage_weights` **must** be present in the canonical WorkUnit. |
| **Rules** | Integration is orthogonal to automation level. Any `unit_type` may be integrated. Covered Stages retain their SOP identity. |

---

### 2.9 Stage Weights

| Attribute | Value |
|---|---|
| **Definition** | Normalized attribution map distributing a WorkUnit's execution contribution across its covered Stages. |
| **Canonical field** | `stage_weights` — map of `{ stage_id: float }`, summing to exactly 1.0 |
| **Computed by** | **Platform Adapter only.** Always materialized in `canonical_scenario.json` before engines receive it. |
| **Rules** | Required when `covered_stage_ids.length > 1`. Used by simulator (per-stage output records) and analytics (per-stage KPI attribution, bottleneck ranking). |

---

### 2.10 Line

| Attribute | Value |
|---|---|
| **Definition** | A logical replication of the full Process, representing one parallel production flow. |
| **Canonical field** | `line_id` (string, slug) |
| **Rules** | Resources are not necessarily 1:1 with lines. Capacity is modeled at stage resource pool level. |

---

### 2.11 Scenario

| Attribute | Value |
|---|---|
| **Definition** | A complete description of a hypothetical or baseline production configuration for evaluation. |
| **Public field** | `schema_version` |
| **Canonical** | No `schema_version`. Always current format. Contains materialized stage weights, BOM, multi-process structure, factory footprint limit, flow policy fields, and random seed. |

---

### 2.12 Run

| Attribute | Value |
|---|---|
| **Definition** | A single execution of a Scenario through the full PIDSS pipeline. |
| **Canonical field** | `run_id` (UUID v4) |
| **Lifecycle** | Created → Validating → Queued → Running → Completed / Failed |

---

### 2.13 Job

| Attribute | Value |
|---|---|
| **Definition** | A sub-unit of a Run representing one engine invocation. |
| **Types** | `Simulation` (C++ CLI), `Analytics` (Python CLI) |
| **Lifecycle** | Pending → Queued → Running → Completed / Failed |
| **Rules** | Simulation executes first. Analytics depends on Simulation completing successfully. |

---

## 3. Flow Model Concepts

### 3.1 WIP (Work In Progress)

| Attribute | Value |
|---|---|
| **Definition** | The aggregate count of units waiting between two consecutive Stages at any point in time. Modeled at stage boundaries — not per unit. |
| **Unit** | Integer (pieces) |
| **Estimation formula** | `WIP_stage ≈ Throughput × Effective_Wait_Time` |
| **Fields** | `wip_per_stage` (map of stage_id → float), `total_wip` (sum across all stages) |
| **Rules** | WIP is an aggregate metric. No per-product or per-lot tracking. Elevated WIP indicates flow imbalance, blocking, or batch gating. |

---

### 3.2 Lead Time

| Attribute | Value |
|---|---|
| **Definition** | The estimated elapsed time for a unit to flow through the full process, from Stage 1 entry to final Stage completion. |
| **Estimation method** | Little's Law: `Lead Time ≈ Total WIP / Throughput` |
| **Unit** | Hours or seconds (normalized in canonical outputs) |
| **Field** | `lead_time_estimate` in `simulation_result.json` |
| **Rules** | Lead time is an aggregate estimate, not a per-unit measurement. As WIP increases (due to blocking, batch gating, or flow imbalance), lead time increases proportionally at constant throughput. |

---

### 3.3 Little's Law

| Attribute | Value |
|---|---|
| **Definition** | A fundamental flow relationship: `Lead Time = WIP / Throughput`. Applies to any stable flow system. |
| **Usage in PIDSS** | Used by Analytics to estimate lead time from simulation WIP and throughput outputs. Used to detect WIP Explosion (lead time growing disproportionately). |
| **Formula** | `Lead_Time ≈ Total_WIP / Throughput` |

---

### 3.4 Blocking

| Attribute | Value |
|---|---|
| **Definition** | The condition in which a Stage or WorkUnit cannot transfer its output to the next Stage because the downstream buffer or stage is full or unavailable. The upstream unit is forced to stop or wait despite being capable of producing. |
| **Measured as** | `blocking_time` — cumulative time a WorkUnit spends blocked per simulation period |
| **Field** | `blocking_time` (per stage, in simulation_result.json) |
| **Causes** | Downstream capacity insufficient; batch size mismatch; downstream machine downtime |

---

### 3.5 Starvation

| Attribute | Value |
|---|---|
| **Definition** | The condition in which a Stage or WorkUnit is idle because it is not receiving sufficient input from the upstream Stage. The unit is capable of producing but has nothing to process. |
| **Measured as** | `starvation_time` — cumulative time a WorkUnit spends starved per simulation period |
| **Field** | `starvation_time` (per stage, in simulation_result.json) |
| **Causes** | Upstream capacity insufficient; upstream machine downtime; upstream batch gating delay |

---

### 3.6 Effective Wait Time

| Attribute | Value |
|---|---|
| **Definition** | The total time a batch of units spends waiting at a stage boundary before being transferred. Includes batch gating delay, transfer delay, and downstream congestion time. |
| **Formula** | `effective_wait_time = batch_gating_delay + transfer_delay + downstream_congestion_time` |
| **Usage** | Used in WIP estimation: `WIP_stage ≈ Throughput × Effective_Wait_Time` |

---

### 3.7 Batch Gating

| Attribute | Value |
|---|---|
| **Definition** | The policy by which units are held at a stage until a full batch is accumulated before transfer to the next stage. Produces cyclic WIP accumulation. |
| **Baseline** | Batch size of 600 pieces; transfer only after full batch completion |
| **WIP implication** | Average WIP at a batch-gated stage boundary ≈ `batch_size / 2` in steady state |

---

## 4. Production Footprint Model

### 4.1 Machine Area

| Attribute | Value |
|---|---|
| **Definition** | The total floor area occupied by all WorkUnit machines across the production floor. |
| **Formula** | `Machine_Area = Σ (unit.count × unit.footprint_m2)` for all WorkUnits in the process |
| **Unit** | m² |
| **Typical share** | 60–80% of total production floor area |
| **Field** | `machine_area_m2` in simulation_result.json |

---

### 4.2 WIP Buffer Area

| Attribute | Value |
|---|---|
| **Definition** | The total floor area consumed by WIP buffers between stages. |
| **Formula** | `WIP_Area = Σ (WIP_stage × unit_buffer_area)` across all stage boundaries |
| **Unit** | m² |
| **Typical share** | 10–30% of production floor area (higher with large batches or flow imbalance) |
| **Field** | `wip_area_m2` in simulation_result.json |
| **Key input** | `unit_buffer_area` — floor area required to store one unit of WIP at a stage boundary (canonical field, per stage or process level) |

---

### 4.3 Layout Factor

| Attribute | Value |
|---|---|
| **Definition** | A multiplier applied to the sum of machine area and WIP buffer area to account for aisles, operator movement corridors, and maintenance access space. |
| **Range** | 1.2 – 1.4 |
| **Canonical field** | `layout_factor` (float, factory level) |
| **Default** | 1.3 if not specified |
| **Formula role** | `Production_Footprint = (Machine_Area + WIP_Area) × Layout_Factor` |

---

### 4.4 Production Footprint

| Attribute | Value |
|---|---|
| **Definition** | The total estimated floor area required by the production configuration, including machines, WIP buffers, and layout overhead. |
| **Formula** | `Production_Footprint = (Machine_Area + WIP_Area) × Layout_Factor` |
| **Unit** | m² |
| **Field** | `production_footprint_m2` in simulation_result.json |
| **Constraint check** | Analytics compares `production_footprint_m2` against `factory_footprint_limit_m2` to detect Footprint Constraint Violation |

---

### 4.5 factory_footprint_limit_m2

| Attribute | Value |
|---|---|
| **Definition** | The hard physical floor space limit of the factory. A scenario whose computed `production_footprint_m2` exceeds this value violates the factory space constraint. |
| **Canonical field** | `factory_footprint_limit_m2` (float, top-level factory field in canonical scenario) |
| **Usage** | Failure mode detection: Footprint Constraint Violation. Also used in ROI analysis (footprint reduction as a value driver). |

---

### 4.6 unit_buffer_area

| Attribute | Value |
|---|---|
| **Definition** | The floor area required to store one unit of WIP at a given stage boundary. Used to compute WIP Buffer Area. |
| **Canonical field** | `unit_buffer_area_m2` (float, per stage or process level) |
| **Unit** | m² per piece |

---

## 5. Key Performance Indicators

### 5.1 Throughput

| Attribute | Value |
|---|---|
| **Definition** | The number of completed units produced per unit of time. Measured at process output or final product level. |
| **Unit** | Units per hour (or per shift, per day — normalized in analytics) |
| **Field** | `throughput` in simulation_result.json and analysis_response.json |

---

### 5.2 Stage Utilization

| Attribute | Value |
|---|---|
| **Definition** | The fraction of available time a WorkUnit at a stage is actively processing (not idle, not blocked, not starved, not in downtime). |
| **Formula** | `utilization = active_time / available_time` |
| **Range** | 0.0 – 1.0 |
| **Field** | `stage_utilization` (per stage) in simulation_result.json |

---

### 5.3 Capacity Utilization

| Attribute | Value |
|---|---|
| **Definition** | The ratio of actual throughput to maximum theoretical throughput of a process or stage. |
| **Formula** | `capacity_utilization = actual_throughput / max_theoretical_throughput` |
| **Field** | `capacity_utilization` in analysis_response.json |

---

### 5.4 Operator Utilization

| Attribute | Value |
|---|---|
| **Definition** | The fraction of available time an operator is actively engaged in productive work (loading, unloading, operating, or attending a WorkUnit). |
| **Formula** | `operator_utilization = active_operator_time / total_operator_available_time` |
| **Field** | `operator_utilization` (per stage or process) in analysis_response.json |
| **Usage** | Labor Utilization Imbalance detection: high variance in operator_utilization across stages indicates imbalance. |

---

### 5.5 throughput_per_m2

| Attribute | Value |
|---|---|
| **Definition** | Production capacity density — units produced per unit of floor area consumed. A key efficiency metric for space-constrained factories. |
| **Formula** | `throughput_per_m2 = throughput / production_footprint_m2` |
| **Unit** | Units per hour per m² |
| **Field** | `throughput_per_m2` in analysis_response.json |
| **Usage** | Comparing automation scenarios: a scenario with higher throughput but proportionally larger footprint may have lower capacity density than a compact semi-auto alternative. |

---

### 5.6 WIP Ratio

| Attribute | Value |
|---|---|
| **Definition** | The ratio of actual total WIP to a reference or baseline WIP level. Indicates relative flow stability. |
| **Formula** | `wip_ratio = total_wip / baseline_wip` |
| **Field** | `wip_ratio` in analysis_response.json |
| **Usage** | WIP stability analysis. A wip_ratio > 1 indicates accumulation relative to baseline. |

---

### 5.7 ROI (Return on Investment)

| Attribute | Value |
|---|---|
| **Definition** | The net financial return of an investment (automation, equipment replacement) relative to its cost, over a defined period. |
| **Formula** | `ROI = (Net_Gain - CAPEX) / CAPEX × 100%` |
| **Field** | `roi_percent` in recommendation.json |

---

### 5.8 Payback Period

| Attribute | Value |
|---|---|
| **Definition** | The time required for the cumulative financial benefit of an investment to equal its initial cost. |
| **Unit** | Years |
| **Field** | `payback_years` in recommendation.json |

---

### 5.9 Comparison Delta Metrics

Used in A/B scenario comparison. All computed from stored artifacts — never by re-invoking engines.

| Field | Definition |
|---|---|
| `throughput_delta` | Candidate throughput − Baseline throughput |
| `lead_time_delta` | Candidate lead_time_estimate − Baseline lead_time_estimate |
| `wip_delta` | Candidate total_wip − Baseline total_wip |
| `footprint_delta` | Candidate production_footprint_m2 − Baseline production_footprint_m2 |
| `roi_delta` | Candidate ROI − Baseline ROI |
| `throughput_per_m2_delta` | Candidate throughput_per_m2 − Baseline throughput_per_m2 |

---

## 6. Failure Mode Definitions

The following 10 failure modes are first-class domain concepts. Analytics v1 must detect all of them. Each has a defined name, definition, detection signal, and canonical field reference.

---

### FM-01: Downstream Blocking

| Attribute | Value |
|---|---|
| **Definition** | Automation increases upstream output beyond downstream capacity, causing the upstream WorkUnit to block. |
| **Detection signals** | `blocking_time` elevated; downstream `stage_utilization` near 100%; WIP accumulation at stage boundary |
| **Effect** | Upstream effective utilization drops; WIP explodes between stages |

---

### FM-02: Upstream Starvation

| Attribute | Value |
|---|---|
| **Definition** | An automated machine requires high input rate but upstream cannot supply sufficient material, causing the machine to be starved. |
| **Detection signals** | `starvation_time` elevated; upstream `stage_utilization` near 100%; auto machine utilization below threshold |
| **Effect** | Auto machine idle despite being capable; ROI expectations not met |

---

### FM-03: Batch Size Mismatch

| Attribute | Value |
|---|---|
| **Definition** | Incompatible batch sizes between consecutive stages create transfer delays and uneven WIP accumulation. |
| **Example** | Auto stage batch = 3000; downstream stage batch = 600 → 5× mismatch |
| **Detection signals** | `batch_size` ratio > threshold between adjacent stages; elevated `transfer_delay`; WIP oscillation |
| **Effect** | Flow instability; unpredictable lead time; buffer overflow |

---

### FM-04: Bottleneck Migration

| Attribute | Value |
|---|---|
| **Definition** | Automation removes an existing bottleneck but creates a new one at a different stage, limiting overall throughput gain. |
| **Detection signals** | Bottleneck stage shifts between baseline and candidate scenario; throughput improvement smaller than expected from the automated stage's cycle time improvement alone |
| **Effect** | Actual throughput gain < modeled gain; investment ROI lower than projected |

---

### FM-05: WIP Explosion

| Attribute | Value |
|---|---|
| **Definition** | Flow imbalance causes uncontrolled WIP accumulation, increasing lead time disproportionately. |
| **Relationship** | Via Little's Law: `Lead Time ≈ WIP / Throughput` — WIP increase at constant throughput directly increases lead time |
| **Detection signals** | `total_wip` growth rate positive and sustained; `lead_time_estimate` increasing; `wip_ratio` > threshold |
| **Effect** | Increased inventory cost; unpredictable delivery time; potential floor space exhaustion |

---

### FM-06: Reliability Dominance

| Attribute | Value |
|---|---|
| **Definition** | High automation introduces lower equipment reliability (MTBF), making unplanned downtime the dominant throughput constraint. |
| **Detection signals** | Availability (`mtbf / (mtbf + mttr/60)`) of automated WorkUnit below threshold; downtime contribution exceeds labor or cycle time as primary loss driver |
| **Effect** | Production volatility; throughput variance high; effective capacity much lower than theoretical capacity |

---

### FM-07: Single Point of Failure

| Attribute | Value |
|---|---|
| **Definition** | An integrated automated cell covers multiple SOP stages. If the cell fails, all covered stages stop simultaneously, with no fallback. |
| **Detection signals** | `covered_stage_ids.length > 1` on a WorkUnit with `count = 1` and no redundant unit in the pool; no legacy manual backup |
| **Effect** | System resilience reduced; failure impact multiplied across covered stages |

---

### FM-08: Footprint Constraint Violation

| Attribute | Value |
|---|---|
| **Definition** | The automation scenario's computed production footprint exceeds the factory's hard space limit. |
| **Detection signals** | `production_footprint_m2 > factory_footprint_limit_m2` |
| **Effect** | Scenario is physically infeasible as configured; requires layout redesign, equipment retirement, or reduced automation scope |

---

### FM-09: Labor Utilization Imbalance

| Attribute | Value |
|---|---|
| **Definition** | Automation changes labor requirements unevenly across stages, creating operator idle time at some stages and overload at others. |
| **Detection signals** | High variance in `operator_utilization` across stages; some stages < threshold (underutilized), others > threshold (overloaded) |
| **Effect** | Inefficient headcount deployment; bottleneck at high-utilization stages; hidden labor cost |

---

### FM-10: ROI Illusion

| Attribute | Value |
|---|---|
| **Definition** | Automation increases system capacity significantly beyond actual demand, resulting in low equipment utilization and an extended payback period. The investment is financially unjustified at current demand. |
| **Detection signals** | `system_capacity >> demand_target`; `capacity_utilization` post-automation well below economical threshold; `payback_years` exceeds acceptable limit |
| **Effect** | CAPEX deployed prematurely; long payback; low ROI relative to demand-matched alternatives |

---

### Failure Mode Summary Table

| ID | Name | Primary Detection Field | Output Location |
|---|---|---|---|
| FM-01 | Downstream Blocking | `blocking_time` | analysis_response.json |
| FM-02 | Upstream Starvation | `starvation_time` | analysis_response.json |
| FM-03 | Batch Size Mismatch | `batch_size` ratio | analysis_response.json |
| FM-04 | Bottleneck Migration | bottleneck stage shift | analysis_response.json |
| FM-05 | WIP Explosion | `total_wip`, `wip_ratio` | analysis_response.json |
| FM-06 | Reliability Dominance | availability, downtime share | analysis_response.json |
| FM-07 | Single Point of Failure | `covered_stage_ids.length`, `count` | analysis_response.json |
| FM-08 | Footprint Constraint Violation | `production_footprint_m2` vs limit | analysis_response.json |
| FM-09 | Labor Utilization Imbalance | `operator_utilization` variance | analysis_response.json |
| FM-10 | ROI Illusion | `capacity_utilization`, `payback_years` | analysis_response.json |

---

## 7. Reliability Fields

| Field | Definition | Unit |
|---|---|---|
| `mtbf_hours` | Mean Time Between Failures | Hours |
| `mttr_minutes` | Mean Time To Repair | Minutes |
| `age_years` | Current equipment age (optional) | Years |
| `useful_life_years` | Manufacturer-defined useful service life | Years |
| `degradation_model` | Optional availability decay model type | Enum or null |
| `availability` | Derived: `mtbf / (mtbf + mttr/60)` | 0.0 – 1.0 |

---

## 8. Status Enumerations

### Run Status

| Value | Meaning |
|---|---|
| `Created` | Run record created, snapshot written |
| `Validating` | Schema validation in progress |
| `Queued` | Validation passed, waiting for concurrency slot |
| `Running` | At least one job is executing |
| `Completed` | All jobs finished successfully |
| `Failed` | Validation or at least one job failed |
| `Cancelled` | Cancelled before completion |

### Job Status

| Value | Meaning |
|---|---|
| `Pending` | Awaiting prerequisite |
| `Queued` | Ready to execute |
| `Running` | Engine process active |
| `Completed` | Engine exited 0, outputs validated |
| `Failed` | Engine error, timeout, or invalid output |

### Job Type

| Value | Engine |
|---|---|
| `Simulation` | C++ CLI |
| `Analytics` | Python CLI |

### WorkUnit Automation Level (`unit_type`)

| Value | Description |
|---|---|
| `manual` | Human-operated; stops during breaks |
| `semi_auto` | Requires operator loading/unloading; stops during breaks |
| `auto` | Fully automatic; may continue through breaks if `requires_operator_presence = false` |

---

## 9. Architectural Constraints (Data Layer)

| Rule | Detail |
|---|---|
| No `stage_id` singular on WorkUnit | Only `covered_stage_ids[]` (always array) |
| Integration = `covered_stage_ids.length > 1` | Structural condition, not a type or flag |
| Stage weights computed by Adapter | Pre-materialized in canonical; engines never compute attribution |
| Domain execution data not in DB | Process structure, BOM, WorkUnit definitions, stage weights — JSON artifacts only |
| No WIP tracking per unit | Aggregate quantities only; no serial/lot/routing |
| Multi-process in canonical | Top-level `processes[]` array; top-level `bom[]` array |
| `factory_footprint_limit_m2` is top-level | Factory-level constraint field in canonical scenario |
| Comparison uses stored artifacts only | A/B comparison never re-invokes engines |
| WIP/footprint/blocking/starvation in simulation_result | Simulator must output these; analytics reads from artifacts |

---

## 10. Abbreviations and Acronyms

| Abbreviation | Full Form |
|---|---|
| PIDSS | Production Intelligence & Decision Support System |
| SOP | Standard Operating Procedure |
| BOM | Bill of Materials |
| KPI | Key Performance Indicator |
| ROI | Return on Investment |
| CAPEX | Capital Expenditure |
| OPEX | Operational Expenditure |
| MTBF | Mean Time Between Failures |
| MTTR | Mean Time To Repair |
| WIP | Work In Progress |
| OEE | Overall Equipment Effectiveness |
| OE | Operational Excellence |
| MES | Manufacturing Execution System |
| ERP | Enterprise Resource Planning |
| SCADA | Supervisory Control and Data Acquisition |
| PLC | Programmable Logic Controller |
| UUID | Universally Unique Identifier |
| CLI | Command Line Interface |
| ADR | Architecture Decision Record |
| FM | Failure Mode |
