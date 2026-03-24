# Production Intelligence & Decision Support System (PIDSS)

## HARD ARCHITECTURAL RULES

### 1. Scenario Authority

```
Scenario is authoritative
Data sources are NOT authoritative
```

### 2. Engine Isolation Rules

Engines MUST:

- read `canonical_scenario.json` ONLY

Engines MUST NOT:

- read Feature Store
- read Calibration Store
- read raw data
- parse public schema
- perform adaptation

### 3. ScenarioBuilder Rules

ScenarioBuilder MUST:

- read ONLY via DataSources
- merge:
	- user input
	- feature store
	- calibration profile
- output:
	- `scenario_snapshot.json` (enriched, PUBLIC schema)

ScenarioBuilder MUST NOT:

- produce canonical model
- perform schema validation
- perform versioning
- estimate parameters
- fit models
- derive new statistical behavior

### 4. Adapter Rules (SINGLE AUTHORITY — CRITICAL)

Adapter is the ONLY components that can:

- validate schema
- perform version adaptation
- canonicalize scenario
- compute `stage_weights`

```
ScenarioBuilder MUST NOT produce canonical model
Adapter is the ONLY component allowed to produce canonical_scenario.json
```

---
