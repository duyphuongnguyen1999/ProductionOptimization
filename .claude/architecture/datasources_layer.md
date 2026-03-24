# Production Intelligence & Decision Support System (PIDSS)

## DATA ACCESS LAYER (DataSources)

### 1. Purpose

DataSources = read-only abstraction layer giữa Platform và Data Platform

### 2. Interfaces (conceptual)

- `IFeatureStoreReader`
- `ICalibrationProfileProvider`

### 3. STRICT RULES

DataSources MUST NOT:

- call external APIs directly
- contain business logic
- perform transformation
- join / aggregate / derive data

DataSources ONLY:

- read from storage
- map to DTO

### 4. Architectural Benefit

Ensures:

- ScenarioBuilder does not be data pipeline
- No modeling logic leak
- Explicit separation:

```
Data Platform → prepare data
Platform → build scenario
Engines → evaluate scenario
```

---
