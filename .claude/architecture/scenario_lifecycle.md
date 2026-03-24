# Production Intelligence & Decision Support System (PIDSS)

## SCENARIO LIFECYCLE 

### 1. Pipeline

```
User Input
   ↓
ScenarioBuilder
   ↓
Scenario Snapshot (public-like, enriched)
   ↓
Adapter
   ↓
Canonical Scenario (internal, deterministic)
```

### 2. Scenario Snapshot

- Immutable
- Stored immediately
- Contains:
	- user input
	- enriched data (feature + calibration reference)
- STILL follows PUBLIC schema

```
Snapshot = audit truth
Canonical = execution truth
```

### 3. Canonical Scenario

- Internal only
- Engine-facing
- Fully normalized
- Deterministic
- No version ambiguity
