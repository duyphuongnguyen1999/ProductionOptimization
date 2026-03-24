# Production Intelligence & Decision Support System (PIDSS)

## Phase 2 — Public Contracts & Schemas

### Target: 

Define external API contracts.

### Define

#### 1. Input Schema
- `scenario.schema.json`
- versioned via `schema_version`

#### 2. Output Schemas
- `simulation_result.schema.json`
- `analysis_response.schema.json`
- `recommendation.schema.json`

#### 3. Validation Rules
- `required`
- `additionalProperties = false`
- enums
- constraints

### Enforce
- Public schema ≠ Canonical model
- Adapter handles:
	- validation
	- version mapping
- Engines:
	- NEVER parse public schema

---