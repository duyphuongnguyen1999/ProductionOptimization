# Production Intelligence & Decision Support System (PIDSS)

## DATA PLATFORM STORAGE (PHYSICAL vs LOGICAL)

### 1. Physical Storage

```
data_storage/
├─ feature_store/
│   └─ {feature_set_id}.json
│
├─ calibration_store/
│   ├─ profiles/
│   │   └─ {profile_id}.json
│   └─ index.json
│
├─ model_store/
├─ dataset_registry/
```

### 2. Logical Classification

Feature Store

- Type: Data Artifact
- Produced by: Feature Engineering
- Contains:
	- aggregated features
	- throughput history
	- demand patterns
	- process structure (optional)

Calibration Profile Store

```
Calibration Profile is NOT a data artifact
Calibration Profile is a MODEL ARTIFACT
```

- Produced by: Calibration Engine
- Contains:
	- estimated parameters
	- distributions
	- system behavior model

### 3. Conceptual Separation

```
Feature Store = Observed Reality
Calibration Profile = Interpreted Reality
Scenario = Hypothetical Reality
```
