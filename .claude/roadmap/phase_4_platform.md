# Production Intelligence & Decision Support System (PIDSS)

## Phase 4 — Platform Core (ScenarioBuilder + Adapter + Orchestration)

### Target: 
Implement core runtime pipeline.

### Implement:

#### 1. ScenarioBuilder
- Read via DataSources ONLY
- Merge:
	- user input
	- feature store
	- calibration profile
- Output:
```
scenario_snapshot.json (enriched, public schema)
```

#### 2. DataSources
- Read-only abstraction
- No logic, no transformation

#### 3. Adapter (CRITICAL)
- Schema validation
- Version adaptation
- Canonicalization
- Stage weight computation

#### 4. Orchestration
- Run creation
- Concurrency control (FIFO)
- Engine invocation (C++ / Python)
- Logging
- Status transitions
- Failure handling

Enforce

- ScenarioBuilder MUST NOT produce canonical
- Adapter = ONLY canonical authority
- Canonical stored immutably

---