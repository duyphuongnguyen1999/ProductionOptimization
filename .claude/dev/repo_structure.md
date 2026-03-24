# Production Intelligence & Decision Support System (PIDSS)

## REPOSITORY STRUCTURE

```graphql
ProductionOptimization/
├─ data/                              # Data governance layer
│   ├─ contracts/
│   ├─ schemas/
│   ├─ validation/
│   ├─ transforms/
│   ├─ lineage/
│   └─ documentation/
|
├─ platform/
│   └─ Pidss.Platform/
│       ├─ Api/
│       │   ├─ Controllers/
│       │   ├─ Contracts/
│       │   ├─ Middleware/
│       │   └─ Filters/
│       ├─ Application/               # Use cases / orchestration
│       ├─ ScenarioBuilder/           # Scenario construction logic
│       ├─ DataSources/               # Read-only abstraction layer
│       └─ Adapters/                  # Canonicalization authority
|
├─ data_platform/
│   ├─ ingestion/
│   │   └─ Pidss.DataPlatform.Ingestion/
│   ├─ feature_engineering/
│   │   └─ Pidss.DataPlatform.FeatureEngineering/
│   ├─ calibration/
│   │   └─ Pidss.DataPlatform.Calibration/
│   └─ synthetic/
│       ├─ demand/                   # future
│       ├─ telemetry/                # future
│       └─ mes/
│           ├─ Pidss.DataPlatform.Synthetic.Mes.Generator/
│           └─ Pidss.DataPlatform.Synthetic.Mes.Api/
|
├─ engines/                           # EXECUTION ONLY
│   ├─ simulation/
│   │   └─ Pidss.Simulation/
│   ├─ analytics/
│   │   └─ Pidss.Analytics/
│   └─ optimization/
│       └─ Pidss.Optimization/
|
├─ presentation/
│   ├─ web/
│	|	└─ Pidss.Web.React/
│   ├─ desktop/			
│	|	└─ Pidss.Desktop.Winforms/	  # future  
│   └─ mobile/						  # future
|
├─ artifacts/                         # RUN-BASED IMMUTABLE STORAGE
│   └─ {run_id}/
│        ├─ scenario_snapshot.json
│        ├─ canonical_scenario.json
│        ├─ simulation_result.json
│        ├─ production_records.csv
│        ├─ analysis_response.json
│        ├─ recommendation.json
│        ├─ artifact_manifest.json
│        └─ logs/
│           ├─ platform.log
│           ├─ simulator.log
│           └─ analytics.log
|
├─ data_storage/                      # DATA PLATFORM OUTPUTS
│   ├─ feature_store/
│   │   └─ {feature_set_id}.json
│   ├─ calibration_store/
│   │   ├─ profiles/
│   │   │   └─ {profile_id}.json
│   │   └─ index.json
│   ├─ model_store/                  # future ML
│   │   └─ {model_id}/
│   │       ├─ model.pkl
│   │       ├─ metadata.json
│   │       └─ metrics.json
│   └─ dataset_registry/             # lineage & catalog
|
└─ docs/
```

Rules:
```
artifacts/ = runtime output (append-only)
data_storage/ = data platform output
```