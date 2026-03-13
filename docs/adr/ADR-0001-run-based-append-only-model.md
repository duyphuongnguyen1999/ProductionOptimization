# ADR-0001: Run-Based, Append-Only Execution Model

**Status:** Accepted  
**Date:** Phase 0  
**Authors:** PIDSS Platform Team

---

## Context

PIDSS is a decision-support system. Its value depends on the ability to:
- Compare current and historical scenario evaluations
- Reproduce any prior analysis
- Audit the inputs and outputs of every decision recommendation

The system must also support multiple stakeholders reviewing past runs and trusting that results have not been altered.

---

## Decision

All scenario executions in PIDSS are organized as **immutable Runs**.

A Run:
- Has a unique `run_id` (UUID v4)
- Has a dedicated artifact directory at `artifacts/{run_id}/`
- Stores all inputs, canonical model, engine outputs, and logs in that directory
- Is **never modified after creation**

If a user wants to re-run a scenario (e.g., after fixing an error), a **new Run** is created with a new `run_id`. The original Run is preserved.

---

## Consequences

### Positive

- Full reproducibility: any run can be re-inspected or re-analyzed at any time
- Full auditability: every recommendation is traceable to its exact input
- Safe failure handling: failed runs leave all partial artifacts intact for diagnosis
- Simple consistency model: no update/delete paths to reason about

### Negative / Trade-offs

- Storage growth over time (mitigated by archival policy in future phases)
- Cannot "fix" a run in-place; must re-submit (acceptable given PIDSS is decision-support, not real-time)
- Requires artifact directory creation and management (handled by Platform)

---

## Alternatives Considered

### Mutable Run Results

Allow updating `simulation_result.json` and analytics outputs in-place if a run is re-processed.

**Rejected** because: it breaks auditability and reproducibility. It becomes impossible to know which version of a result a decision was based on.

### Database-Only Storage (No File Artifacts)

Store all outputs in relational DB tables instead of files.

**Rejected** because: JSON artifacts produced by engines are complex, deeply nested, and would require expensive schema migrations as models evolve. File-based artifacts are more resilient to model changes and easier to version.

---

## Related Decisions

- ADR-0002 (planned): Concurrency and FIFO queue for bounded parallel execution
- ADR-0003 (planned): Canonical model stability guarantee
