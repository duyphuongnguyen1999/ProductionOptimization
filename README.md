# Production Intelligence & Decision Support System (PIDSS)

<p align="right">
  🇺🇸 <a href="README.md">English</a>
  | 🇻🇳 <a href="README_VI.md">Tiếng Việt</a>
</p>

## 1. Overview

**Production Intelligence & Decision Support System (PIDSS)** is a digital platform designed to support **manufacturing capacity growth under real-world constraints** through data-driven analysis, simulation, and decision support.

The system helps manufacturing teams **identify performance bottlenecks, evaluate optimization strategies (including automation and stage integration), and make informed decisions** to increase production capacity **without increasing labor or factory footprint**.

PIDSS sits **above existing MES/ERP systems** and focuses on **evaluation, comparison, and recommendation**, not execution.

> **PIDSS is not an MES, not a scheduling system, and not a real-time control system.**  
> It is a **decision support and production intelligence layer** for planners, managers, and digital/process engineers.

---

## 2. Business Context

Manufacturing organizations increasingly face strategic growth targets such as:

> **Increase overall production capacity by 40–60% within 3–5 years,  
> without increasing headcount or factory area.**

At the same time, they operate under strict constraints:

- Limited factory space
- Difficulty in hiring and retaining labor
- Rising operational costs
- High-risk, high-value automation investments ($200K–$1M)

In practice, decisions related to optimization and automation are often based on:
- Fragmented spreadsheets
- Local experience and intuition
- Inconsistent assumptions
- Limited ability to compare alternatives objectively

This leads to **high investment risk and low decision confidence**.

---

## 3. Core Business Problem

PIDSS addresses the following fundamental question:

> **How can manufacturers evaluate and compare production optimization and automation options  
> before committing capital, under real operational constraints?**

Key business questions include:

- Where are the true bottlenecks (line / stage / flow)?
- Which loss drivers dominate (labor, downtime, quality, imbalance)?
- How will KPIs change under different improvement strategies?
- Is a $500K automation investment justified?
- What is the expected ROI and payback period?
- Which option delivers the best trade-off between cost, capacity, and risk?

---

## 4. What PIDSS Does

PIDSS enables manufacturing teams to:

- Collect and normalize observed or simulated production data
- Quantify and rank performance of stages and production lines
- Identify bottlenecks and dominant loss drivers
- Simulate **what-if optimization scenarios** at an aggregate, decision-support level
- Evaluate strategies such as:
  - Manual process optimization
  - Semi-automation
  - Full automation and stage integration
- Perform **A/B comparisons** between baseline and candidate scenarios
- Generate explainable recommendations with quantified impact
- Maintain a **run-based, auditable, and reproducible history** of decisions

---

## 5. What PIDSS Explicitly Does NOT Do

To maintain a clear system boundary and avoid scope creep, PIDSS does **not**:

- Dispatch work orders or assign tasks to operators
- Track WIP movement or detailed product routing
- Perform real-time scheduling or machine control
- Design, program, or control automation equipment
- Replace MES, ERP, SCADA, or PLC systems

> **PIDSS supports decision-making, not execution.**

---

## 6. Target Users

### Production / Plant Managers
- Monitor performance and KPIs
- Prioritize optimization and automation initiatives
- Evaluate cost, capacity, and investment trade-offs

### Production Planners
- Assess capacity versus demand
- Compare production strategies and configuration changes

### Process / Digital Engineers
- Analyze cycle time, downtime, and defect data
- Design and validate optimization and automation scenarios
- Support OE, Lean, and Kaizen initiatives

### Non-Target Users

- Operators
- Real-time execution systems
- PLC and automation controllers

---

## 7. Modeling & Design Philosophy

- **Decision-Centric**: Focus on strategic and tactical decisions, not execution
- **Aggregate Modeling**: Sufficient fidelity for evaluation without MES-level complexity
- **Automation-Aware**: Automation is evaluated as a strategic option, not implemented
- **Explainable**: Clear metrics and rationale behind recommendations
- **Human-in-the-Loop**: Final decisions remain with people
- **Run-Based & Auditable**: Every analysis is reproducible and traceable

---

## 8. Business Value

> **PIDSS helps manufacturers evaluate $500K automation investments *before spending***  
> by quantifying expected ROI and payback periods, rather than relying on rough estimates.

Key value delivered:
- Reduced investment risk
- Data-driven CAPEX decision support
- Consistent comparison of optimization and automation strategies
- Auditable justification for strategic decisions
- Institutionalization of OE and automation knowledge

---

## 9. Technical Scope (High-Level)

| Area | Technology |
| --- | --- |
| Backend Platform | ASP.NET Core (.NET 8) |
| Simulation Engine | C++ (aggregate digital twin) |
| Analytics & Optimization | Python |
| Database | SQL Server or PostgreSQL |
| UI Client | WinForms (.NET) |
| Data Contracts | JSON + JSON Schema |
| Architecture | Run-based, append-only, versioned |

---

## 10. Core Concepts

### Scenario

A **Scenario** describes a hypothetical production strategy, including:

- Line and stage configuration
- Staffing policies
- Automation and integration assumptions
- Capacity, footprint, and cost parameters
- Random seed (reproducibility)
- `schema_version` (compatibility)

### Run

A **Run** is one execution of a scenario:

- Uniquely identified by `run_id` (UUID)
- **Append-only** (results are never overwritten)
- Produces artifacts (datasets, logs, reports)
- Stores KPIs and recommendations

---

## 11. High-Level Architecture

```text
Machines / PLC / MES Export (Observed Data)
            │
            ▼
Data Ingestion & Normalization
            │
            ▼
Platform Backend (.NET)
  - Scenario & Run Management
  - Audit & Versioning
  - Job Orchestration
            │
            ├── Simulation Engine (C++)
            ├── Analytics & Optimizer (Python)
            │
            ▼
KPIs & Recommendations (Database)
            │
            ▼
GUI Client (WinForms)
```

---

## 12. Repository Structure
```text
Production-Optimization/
├─ core_spec/                 # JSON contracts & schemas (single source of truth)
├─ platform_dotnet/           # ASP.NET Core backend (API, orchestration)
│   └─ Pidss.Platform.Api/
├─ ui_client/                 # WinForms client application
│   └─ Pidss.UiClient/
├─ simulator_cpp/             # C++ simulation engine (CLI)
│   └─ Pidss.Simulator.Cli/
├─ ai_py/                     # Python analytics & optimizer
├─ artifacts/                 # artifacts/{run_id}/... (append-only)
└─ docs/                      # Architecture & design documentation
```

---

## 13. Roadmap

- Phase 0: Repository foundation & conventions
- Phase 1: CoreSpec contracts and schemas
- Phase 2: Database (runs, jobs, metrics, recommendations)
- Phase 3: Platform backend orchestration
- Phase 4: C++ simulation v1 (aggregate digital twin)
- Phase 5: Python KPI & ROI analytics v1
- Phase 6: WinForms dashboard MVP
- Phase 7: Optimization & automation strategy evaluation
- Phase 8: Integration-ready (mock MES import)

---

## 14. License

MIT License

