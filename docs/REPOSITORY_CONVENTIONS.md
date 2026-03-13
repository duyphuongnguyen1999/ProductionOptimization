# PIDSS Repository Conventions

**Version:** 1.0.0  
**Phase:** 0 — Repository Foundation & Data-Layer Conventions  
**Status:** Active

---

## 1. Repository Structure

```
ProductionOptimization/
├─ data/
│  ├─ contracts/          # Example payloads (versioned input/output samples)
│  ├─ schemas/            # JSON Schema definitions (Draft-07)
│  ├─ validation/         # Validation logic, tests, and rules
│  ├─ transforms/         # Analytical transformation definitions
│  ├─ lineage/            # Artifact and run metadata policies
│  └─ documentation/      # Versioning docs, domain model, data dictionary
├─ platform_dotnet/
│  └─ Pidss.Platform.Api/ # ASP.NET Core Web API — orchestration, adapters, validation
├─ simulator_cpp/
│  └─ Pidss.Simulator.Cli/ # C++ CLI — aggregate simulation engine
├─ analytics/
│  └─ Pidss.Analytics.Cli/ # Python CLI — KPI computation and recommendation
├─ presentation/
│  └─ Pidss.Destop.Winforms/ # WinForms UI — decision-support interface
├─ artifacts/             # Run artifacts (append-only, gitignored except .gitkeep)
└─ docs/                  # Architecture and project-level documentation
```

---

## 2. Naming Conventions

### General

| Context | Convention | Example |
|---|---|---|
| C# namespaces | PascalCase | `Pidss.Platform.Api` |
| C# files | PascalCase | `ScenarioService.cs` |
| C++ files | PascalCase | `SimulatorEngine.cpp` |
| Python files | snake_case | `kpi_aggregator.py` |
| JSON schema files | kebab-case + `.schema.json` | `scenario.schema.json` |
| JSON contract files | kebab-case + `.example.json` | `scenario.v1.example.json` |
| JSON artifact files | snake_case | `simulation_result.json` |
| CSV artifact files | snake_case | `production_records.csv` |
| Documentation files | UPPER_SNAKE_CASE | `REPOSITORY_CONVENTIONS.md` |
| SQL migration files | `NNNN_description.sql` | `0001_create_runs_table.sql` |

### Versioning in File Names

Public schemas and contracts include a version segment:

```
scenario.v1.schema.json
scenario.v1.example.json
scenario.v2.schema.json
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
Scope: platform | simulator | analytics | ui | data | db | docs

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
| `data/contracts/` | Data Platform | Example payloads only. No logic. |
| `data/schemas/` | Data Platform | JSON Schema only. Draft-07. |
| `data/validation/` | Data Platform | Validation scripts/tests. No adapters. |
| `data/transforms/` | Data Platform | Transform definitions only. |
| `data/lineage/` | Data Platform | Policy docs and run metadata conventions. |
| `platform_dotnet/` | Platform Team | Orchestration, adapters, validation, API. |
| `simulator_cpp/` | Simulation Team | C++ CLI engine. Canonical input only. |
| `analytics/` | Analytics Team | Python CLI engine. Canonical input only. |
| `presentation/` | UI Team | WinForms. API calls only. No direct DB. |
| `artifacts/` | Platform (runtime) | Append-only. Never manually modified. |
| `docs/` | All Teams | Architecture docs. No code. |

---

## 5. Key Design Rules

1. **Adapter logic belongs only in `platform_dotnet/`** — never in engines or UI.
2. **Engines (`simulator_cpp/`, `analytics/`) consume canonical format only** — no version branching.
3. **Artifacts are append-only** — never overwrite run artifacts.
4. **Public schemas may evolve** — backward compatibility is managed by the adapter layer.
5. **Canonical model is stable** — it is the contract between Platform and Engines.
6. **No MES logic** — no WIP tracking, no dispatching, no real-time control.
7. **UI calls Platform API only** — never accesses DB or artifacts directly.
