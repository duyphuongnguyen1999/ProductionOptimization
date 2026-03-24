# Production Intelligence & Decision Support System (PIDSS)

## Canonical Model

### 1. Canonical Scenario (Internal Execution Model)

Platform must:

- Validate public scenario against schema
- Adapt version → canonical model
- Compute stage_weights if needed
- Output canonical_scenario.json

Canonical Scenario:

- No version ambiguity
- No oneOf
- No nullable ambiguity
- covered_stage_ids always array
- Explicit integration object when multi-stage
- Engines consume canonical only

Engines (C++ & Python) only read canonical format.

### 2. Adapter Strategy

Only Platform (.NET) handles:

- schema validation
- version adaptation
- canonicalization
- Stage weight computation
- Normalization

Simulator & Analytics:

- NEVER parse public schema versions
- NEVER handle version branching