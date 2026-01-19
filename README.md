# Production Intelligence & Decision Support System (PIDSS)

## 1. Overview

**Production Intelligence & Decision Support System (PIDSS)** is a digital platform designed to support **manufacturing capacity growth under real-world constraints** through data-driven analysis, simulation, and decision support.

The system helps manufacturing teams **identify performance bottlenecks, evaluate optimization strategies (including automation and stage integration), and make informed decisions** to increase production capacity **without increasing labor or factory footprint**.

PIDSS sits **above MES/ERP systems** and focuses on **evaluation, comparison, and recommendation**, not execution.

> **PIDSS is not an MES, not a scheduling system, and not a real-time control system.**  
> It is a **decision support and intelligence layer** for planners, managers, and digital/process engineers.

---

## 2. Business Problem

Manufacturing organizations often face long-term capacity growth targets under strict constraints:

- Limited factory space
- Difficulty in hiring additional labor
- Increasing demand and cost pressure

A typical strategic goal is:

> **Increase overall production capacity by 50% within 5 years,  
> without increasing headcount or factory area.**

Achieving this goal requires more than incremental improvements. Organizations must evaluate **multiple optimization strategies**, such as:

- Process optimization and cycle time balancing
- Reduction of downtime and quality losses
- **Automation and integration of multiple production stages**
- Reduction of material handling, waiting time, and footprint

These decisions are high-impact, capital-intensive, and difficult to reverse — making **decision support and quantitative evaluation essential**.

PIDSS is designed to address this exact class of problems.

---

## 3. What PIDSS Does

PIDSS enables manufacturing teams to:

- Collect and normalize production performance data from multiple sources
- Quantify and rank performance of stages and production lines
- Identify bottlenecks and dominant loss drivers
- Simulate **what-if optimization scenarios** at an aggregate, decision-support level
- Evaluate and compare strategies such as:
  - Manual optimization
  - Semi-automation
  - Full automation and stage integration
- Compare baseline vs improved scenarios (A/B comparison)
- Generate actionable recommendations with expected impact and rationale
- Maintain an **auditable, reproducible history** of decisions and outcomes

---

## 4. What PIDSS Explicitly Does NOT Do

To maintain a clear boundary and avoid scope creep, PIDSS does **not**:

- Dispatch work orders or assign tasks to operators
- Track WIP movement or detailed product routing
- Perform real-time scheduling or machine control
- Design, program, or control automation equipment
- Replace MES, ERP, SCADA, or PLC systems

> **PIDSS supports decision-making, not execution.**

---

## 5. Target Users

### Primary Users

#### Production / Plant Managers

- Monitor performance and KPIs
- Prioritize improvement and automation initiatives
- Evaluate cost, capacity, and risk trade-offs

#### Production Planners

- Assess capacity vs demand
- Evaluate production strategies and configuration changes

#### Process / Digital Engineers

- Analyze cycle time, downtime, and defect data
- Design and validate optimization and automation scenarios
- Support OE, Lean, and Kaizen initiatives

### Non-Target Users

- Operators
- Automation execution systems
- Real-time control systems

---

## 6. Core Use Cases

### 1. Performance Scoring & Bottleneck Identification

- Rank stages and production lines by efficiency
- Identify dominant loss drivers (labor, downtime, quality, imbalance)

### 2. Cycle Time & Flow Imbalance Analysis

- Analyze cycle time distribution and variance
- Detect imbalance and flow inefficiencies across stages or stations

### 3. What-if Optimization & Automation Simulation

- Evaluate impact of:
  - Cycle time balancing
  - Downtime or defect reduction
  - Automation and stage integration
- Estimate throughput, labor productivity, footprint reduction, and cost impact

### 4. Recommendation Generation

- Propose optimization or automation strategies
- Provide expected gains, constraints, and rationale
- Support capital investment and roadmap decisions

### 5. A/B Comparison & OE Validation

- Compare baseline vs improved scenarios
- Quantify labor savings, capacity gains, and cost improvements
- Maintain audit trail for OE and automation initiatives

---

## 7. Design Philosophy

- **Decision-Centric**: Focus on strategic and tactical decisions, not execution
- **Aggregate Modeling**: Sufficient fidelity for evaluating strategies without MES-level complexity
- **Automation-Aware**: Automation is evaluated as a strategic option, not implemented by the system
- **Explainable**: Clear metrics and rationale behind recommendations
- **Human-in-the-Loop**: Final decisions remain with people
- **Run-Based & Auditable**: Every analysis is reproducible and traceable

---

## 8. Technical Scope (High-Level)

| Area | Technology |
| --- | --- |
| Backend Platform | ASP.NET Core (C#) |
| Simulation Engine | C++ (aggregate digital twin) |
| Analytics & Optimization | Python |
| Database | SQL Server or PostgreSQL |
| UI | WinForms or Blazor (MVP) |
| Data Contracts | JSON + JSON Schema |
| Architecture | Run-based, append-only, versioned |

---

## 9. Why This Project Exists

Traditional MES systems answer:

> **“What happened?” / “What is happening now?”**

PIDSS answers:

> **“Which optimization or automation strategy should we choose,  
> and how much value will it deliver under our constraints?”**

The platform enables:

- Scenario-based **what-if evaluation**
- KPI-driven bottleneck discovery
- Strategy comparison (manual vs automation)
- Decision-ready recommendations
- Run-based audit and reproducibility

---

## 10. Core Concepts

### Scenario

A **Scenario** describes a hypothetical production strategy, including:

- Line and stage configuration
- Staffing policies
- Automation or integration assumptions
- Capacity, footprint, and cost parameters
- Random seed (reproducibility)
- `schema_version` (compatibility)

### Run

A **Run** is one execution of a scenario:

- Uniquely identified by `run_id` (UUID)
- **Append-only** (results are never overwritten)
- Produces artifacts (datasets, logs, reports)
- Stores metrics and recommendations

---

## 11. High-Level Architecture

```text
Machines / PLC / MES Export (Observed Data)
            │
            ▼
Data Ingestion & Normalization
            │
            ▼
Platform Backend (ASP.NET Core)
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
GUI (Winform or Blazor)
```

---

## 12. Repository Structure

```graphql
Production-Optimization/
├─ core_spec/            # JSON contracts & schemas (single source of truth)
├─ ui_client/            
|   ├─ Forms/      
|   ├─ Services/         
|   ├─ Models/         
|   ├─ Services/         
|   ├─ Program.cs         
|   ├─ Pidss.UiClient.csproj         
├─ simulator_cpp/        # C++ simulation engine (CLI)
├─ ai_py/                # Python analytics & optimizer
├─ artifacts/            # artifacts/{run_id}/... (append-only)
└─ docs/
```

---

## 13. Roadmap

- Phase 0: Repository foundation & conventions
- Phase 1: CoreSpec contracts and schemas
- Phase 2: Database (runs, jobs, metrics, recommendations)
- Phase 3: Platform backend orchestration
- Phase 4: C++ simulation v1 (aggregate digital twin)
- Phase 5: Python KPI analytics v1
- Phase 6: Blazor dashboard MVP
- Phase 7: Optimization & automation strategy evaluation
- Phase 8: Integration-ready (mock MES import)

---

## 14. License

MIT License
