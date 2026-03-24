## Phase 1 — Domain & Canonical Model

### Target: 

Define stable internal execution model, unchanging across versions, that serves as single source of truth for all engines.

### Define:

#### 1. Domain Model
- Stage (SOP identity, immutable)
- WorkUnit (execution unit)
- Integration (multi-stage execution)
- BOM → transformed to constraints (NOT direct in scenario)

#### 2. Canonical Scenario
- Fully normalized
- Deterministic
- No version ambiguity
- Engine-facing only

#### 3. Critical Rules
- `covered_stage_ids`:
	- MUST exist
	- MUST NOT be empty
- Multi-stage:
	- MUST have `integration`
	- MUST have `stage_weights`

#### 4. Execution Modeling
- Equipment-centric execution
- Batch flow modeling
- Transfer delay
- Break behavior
- Reliability (MTBF/MTTR)

#### 5. Determinism
- `random_seed` REQUIRED

### Output:
- `canonical_scenario.example.json`
- Domain explanation document

---