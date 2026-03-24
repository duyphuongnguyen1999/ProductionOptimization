# Production Intelligence & Decision Support System (PIDSS)

## 1. System Overview

PIDSS (Production Intelligence & Decision Support System) is:

- Decision-support only (NOT MES / ERP / PLC)
- Scenario-driven (NOT data-driven)
- Run-based, append-only
- Canonical execution model internally
- Versioned public JSON contracts
- Adapter-controlled architecture
- Equipment-centric simulation

### Core Philosophy

```
- Scenario represents system behavior
- Data represents observation
- Canonical Scenario = Single Source of Truth for ALL engines
```

### Key Implications

| Concept | Meaning |
| --- | --- |
| Data | Observed reality |
| Feature Store | Curated observation |
| Calibration Profile | Modeled system behavior |
| Scenario | Hypothetical decision space |
| Canonical Scenario | Executable truth |

---

## 2. System Modules (Refactored — Boundary Clean)

PIDSS consists of 3 main layers:

```
Data Platform → Platform → Execution Engines
```
