# PIDSS Run Pipeline (Final Architecture-Aligned Version)

This pipeline reflects the actual PIDSS architecture:

- Platform (.NET) handles validation, versioning, adapter, orchestration, and persistence  
- C++ CLI performs aggregate simulation  
- Python CLI performs KPI computation and recommendation  
- All runs are immutable and artifact-based  

---

## 1. Ingest & Run Creation (Platform)

1. `POST /runs` receives the public scenario payload (including `schema_version`).
2. Create a `runs` record and assign a `run_id` (UUID).
3. Create initial `jobs` entries (Simulation = Pending, Analytics = Pending).
4. Persist the snapshot:
   - `artifacts/{run_id}/scenario_snapshot.json` (immutable).

---

## 2. Validation & Canonicalization (Platform-only)

1. Validate the payload against the JSON Schema in `data/schemas/` using `schema_version`.
2. If validation fails:
   - Persist validation errors.
   - Mark run as `Failed`.
   - Stop execution.
3. Transform the public scenario into the internal execution model:
   - Generate `canonical_scenario.json`.
   - Engines must not perform version handling.
4. Persist canonical artifact:
   - `artifacts/{run_id}/canonical_scenario.json` (immutable).

---

## 3. Execution Preparation (Platform)

1. Apply concurrency control (`max_concurrent_runs`, FIFO queue).
2. Prepare execution environment:
   - Ensure artifact directories exist.
   - Create `logs/` folder.
   - Set required environment variables.
3. Update run and job status to `Queued` → `Running`.

---

## 4. Simulation Execution (C++ Engine)

1. Invoke the C++ simulator CLI with:
   - Input: `canonical_scenario.json`.
2. Monitor execution:
   - Capture exit code.
   - Capture stdout/stderr.
   - Record runtime metrics.
3. Persist outputs immutably:
   - `simulation_result.json`
   - `production_records.csv`
   - Simulator logs under `logs/`
4. If simulator fails:
   - Mark simulation job and run as `Failed`.
   - Stop pipeline.

---

## 5. Analytics Execution (Python Engine)

1. Invoke the Python analytics CLI with simulator outputs.
2. Monitor execution:
   - Capture exit code.
   - Capture stdout/stderr.
   - Record runtime metrics.
3. Persist outputs immutably:
   - `analysis_response.json`
   - `recommendation.json`
   - Analytics logs under `logs/`
4. If analytics fails:
   - Mark analytics job and run as `Failed`.

---

## 6. Post-Processing & Persistence (Platform)

1. Extract KPI summaries from `analysis_response.json`.
2. Persist metrics into `run_metrics`.
3. Persist recommendations into `run_recommendations`.
4. Index all artifacts in `run_artifacts`.
5. Optionally generate `artifact_manifest.json`.

---

## 7. Status & Observability

1. Finalize statuses:
   - Jobs → `Completed`
   - Run → `Completed`
2. Ensure structured logs are stored under:
   - `artifacts/{run_id}/logs/`
3. Guarantee reproducibility using:
   - `scenario_snapshot.json`
   - `canonical_scenario.json`
   - All generated artifacts

---

## Architectural Constraints

- Platform owns validation, versioning, adapter logic, and orchestration.
- Engines consume canonical format only.
- Artifacts are append-only and immutable per run.
- No MES logic, no dispatching, no real-time control.
- Designed for on-prem deployment and bounded parallel execution.
