# PIDSS Repository Conventions

**Version:** 1.0.0
**Phase:** 0 — Repository Foundation & Data-Layer Conventions
**Status:** Active

---

## 1. Repository Structure

```
ProductionOptimization/
├─ data/
│  ├─ contracts/          # Versioned example payloads (input/output samples)
│  ├─ schemas/            # JSON Schema definitions (Draft-07)
│  ├─ validation/         # Validation logic, tests, and rules
│  ├─ transforms/         # Analytical transformation definitions
│  ├─ lineage/            # Artifact and run metadata policies
│  └─ documentation/      # Versioning docs, domain model, data dictionary
├─ platform/
│  └─ Pidss.Platform/     # ASP.NET Core Web API — orchestration, adapter, validation
├─ engines/
│  ├─ simulation/
│  │  └─ Pidss.Simulation/     # C++ CLI — aggregate simulation engine
│  ├─ analytics/
│  │  └─ Pidss.Analytics/      # Python CLI — KPI computation and recommendations
│  └─ optimization/
│     └─ Pidss.Optimization/   # Python CLI — batch scenario exploration
├─ data_platform/
│  ├─ ingestion/
│  │  └─ Pidss.DataPlatform.Ingestion/
│  ├─ feature_engineering/
│  │  └─ Pidss.DataPlatform.FeatureEngineering/
│  ├─ calibration/
│  │  └─ Pidss.DataPlatform.Calibration/
│  └─ synthetic/mes/
│     ├─ Pidss.DataPlatform.Synthetic.Mes.Generator/
│     └─ Pidss.DataPlatform.Synthetic.Mes.Api/
├─ presentation/
│  ├─ web/
│  │  └─ Pidss.Web.React/      # React SPA — primary UI client
│  └─ desktop/
│     └─ Pidss.Desktop.Winforms/ # WinForms — future desktop client
├─ data_storage/
│  ├─ feature_store/            # Feature Store (data artifacts)
│  ├─ calibration_store/        # Calibration Profile Store (model artifacts)
│  └─ model_store/              # ML Model Store (future)
├─ artifacts/                   # Run artifacts (append-only, gitignored except .gitkeep)
└─ docs/                        # Architecture and project-level documentation
```

---

## 2. Naming Conventions

### General

| Context | Convention | Example |
|---|---|---|
| C# namespaces | PascalCase | `Pidss.Platform` |
| C# files | PascalCase | `ScenarioService.cs` |
| C++ files | PascalCase | `SimulatorEngine.cpp` |
| Python files | snake_case | `kpi_aggregator.py` |
| JSON schema files | kebab-case + `.schema.json` | `scenario.v1.schema.json` |
| JSON contract files | kebab-case + version + `.example.json` | `scenario.v1.example.json` |
| JSON artifact files | snake_case | `simulation_result.json` |
| CSV artifact files | snake_case | `production_records.csv` |
| Documentation files | UPPER_SNAKE_CASE | `REPOSITORY_CONVENTIONS.md` |
| SQL migration files | `NNNN_description.sql` | `0001_create_runs_table.sql` |

### Versioning in File Names

Public schemas and contracts include a version segment:

```
data/schemas/scenario.v1.schema.json
data/schemas/scenario.v2.schema.json
data/contracts/scenario.v1.example.json
data/contracts/scenario.v2.example.json
```

Canonical (engine-facing) files do NOT use a version segment — they are always the current stable model:

```
canonical_scenario.json
```

---

## 3. Branch and Commit Conventions

### Branch Naming

```
main                    # Stable, releasable
develop                 # Integration branch
feature/<phase>/<name>  # e.g., feature/phase0/conventions
fix/<issue>             # e.g., fix/schema-validation-null
```

### Commit Message Format

```
<type>(<scope>): <short description>

Types: feat | fix | docs | refactor | test | chore | build
Scope: platform | simulator | analytics | ui | data | db | docs | data_platform

Examples:
feat(data): add scenario.v1.schema.json
fix(platform): handle null cycle_time in adapter
docs(data): add VERSIONING_POLICY.md
chore(db): add migration 0001_create_runs_table
```

---

## 4. Folder Ownership and Responsibility

| Folder | Owner | Rule |
|---|---|---|
| `data/` | Data Platform | No business logic. Governance only. |
| `data/contracts/` | Data Platform | Versioned example payloads only. No logic. |
| `data/schemas/` | Data Platform | JSON Schema only. Draft-07. |
| `data/validation/` | Data Platform | Validation scripts/tests. No adapters. |
| `data/transforms/` | Data Platform | Transform definitions only. |
| `data/lineage/` | Data Platform | Policy docs and run metadata conventions. |
| `data/documentation/` | Data Platform | Domain model docs, data dictionary. |
| `platform/` | Platform Team | Orchestration, adapters, validation, API. |
| `engines/simulation/` | Simulation Team | C++ CLI engine. Canonical input only. |
| `engines/analytics/` | Analytics Team | Python CLI engine. Canonical input only. |
| `engines/optimization/` | Analytics Team | Python CLI engine. Canonical input only. |
| `data_platform/` | Data Platform | Offline data pipeline. No runtime execution. |
| `data_storage/` | Data Platform (runtime) | Feature store, calibration store. Read by Platform via DataSources. |
| `presentation/` | UI Team | API clients only. No business logic. No direct DB. |
| `artifacts/` | Platform (runtime) | Append-only. Never manually modified. |
| `docs/` | All Teams | Architecture docs. No code. |

---

## 5. Key Design Rules

1. **Adapter logic belongs only in `platform/`** — never in engines, data_platform, or UI.
2. **Engines (`engines/simulation/`, `engines/analytics/`, `engines/optimization/`) consume canonical format only** — no version branching, no public schema parsing.
3. **Data Platform (`data_platform/`) runs before the execution pipeline** — it never consumes `canonical_scenario.json`.
4. **Artifacts are append-only** — never overwrite run artifacts.
5. **Public schemas may evolve** — backward compatibility is managed by the adapter layer.
6. **Canonical model is stable** — it is the contract between Platform and Engines.
7. **No MES logic** — no WIP tracking, no dispatching, no real-time control.
8. **UI calls Platform API only** — never accesses DB or artifacts directly.
9. **DataSources are read-only** — no transformation or business logic in the data access layer.
10. **`data_storage/` is separate from `data/`** — `data/` is governance; `data_storage/` is runtime data produced by Data Platform.
