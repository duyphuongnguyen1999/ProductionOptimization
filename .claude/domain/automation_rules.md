# Production Intelligence & Decision Support System (PIDSS)

## 1. Automation Scenario Modeling Rules

Automation scenarios must respect realistic manufacturing constraints.

The system must prevent unrealistic automation modeling.

Rules:

### 1.1. Stage Identity Preservation

Automation must NEVER delete or replace Stage definitions.

Stages represent SOP traceability and historical comparability.

Automation is modeled only through WorkUnit execution capacity.

### 1.2. Integrated Cell Representation

If an automated cell covers multiple SOP stages:

- covered_stage_ids must include all affected stages
- integration object must exist
- stage_weights must be materialized in canonical scenario

### 1.3. Batch Compatibility

Automation scenarios must consider batch compatibility across stages.

Large automation batches may cause:

- downstream blocking
- WIP explosion
- flow instability

PIDSS must evaluate automation batch size against downstream batch policy.

### 1.4. Automation Requires System-Level Evaluation

Automation must never be evaluated at machine level only.

Simulation and analytics must evaluate:

- upstream supply capability
- downstream capacity
- WIP accumulation
- footprint impact
- labor redistribution

Automation scenarios must therefore be evaluated at full process level.
