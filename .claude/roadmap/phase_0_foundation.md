# Production Intelligence & Decision Support System (PIDSS)

## Phase 0 — Repository Foundation & Data-Layer Conventions

### Target: 

Establish foundational structure and conventions for PIDSS to ensure the system is alligned with:

- Scenario-driven architecture
- Run-based execution model
- Strict boundary separation

### Define

#### 1. Repository & Structure

- Repository layout (platform / data_platform / engines / artifacts)
- Artifact directory:
```
artifacts/{run_id}/
```
- Logs convention:
```
artifacts/{run_id}/logs/
```

#### 2. Execution Model

- Run lifecycle:
```
Created → Queued → Running → Completed / Failed
```
- Job lifecycle:
```
Pending → Running → Completed / Failed
```

#### 3. Artifact Model

- Artifact classification:
	- snapshot (public)
	- canonical (internal)
	- simulation outputs
	- analytics outputs
	- manifest
- Append-only policy (NO overwrite)

#### 4. Scenario Lifecycle
```
User Input
   ↓
ScenarioBuilder
   ↓
Scenario Snapshot (public)
   ↓
Adapter
   ↓
Canonical Scenario
```

#### 5. Core Interfaces
- `IScenarioBuilder`
- `IDataSources`
	- `IFeatureStoreReader`
	- `ICalibrationProfileProvider`

#### Ensure
- Scenario ≠ Data (strict separation)
- Canonical = single execution source of truth
- Engines consume canonical only
- Data Platform NOT part of runtime execution

#### Out of Scope
- No business logic
- No simulation
- No analytics

---