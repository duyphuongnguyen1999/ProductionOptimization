# PIDSS Naming Conventions

## 1. General Principles

- Names must be descriptive, unambiguous, and consistent across all layers.
- Abbreviations are only permitted when universally understood (e.g., `id`, `kpi`, `roi`, `wip`).
- All names are English except domain-specific Vietnamese terms in documentation.

---

## 2. JSON Field Names

All JSON fields (in public contracts and canonical model) use **`snake_case`**.

```json
{
  "run_id": "...",
  "schema_version": "...",
  "covered_stage_ids": [],
  "cycle_time_mean_sec": 0,
  "requires_operator_presence": true
}
```

---

## 3. Enum Values

All enum values use **`snake_case`** lowercase strings.

```json
"unit_type": "semi_auto"
"run_status": "completed"
"job_name": "simulation"
```

Standard enum sets:

### unit_type
```
manual | semi_auto | auto
```

### run_status
```
created | queued | running | completed | failed
```

### job_status
```
pending | running | completed | failed
```

### job_name
```
simulation | analytics
```

---

## 4. File Names

| Type | Convention | Example |
|---|---|---|
| JSON Schema | `{entity}.v{N}.schema.json` | `scenario.v1.schema.json` |
| Example Contract | `{entity}.example.json` | `scenario.example.json` |
| Artifact | `{artifact_type}.json` / `.csv` | `simulation_result.json` |
| Documentation | `UPPER_SNAKE_CASE.md` | `VERSIONING_POLICY.md` |
| Vietnamese Doc | `{NAME}_VI.md` | `VERSIONING_POLICY_VI.md` |

---

## 5. Identifiers

| Identifier | Format | Example |
|---|---|---|
| `run_id` | UUID v4 lowercase hyphenated | `"a3f1c2d4-..."` |
| `job_id` | UUID v4 lowercase hyphenated | `"b9e7a001-..."` |
| `stage_id` | Stable string slug | `"pressing"`, `"welding"` |
| `unit_id` | Stable string slug | `"press_machine_01"` |
| `process_id` | Stable string slug | `"assembly"` |
| `product_id` | Stable string slug | `"final_product_a"` |
| `component_id` | Stable string slug | `"sub_assembly_x"` |

Rules for slug identifiers:

- Lowercase letters, digits, and underscores only.
- No spaces. No hyphens (except UUIDs).
- Stable — never renamed after being established in production data.

---

## 6. C# Naming (Platform)

| Element | Convention | Example |
|---|---|---|
| Class | PascalCase | `ScenarioAdapterService` |
| Interface | IPascalCase | `IRunRepository` |
| Method | PascalCase | `ExecuteSimulationAsync` |
| Property | PascalCase | `RunId` |
| Private field | _camelCase | `_runRepository` |
| Local variable | camelCase | `canonicalScenario` |
| Constant | UPPER_SNAKE | `MAX_CONCURRENT_RUNS` |
| DTO suffix | Dto | `RunStatusDto` |
| Service suffix | Service | `CanonicalAdapterService` |
| Repository suffix | Repository | `RunRepository` |

---

## 7. C++ Naming (Simulator)

| Element | Convention | Example |
|---|---|---|
| Class | PascalCase | `AggregateSimulator` |
| Method | PascalCase | `RunSimulation` |
| Member variable | m_camelCase | `m_batchSize` |
| Local variable | camelCase | `stageCapacity` |
| Constant | k_UPPER_SNAKE | `k_MAX_STAGES` |
| File | PascalCase.cpp / .h | `SimulationEngine.cpp` |

---

## 8. Python Naming (Analytics)

Follows PEP 8:

| Element | Convention | Example |
|---|---|---|
| Module | snake_case | `kpi_calculator.py` |
| Class | PascalCase | `BottleneckDetector` |
| Function | snake_case | `compute_throughput` |
| Variable | snake_case | `stage_utilization` |
| Constant | UPPER_SNAKE | `MAX_WIP_THRESHOLD` |

---

## 9. Database Naming (SQL)

| Element | Convention | Example |
|---|---|---|
| Table | snake_case plural | `runs`, `jobs`, `run_metrics` |
| Column | snake_case | `run_id`, `created_at` |
| PK | `id` (surrogate) or `run_id` | `run_id UUID PRIMARY KEY` |
| FK | `{ref_table_singular}_id` | `run_id`, `job_id` |
| Index | `idx_{table}_{column}` | `idx_runs_status` |
