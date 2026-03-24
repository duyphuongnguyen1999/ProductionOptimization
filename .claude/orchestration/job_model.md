# JOB MODEL

## 1. Overview

A run is composed of multiple execution jobs.

Jobs represent independent execution units within the pipeline.

---

## 2. Job Types

### 2.1 Simulation Job

- Executes C++ simulation engine
- Input:
  - canonical_scenario.json
- Output:
  - simulation_result.json
  - production_records.csv

---

### 2.2 Analytics Job

- Executes Python analytics engine
- Input:
  - simulation outputs
- Output:
  - analysis_response.json
  - recommendation.json

---

## 3. Job Dependency Graph
```
Simulation → Analytics
```


Rules:

- Analytics depends on Simulation
- No parallel execution between them (v1)

---

## 4. Job Execution Model

Each job:

- is stateless
- executes per run
- has isolated input/output

---

## 5. Job Metadata (Database)

Each job record includes:

- job_id
- run_id
- job_type
- status
- start_time
- end_time
- exit_code

---

## 6. Failure Propagation

| Job | Failure Impact |
|-----|--------|
| Simulation | Entire run fails |
| Analytics | Run marked partial or failed |

---

## 7. Future Extensions

- Retry policy
- Parallel execution (Optimization phase)
- Distributed workers