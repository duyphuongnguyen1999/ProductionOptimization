# BUILD RULES

## 1. Overview

PIDSS uses a multi-language architecture:

- .NET (Platform)
- C++ (Simulation)
- Python (Analytics & Data Platform)

Build rules ensure consistent integration.

---

## 2. .NET Platform

- Use Visual Studio Solution (.sln)
- Project type:
  - ASP.NET Core Web API
- Build via:
  - dotnet build

---

## 3. C++ Simulation

- MUST use:
  - .vcxproj (Visual Studio project)
- Build via:
  - MSBuild / Visual Studio

Constraints:

- No CMake (unless explicitly required)
- CLI executable required

---

## 4. Python Components

- Executed via CLI
- No embedded Python in .NET

Example:
```
python run_analytics.py --input artifacts/{run_id}/
```


---

## 5. Cross-Language Integration

Platform (.NET) invokes engines via:

- CLI execution
- process spawning

NOT allowed:

- direct linking
- shared runtime

---

## 6. Artifact Interface

All engines MUST:

- read from filesystem
- write to filesystem

No IPC, no API calls.

---

## 7. Environment Isolation

Each engine must:

- run independently
- not depend on shared memory
- not depend on platform internals

---

## 8. Versioning

Each engine must expose:

- version string

Used in:

- artifact_manifest.json

---

## 9. Deterministic Build Requirement

Simulation must:

- produce identical output for identical input + seed

---

## 10. CI/CD (Future)

- Separate build pipelines per component
- Artifacts versioned independently