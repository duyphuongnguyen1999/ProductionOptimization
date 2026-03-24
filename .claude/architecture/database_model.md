# Production Intelligence & Decision Support System (PIDSS)

## DATABASE (RUN METADATA ONLY)

### 1. Purpose

Database is used ONLY for:

- Run tracking
- Job orchestration
- Artifact indexing
- KPI indexing (for query)

### 2. Tables:

- `runs`
- `jobs`
- `run_artifacts`
- `run_metrics`
- `run_recommendations`

### 3. Strict Rules

Domain execution data NOT stored relationally. 

All domain data must stored as: 

- JSON artifacts.
- CSV outputs