# Production Intelligence & Decision Support System (PIDSS)

## Data Flow

### 1. Data Platform Pipeline

```
Synthetic MES / CSV / External Source
        ↓
[Ingestion]
        ↓
[Feature Engineering]
        ↓
Feature Store (versioned)
        ↓
[Calibration Engine]
        ↓
Calibration Profile Store (versioned)
```

### 2 Execution Pipeline (FINAL — FIXED)

```
User Input
        ↓
[ScenarioBuilder]
        ↓
Scenario Snapshot (enriched, still PUBLIC schema)
        ↓
[Adapter]
        ↓
Canonical Scenario (ONLY HERE)
        ↓
Simulation → Analytics → Optimization
```

---