# Production Intelligence & Decision Support System (PIDSS)

# 1. CORE BUSINESS PROBLEM

Goal:
Increase manufacturing capacity by ~50% in 5 years without increasing:

- factory footprint
- headcount

Strategies evaluated:

- Labor optimization
- Downtime reduction
- Defect reduction
- Semi-automation
- Full automation
- Stage integration (equipment covering multiple SOP stages)
- Equipment replacement & lifecycle optimization

The system must support financial evaluation:

- CAPEX
- OPEX
- Useful life
- ROI
- Payback period
- Footprint reduction impact

---

# 2. REAL FACTORY CONTEXT (FINALIZED DOMAIN REALITY)

## 2.1. Process

Transforms raw materials → component or final product.  

Defined by SOP. Rarely changes.

A factory may contain multiple processes.

Some processes produce:
- Components (semi-finished goods)
- Final products (assembly / packaging)

## 2.2. Component

Output of a process.

Consumed by downstream process via BOM.

Modeled in aggregate only (no WIP tracking).

## 2.3. Product (Final Product)

Assembled or packaged from multiple components.

Final production capacity is constrained by:

- Final assembly capacity
- Upstream component availability

## 2.4. BOM (Bill of Materials)

Defines:

- product - component mapping
- quantity_required_per_product

Final output limited by minimum component availability.

Important:

BOM NOT directly in scenario

→ must be transformed by ScenarioBuilder

## 2.5. Stage

A stable SOP step within a process.

Stage represents business traceability and comparability.

Stage contains:

- stage_id
- order
- name

Critical rule:

> Stage identity MUST NEVER be deleted  
Stage identity MUST NEVER be replaced by automation.  
Stage contains NO execution logic.


## 2.6. Work Unit (Execution Unit)

Execution is equipment-centric.

A WorkUnit may represent:

- Manual workbench
- Semi-automatic machine
- Fully automatic machine

Each WorkUnit defines:

- unit_id
- unit_type (manual / semi_auto / auto)
- covered_stage_ids[] (minItems = 1)
- count
- cycle_time distribution
- operators_per_unit
- requires_operator_presence
- reliability (optional)
- footprint_m2 (optional)
- financial attributes (optional)

## 2.7. Integration Concept

Integrated cell is NOT a separate type.

Integration is defined by:

- `covered_stage_ids.length` > 1

If multiple stages are covered, then:

- An `integration` object must exist
- Adapter MUST compute `stage_weights` 
- `stage_weights` MUST be explicitly materialized in canonical

## 2.8 Line

Logical replication of process.

Important:
- Resources are NOT necessarily 1:1 with lines.  
- Capacity must be modeled at stage resource pool level.

### 2.8.1. Example: Assembly Process (7 Stages)

1. Pressing (semi-auto) – 6–7 sec
2. Welding (semi-auto) – 7–8 sec
3. Manual assembly – ~12 sec
4. Manual connection – 15–17 sec
5. Manual coating – 15–17 sec
6. Silicon processing (semi-auto) – ~20 sec
7. Visual inspection (manual) – ~12 sec

Manual stations:
- Parallel workbenches
- 1–4 operators per bench

Semi-auto:
- Machine-human coupling
- Machine may wait for operator
- Operator may wait for machine

### 2.8.2. Line vs Stage Capacity Reality

There are 7 lines.

However:

- Pressing & Heating: exactly 7 machines
- Other stages: more than 7 workstations

Therefore:

> Capacity constraints must be modeled at stage resource pools,
> NOT at fixed line mapping.

## 2.9. Batch Flow Reality

Current production flow is batch-gated:

- Batch size: 600 pieces
- Transfer only after full batch completion
- Transfer delay: 3–5 minutes (checksheet/confirmation)

Automation goal includes:

- Reducing transfer delay
- Reducing labor
- Reducing footprint
- Increasing throughput

## 2.10. Critical Rule — Integrated Automated Cell

When one automated cell integrates multiple stages:

- DO NOT create new SOP stages
- DO NOT delete original stage identity
- Model automation as execution override
- Preserve stage-level comparability

This ensures:

- A/B comparison validity
- SOP traceability
- Bottleneck reporting consistency

