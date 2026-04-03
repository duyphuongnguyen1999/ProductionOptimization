# PIDSS Data Dictionary

**Version:** 1.1.0  
**Phase:** 0 — Repository Foundation (Updated Phase 1)  
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
| **Key field** | `factory.footprint_limit_m2` — the hard physical floor space limit. Analytics uses this to detect Footprint Constraint Violation (FM-08). |
| **Rules** | A factory may contain multiple parallel or sequential processes. The total production footprint computed by simulation must not exceed this limit in a valid automation scenario. |

---

### 2.2 Process

| Attribute | Value |
|---|---|
| **Definition** | A defined manufacturing workflow that transforms raw materials and intermediate products into a SemiProduct. A factory may contain multiple processes. |
| **Identity** | Defined by an SOP. Stable over long periods. |
| **Output** | A **SemiProduct** — consumed by downstream processes or FinishedProduct BOM. |
| **Scope** | Contains one or more Stages in a fixed sequence. Associated with WorkUnitModels and WorkUnit instances. |
| **Canonical field** | `process_id` (string, slug) |
| **Rules** | Multiple processes may run in parallel. Final capacity is constrained by BOM availability across all upstream processes. |

---

### 2.3 Material

| Attribute | Value |
|---|---|
| **Definition** | Raw materials or purchased components that are **not manufactured** within the factory. |
| **Canonical field** | `material_id` (string, slug) |
| **Example** | `"sheath_tube"`, `"housing"`, `"syringe"`, `"needle"` |
| **Rules** | Defined in `materials[]`. Never produced by any Stage. Appear as inputs to Stages and as BOM entries in SemiProducts and FinishedProducts. |

---

### 2.4 Product Types

PIDSS defines three types of manufactured products.

#### IntermediateProduct

| Attribute | Value |
|---|---|
| **Definition** | The output of a single Stage within a Process. Consumed only by the next Stage in the same Process. |
| **Canonical field** | `product_id` (string, slug) |
| **Type value** | `"intermediate_product"` |
| **BOM** | Has `bill_of_materials[]` listing the materials and prior intermediate product required, with `quantity_required_per_output` per item. |
| **Rules** | Declared in `products[]`. Referenced in stage `input[]` and `output[]`. Enables accurate material consumption and WIP accounting per stage, especially when defect rates vary. |

#### SemiProduct

| Attribute | Value |
|---|---|
| **Definition** | The output of a complete Process. Consumed by a FinishedProduct BOM or by another Process (cross-process dependency). |
| **Canonical field** | `product_id` (string, slug) |
| **Type value** | `"semi_product"` |
| **BOM** | Has `bill_of_materials[]` listing all materials and any upstream semi_products consumed to produce 1 unit, with `quantity_required_per_output` per item. |
| **Rules** | Produced at the final Stage of its Process. BOM may reference other SemiProducts when a process consumes the output of a prior process. |

#### FinishedProduct

| Attribute | Value |
|---|---|
| **Definition** | The final manufactured output assembled or packaged from SemiProducts and materials. |
| **Canonical field** | `product_id` (string, slug) |
| **Type value** | `"finished_product"` |
| **BOM** | Has `bill_of_materials[]` referencing semi_products and materials with `quantity_required_per_output`. |
| **Rules** | Final output = `min over all BOM semi_product entries of (semi_product_throughput / quantity_required_per_output)`. |

---

### 2.5 BOM (Bill of Materials)

| Attribute | Value |
|---|---|
| **Definition** | Defines the inputs required to produce one unit of a product, with their required quantities per output unit. |
| **Location** | Embedded within each product object in `products[]`. There is **no** separate top-level `bom[]` array. |
| **Applies to** | **All three product types**: `intermediate_product`, `semi_product`, and `finished_product`. |
| **Canonical fields** | `type` (`material` \| `intermediate_product` \| `semi_product`), `id`, `quantity_required_per_output` |
| **Rules** | `quantity_required_per_output` is required on every BOM item. Enables the simulator to compute accurate material consumption accounting for defect rates. Analytics uses BOM to identify the binding upstream bottleneck across processes and to compute material cost per unit. |

**Example — IntermediateProduct BOM:**
```json
{
  "product_id": "ip_1_2",
  "type": "intermediate_product",
  "bill_of_materials": [
    { "type": "intermediate_product", "id": "ip_1_1", "quantity_required_per_output": 1 },
    { "type": "material", "id": "valve",     "quantity_required_per_output": 1 },
    { "type": "material", "id": "sheath_cap","quantity_required_per_output": 1 }
  ]
}
```

**Example — SemiProduct BOM (with cross-process reference):**
```json
{
  "product_id": "semi_product_2",
  "type": "semi_product",
  "bill_of_materials": [
    { "type": "semi_product", "id": "semi_product_1", "quantity_required_per_output": 1 },
    { "type": "material",     "id": "housing",        "quantity_required_per_output": 1 }
  ]
}
```

---

### 2.6 Stage

| Attribute | Value |
|---|---|
| **Definition** | A single, stable SOP step within a Process. The unit of business traceability and comparability. |
| **Identity** | **Immutable.** Never deleted, renamed, split, or merged. Automation does not alter Stage identity. |
| **Content** | `stage_id`, `order`, `name`, `eligible_work_unit_model_ids[]`, `input[]`, `output[]`, `stage_parameters`, `wip_model`. |
| **Canonical field** | `stage_id` (string, slug) |
| **Rules** | Stages are the stable anchor for A/B comparison and bottleneck reporting across all scenarios. |

**Stage Parameters (required on every Stage):**

| Field | Definition |
|---|---|
| `defect_rate` | Fraction of output that is defective. Serves as process-design baseline. Simulator uses `work_unit_parameters.defect_rate` (per-machine) as override; stage baseline used by analytics for scenario comparison. |
| `rework.available` | Boolean — whether rework is physically possible at this stage. |
| `rework.rework_rate` | Fraction of defective units that can be successfully reworked. |
| `rework.maximum_rework_cycles` | Maximum number of rework passes allowed before scrapping. |

**WIP Model (required on all stages except last):**

| Field | Definition |
|---|---|
| `buffer_id` | Unique identifier for this buffer. |
| `capacity_units` | Maximum WIP units the buffer can hold. |
| `initial_wip_units` | WIP present at simulation start (typically 0). |
| `buffer_policy.type` | Flow discipline: `"fifo"` (default). |

The last stage of each process has `"wip_model": null`.

---

### 2.7 WorkUnitModel (Equipment Class)

| Attribute | Value |
|---|---|
| **Definition** | A template representing the class characteristics shared by all physical machines of the same model (dòng máy). Not a physical instance. |
| **Automation levels** | `manual`, `semi_auto`, `auto` |
| **Canonical field** | `model_id` (string, slug) |
| **Key fields** | `covered_stage_ids[]`, `type`, `operators_per_unit`, `requires_operator_presence`, `footprint_m2`, `unit_buffer_area_m2`, `transfer_delay_sec`, `batch_size`, `cycle_time_default`, `reliability` (optional), `financial` (optional), `integration` (conditional) |
| **Rules** | `covered_stage_ids[]` is **always an array** (minimum one element). There is no `stage_id` singular field. Integration is defined by `covered_stage_ids.length > 1`. A WorkUnitModel is bound to specific stages and does **not** reuse across different processes. |

> **Critical:** `stage_id` singular field does NOT exist on WorkUnitModel. Only `covered_stage_ids[]`.

**Reliability (optional) — models Availability component of OEE (unplanned downtime):**

| Field | Definition |
|---|---|
| `mtbf_hours` | Mean Time Between Failures |
| `mttr_minutes` | Mean Time To Repair |
| `useful_life_years` | Manufacturer-defined useful service life |
| `degradation_model` | Optional availability decay model (`"linear"` or null) |

Derived: `availability = mtbf / (mtbf + mttr/60)`

---

### 2.8 WorkUnit (Physical Machine Instance)

| Attribute | Value |
|---|---|
| **Definition** | A specific physical machine on the production floor. Carries per-machine observed parameters that may differ from the class (WorkUnitModel) defaults. |
| **Canonical field** | `work_unit_id` (string, slug) — **globally unique** |
| **Key fields** | `work_unit_model_id`, `cycle_time`, `age_years`, `work_unit_parameters` |
| **Rules** | `work_unit_parameters` is **required** on every WorkUnit. The `cycle_time` on a WorkUnit overrides `cycle_time_default` from its model. |

**work_unit_parameters (required on every WorkUnit):**

| Field | Definition | Source |
|---|---|---|
| `defect_rate` | Per-machine observed quality rate. **Overrides** `stage_parameters.defect_rate` in simulation. | Data Platform extracts from MES history via feature engineering |
| `operating_rate` | OEE Performance component — fraction of time machine runs at intended speed (planned losses, speed losses, minor stoppages). | Data Platform extracts from MES history via feature engineering |

**OEE Component Mapping:**

```
OEE = Availability × Performance × Quality

Availability  ← WorkUnitModel.reliability (unplanned downtime — MTBF/MTTR)
Performance   ← WorkUnit.work_unit_parameters.operating_rate
Quality       ← WorkUnit.work_unit_parameters.defect_rate
```

**Defect Rate Resolution (simulator):**
```
effective_defect_rate =
    work_unit.work_unit_parameters.defect_rate   ← primary (per-machine from MES)
    else stage.stage_parameters.defect_rate       ← stage baseline
```

---

### 2.9 Integration (Multi-Stage Coverage)

| Attribute | Value |
|---|---|
| **Definition** | The condition in which a single WorkUnitModel covers two or more consecutive Stages. Defined structurally — not a type. |
| **Condition** | `covered_stage_ids.length > 1` |
| **Canonical requirement** | When integrated, an `integration` object with `stage_weights` **must** be present in the canonical WorkUnitModel. |
| **Rules** | Integration is orthogonal to automation level. Any `type` may be integrated. Covered Stages retain their SOP identity. |

---

### 2.10 Stage Weights

| Attribute | Value |
|---|---|
| **Definition** | Normalized attribution map distributing a WorkUnitModel's execution contribution across its covered Stages. |
| **Canonical field** | `stage_weights` — map of `{ stage_id: float }`, summing to exactly 1.0 |
| **Computed by** | **Platform Adapter only.** Always materialized in `canonical_scenario.json` before engines receive it. |
| **Rules** | Required when `covered_stage_ids.length > 1`. Used by simulator (per-stage output records) and analytics (per-stage KPI attribution, bottleneck ranking). |

---

### 2.11 Scenario

| Attribute | Value |
|---|---|
| **Definition** | A complete description of a hypothetical or baseline production configuration for evaluation. |
| **Public field** | `schema_version` |
| **Canonical** | No `schema_version`. Always current format. Contains materialized stage weights, BOM on all product types, multi-process structure, factory footprint limit, flow policy fields, stage parameters, work_unit_parameters, and random seed. |

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
| **Definition** | The estimated elapsed time for a unit to flow through the full process. |
| **Estimation method** | Little's Law: `Lead Time ≈ Total WIP / Throughput` |
| **Unit** | Hours or seconds (normalized in canonical outputs) |
| **Field** | `lead_time_estimate` in `simulation_result.json` |

---

### 3.3 Little's Law

| Attribute | Value |
|---|---|
| **Formula** | `Lead_Time ≈ Total_WIP / Throughput` |
| **Usage in PIDSS** | Used by Analytics to estimate lead time and detect WIP Explosion. |

---

### 3.4 Blocking

| Attribute | Value |
|---|---|
| **Definition** | The condition in which a Stage cannot transfer its output downstream because the buffer or next stage is full or unavailable. |
| **Field** | `blocking_time` (per stage, in simulation_result.json) |

---

### 3.5 Starvation

| Attribute | Value |
|---|---|
| **Definition** | The condition in which a Stage is idle because it is not receiving sufficient input from upstream. |
| **Field** | `starvation_time` (per stage, in simulation_result.json) |

---

### 3.6 Effective Wait Time

| Attribute | Value |
|---|---|
| **Formula** | `effective_wait_time = batch_gating_delay + transfer_delay + downstream_congestion_time` |
| **Usage** | `WIP_stage ≈ Throughput × Effective_Wait_Time` |

---

### 3.7 Batch Gating

| Attribute | Value |
|---|---|
| **Definition** | Policy by which units are held until a full batch is accumulated before transfer to the next stage. |
| **WIP implication** | Average WIP ≈ `batch_size / 2` in steady state |

---

## 4. Production Footprint Model

### 4.1 Machine Area

| Attribute | Value |
|---|---|
| **Formula** | `Machine_Area = Σ (count_of_instances × model.footprint_m2)` |
| **Field** | `machine_area_m2` in simulation_result.json |

---

### 4.2 WIP Buffer Area

| Attribute | Value |
|---|---|
| **Formula** | `WIP_Area = Σ (WIP_stage × unit_buffer_area_m2)` |
| **Field** | `wip_area_m2` in simulation_result.json |

---

### 4.3 Layout Factor

| Attribute | Value |
|---|---|
| **Definition** | Multiplier for aisles, operator movement, maintenance access. |
| **Range** | 1.2 – 1.4 |
| **Default** | 1.3 |
| **Formula role** | `Production_Footprint = (Machine_Area + WIP_Area) × Layout_Factor` |

---

### 4.4 Production Footprint

| Attribute | Value |
|---|---|
| **Formula** | `Production_Footprint = (Machine_Area + WIP_Area) × Layout_Factor` |
| **Field** | `production_footprint_m2` in simulation_result.json |
| **Constraint** | Analytics compares against `factory.footprint_limit_m2` to detect FM-08 |

---

### 4.5 factory_footprint_limit_m2

| Attribute | Value |
|---|---|
| **Definition** | Hard physical floor space limit of the factory. |
| **Canonical field** | `factory.footprint_limit_m2` (float, top-level factory object) |

---

### 4.6 unit_buffer_area_m2

| Attribute | Value |
|---|---|
| **Definition** | Floor area required to store one unit of WIP at a stage boundary. |
| **Canonical field** | `unit_buffer_area_m2` on WorkUnitModel |

---

## 5. Key Performance Indicators

### 5.1 Throughput

| Attribute | Value |
|---|---|
| **Definition** | Good units (quality-adjusted) produced per unit of time. |
| **Note** | Quality-adjusted: defective units that are scrapped are excluded from throughput count. |
| **Field** | `throughput` in simulation_result.json and analysis_response.json |

---

### 5.2 Stage Utilization

| Attribute | Value |
|---|---|
| **Formula** | `utilization = active_time / available_time` |
| **Range** | 0.0 – 1.0 |
| **Field** | `stage_utilization` (per stage) in simulation_result.json |

---

### 5.3 Capacity Utilization

| Attribute | Value |
|---|---|
| **Formula** | `capacity_utilization = actual_throughput / max_theoretical_throughput` |
| **Field** | `capacity_utilization` in analysis_response.json |

---

### 5.4 Operator Utilization

| Attribute | Value |
|---|---|
| **Formula** | `operator_utilization = active_operator_time / total_operator_available_time` |
| **Field** | `operator_utilization` (per stage) in analysis_response.json |

---

### 5.5 OEE (Overall Equipment Effectiveness)

| Attribute | Value |
|---|---|
| **Formula** | `OEE = Availability × Performance × Quality` |
| **Availability** | From `WorkUnitModel.reliability` — `mtbf / (mtbf + mttr/60)` |
| **Performance** | From `WorkUnit.work_unit_parameters.operating_rate` |
| **Quality** | From `WorkUnit.work_unit_parameters.defect_rate` — `(1 - defect_rate)` |
| **Field** | `effective_oee` per work_unit in simulation_result.json |

---

### 5.6 throughput_per_m2

| Attribute | Value |
|---|---|
| **Formula** | `throughput_per_m2 = throughput / production_footprint_m2` |
| **Field** | `throughput_per_m2` in analysis_response.json |

---

### 5.7 WIP Ratio

| Attribute | Value |
|---|---|
| **Formula** | `wip_ratio = total_wip / baseline_wip` |
| **Field** | `wip_ratio` in analysis_response.json |

---

### 5.8 ROI (Return on Investment)

| Attribute | Value |
|---|---|
| **Formula** | `ROI = (Net_Gain - CAPEX) / CAPEX × 100%` |
| **Field** | `roi_percent` in recommendation.json |

---

### 5.9 Payback Period

| Attribute | Value |
|---|---|
| **Unit** | Years |
| **Field** | `payback_years` in recommendation.json |

---

### 5.10 Comparison Delta Metrics

| Field | Definition |
|---|---|
| `throughput_delta` | Candidate throughput − Baseline throughput |
| `lead_time_delta` | Candidate lead_time_estimate − Baseline |
| `wip_delta` | Candidate total_wip − Baseline |
| `footprint_delta` | Candidate production_footprint_m2 − Baseline |
| `roi_delta` | Candidate ROI − Baseline |
| `throughput_per_m2_delta` | Candidate throughput_per_m2 − Baseline |
| `quality_delta` | Candidate effective scrap rate − Baseline |

---

## 6. Failure Mode Definitions

### FM-01: Downstream Blocking
**Detection:** `blocking_time` elevated; downstream `stage_utilization` near 100%; WIP accumulation at stage boundary.

### FM-02: Upstream Starvation
**Detection:** `starvation_time` elevated; upstream `stage_utilization` near 100%; auto machine utilization below threshold.

### FM-03: Batch Size Mismatch
**Detection:** `batch_size` ratio > threshold between adjacent stages; elevated `transfer_delay`; WIP oscillation.

### FM-04: Bottleneck Migration
**Detection:** Bottleneck stage shifts between baseline and candidate scenario; throughput improvement smaller than expected.

### FM-05: WIP Explosion
**Detection:** `total_wip` growth rate positive and sustained; `lead_time_estimate` increasing; `wip_ratio` > threshold.

### FM-06: Reliability Dominance
**Detection:** `effective_availability` of automated WorkUnit below threshold; downtime contribution exceeds other loss drivers.

### FM-07: Single Point of Failure
**Detection:** `covered_stage_ids.length > 1` on a WorkUnitModel with only one instance and no redundant unit; no legacy manual backup.

### FM-08: Footprint Constraint Violation
**Detection:** `production_footprint_m2 > factory.footprint_limit_m2`.

### FM-09: Labor Utilization Imbalance
**Detection:** High variance in `operator_utilization` across stages.

### FM-10: ROI Illusion
**Detection:** `system_capacity >> demand_target`; `capacity_utilization` post-automation below economical threshold; `payback_years` exceeds acceptable limit.

### Failure Mode Summary Table

| ID | Name | Primary Detection Field |
|---|---|---|
| FM-01 | Downstream Blocking | `blocking_time` |
| FM-02 | Upstream Starvation | `starvation_time` |
| FM-03 | Batch Size Mismatch | `batch_size` ratio |
| FM-04 | Bottleneck Migration | bottleneck stage shift |
| FM-05 | WIP Explosion | `total_wip`, `wip_ratio` |
| FM-06 | Reliability Dominance | `effective_availability` |
| FM-07 | Single Point of Failure | `covered_stage_ids.length`, instance count |
| FM-08 | Footprint Constraint Violation | `production_footprint_m2` vs `factory.footprint_limit_m2` |
| FM-09 | Labor Utilization Imbalance | `operator_utilization` variance |
| FM-10 | ROI Illusion | `capacity_utilization`, `payback_years` |

---

## 7. Reliability Fields

| Field | Definition | Unit |
|---|---|---|
| `mtbf_hours` | Mean Time Between Failures | Hours |
| `mttr_minutes` | Mean Time To Repair | Minutes |
| `useful_life_years` | Manufacturer-defined useful service life | Years |
| `degradation_model` | Optional availability decay model | Enum or null |
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

### WorkUnitModel Automation Level (`type`)

| Value | Description |
|---|---|
| `manual` | Human-operated; stops during breaks |
| `semi_auto` | Requires operator loading/unloading; stops during breaks |
| `auto` | Fully automatic; may continue through breaks if `requires_operator_presence = false` |

---

## 9. Architectural Constraints (Data Layer)

| Rule | Detail |
|---|---|
| No `stage_id` singular on WorkUnitModel | Only `covered_stage_ids[]` (always array, min 1) |
| Integration = `covered_stage_ids.length > 1` | Structural condition, not a type or flag |
| Stage weights computed by Adapter | Pre-materialized in canonical; engines never compute attribution |
| BOM embedded in product objects | `bill_of_materials[]` is inside each product in `products[]`; there is no top-level `bom[]` array |
| BOM required on ALL product types | `intermediate_product`, `semi_product`, and `finished_product` all carry `bill_of_materials[]` |
| `quantity_required_per_output` required on all BOM items | Enables accurate material consumption and capacity constraint computation |
| SemiProduct may reference other SemiProducts in BOM | Reflects real cross-process dependencies |
| Domain execution data not in DB | Process structure, product definitions, BOM, WorkUnitModel definitions, WorkUnit instances, stage parameters, work_unit_parameters — JSON artifacts only |
| No WIP tracking per unit | Aggregate quantities only; no serial/lot/routing |
| `stage_parameters` required on every Stage | `defect_rate` (baseline) and `rework` fields must be present |
| `work_unit_parameters` required on every WorkUnit | `defect_rate` (per-machine override) and `operating_rate` (OEE Performance) must be present |
| OEE fully decomposed | Availability ← reliability on WorkUnitModel; Performance ← `operating_rate` on WorkUnit; Quality ← `defect_rate` on WorkUnit |
| Defect rate precedence | `work_unit.work_unit_parameters.defect_rate` overrides `stage.stage_parameters.defect_rate` in simulation |
| `work_unit_parameters` sourced from MES | Data Platform extracts per-machine `defect_rate` and `operating_rate` from MES history via feature engineering |
| No `planning_period` at top level | Demand lives in `calendar.demand` |
| `factory.footprint_limit_m2` is top-level | Factory-level constraint field in `factory` object of canonical scenario |
| Comparison uses stored artifacts only | A/B comparison never re-invokes engines |
| WIP/footprint/blocking/starvation in simulation_result | Simulator must output these; analytics reads from artifacts |
| WorkUnitModel does not reuse across processes | A model is bound to a specific set of stages; not reused in different processes |

---

## 10. Abbreviations and Acronyms

| Abbreviation | Full Form |
|---|---|
| PIDSS | Production Intelligence & Decision Support System |
| SOP | Standard Operating Procedure |
| BOM | Bill of Materials |
| KPI | Key Performance Indicator |
| OEE | Overall Equipment Effectiveness |
| ROI | Return on Investment |
| CAPEX | Capital Expenditure |
| OPEX | Operational Expenditure |
| MTBF | Mean Time Between Failures |
| MTTR | Mean Time To Repair |
| WIP | Work In Progress |
| MES | Manufacturing Execution System |
| ERP | Enterprise Resource Planning |
| SCADA | Supervisory Control and Data Acquisition |
| PLC | Programmable Logic Controller |
| UUID | Universally Unique Identifier |
| CLI | Command Line Interface |
| ADR | Architecture Decision Record |
| FM | Failure Mode |
