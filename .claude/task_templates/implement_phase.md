# TASK TEMPLATE — IMPLEMENT PHASE

## 1. OBJECTIVE

You are implementing:

```
Phase: {PHASE_ID}
File: .claude/roadmap/{phase_file}.md
```

Your goal:

- Translate phase definition → actual implementation
- Ensure full alignment with architecture
- Produce production-ready code

---

## 2. CONTEXT LOADING (MANDATORY)

Before doing anything, you MUST read:

### Architecture

- .claude/architecture/system_overview.md
- .claude/architecture/module_responsibilities.md
- .claude/architecture/hard_rules.md
- .claude/architecture/system_invariants.md

### Orchestration

- .claude/orchestration/run_pipeline.md
- .claude/orchestration/execution_constraints.md
- .claude/orchestration/engine_contracts.md

### Domain (if relevant)

- .claude/domain/*

### Data (if relevant)

- .claude/data/canonical_model.md
- .claude/data/data_rules.md

### Engineering

- .claude/engineering/code_generation_rules.md
- .claude/engineering/development_rules.md

---

## 3. PHASE ANALYSIS

Extract from phase file:

### 3.1 Scope

- What MUST be built
- What is OUT OF SCOPE

### 3.2 Deliverables

List:

- modules
- services
- interfaces
- artifacts

### 3.3 Constraints

- architecture constraints
- engine constraints
- data constraints

---

## 4. SYSTEM MAPPING

Map requirements → actual system:

| Requirement | Layer | Module | File |
|------------|------|--------|------|

Example:

| ScenarioBuilder | Platform | ScenarioBuilder | platform/... |

---

## 5. DESIGN BEFORE CODE (MANDATORY)

You MUST define:

### 5.1 Interfaces

- Required interfaces
- Input/output contracts

### 5.2 Data Flow

- Input → processing → output

### 5.3 Dependency Boundaries

Ensure:

- No layer violation
- No cross-layer leakage

---

## 6. IMPLEMENTATION RULES

### MUST:

- Follow repository structure exactly
- Respect all hard rules
- Keep code deterministic
- Use clear naming
- Ensure separation of concerns

### MUST NOT:

- Mix responsibilities across layers
- Let engines access data platform
- Let ScenarioBuilder produce canonical
- Add hidden logic

---

## 7. CODE GENERATION FORMAT (STRICT)

For each file:
```
[FILE PATH]
<full content>
```


Rules:

- One file per block
- No truncation
- No placeholder
- No pseudo code

---

## 8. VALIDATION CHECKLIST (MANDATORY)

Before finishing, verify:

### Architecture

- [ ] No violation of hard_rules.md
- [ ] Boundaries respected
- [ ] Correct module ownership

### Orchestration

- [ ] Run pipeline alignment
- [ ] Correct execution order

### Data

- [ ] Canonical model respected
- [ ] No schema ambiguity

### Engineering

- [ ] Code is production-ready
- [ ] No debug artifacts
- [ ] Deterministic behavior

---

## 9. OUTPUT STRUCTURE

You MUST output in this order:

1. Phase summary (short)
2. Design summary
3. File list
4. Generated files

---

## 10. FAILURE MODE

If something is unclear:

- STOP
- Ask precise question

If architecture conflict detected:

- STOP
- Explain conflict
- Propose fix

---

## 11. FINAL RULE

You are NOT writing code.

You are:
```
Building a production-grade system aligned with strict architecture
```