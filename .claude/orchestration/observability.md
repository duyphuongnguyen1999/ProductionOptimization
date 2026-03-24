# OBSERVABILITY

## 1. Purpose

PIDSS ensures full traceability and reproducibility of every run.

Observability is achieved through:

- artifact tracking
- structured logging
- execution metadata

---

## 2. Artifact-Based Observability

Each run produces a complete artifact set:
```
artifacts/{run_id}/
```

Includes:

- scenario_snapshot.json
- canonical_scenario.json
- simulation_result.json
- production_records.csv
- analysis_response.json
- recommendation.json

---

## 3. Logging

Logs are stored per run:

```
artifacts/{run_id}/logs/
```

Log files:

- platform.log
- simulator.log
- analytics.log

---

## 4. Artifact Manifest

Each run MUST generate:
```
artifact_manifest.json
```

Contains:

- run_id
- artifact list
- creation timestamps
- engine versions:
  - simulator_version
  - analytics_version

---

## 5. Reproducibility

A run is reproducible if:

- canonical scenario is identical
- random_seed is identical
- engine versions are identical

---

## 6. Debugging Support

System must allow:

- tracing pipeline step-by-step
- inspecting intermediate artifacts
- identifying failure stage

---

## 7. Observability Constraints

- No hidden state allowed
- All execution must be artifact-driven
- Logs must be deterministic and structured