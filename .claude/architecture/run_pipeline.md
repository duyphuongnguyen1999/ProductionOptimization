# Production Intelligence & Decision Support System (PIDSS)

## Run Pipeline

### 1. Execution Pipeline (FINAL — FIXED)

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

## 2. Run-Based Execution Model

Each run:

- run_id (UUID v4)
- immutable
- artifacts stored under:

```graphql
artifacts/{run_id}/
   scenario_snapshot.json
   canonical_scenario.json
   simulation_result.json
   production_records.csv
   analysis_response.json
   recommendation.json
   artifact_manifest.json
   logs/
```

Append-only. Never overwrite.

## 3. Artifact Manifest & Engine Version Tracking

Each run must generate:

artifact_manifest.json

The manifest records:

- run_id
- artifact list
- artifact creation timestamps
- simulator_version
- analytics_version

Purpose:

- run reproducibility
- artifact lineage tracking
- debugging support
- comparison traceability
