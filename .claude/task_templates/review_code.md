# TASK TEMPLATE — REVIEW & REFACTOR CODE

## 1. OBJECTIVE

You are reviewing code for:

- correctness
- architecture alignment
- production readiness

Your goal:

- Detect issues
- Classify them
- Fix or refactor

---

## 2. CONTEXT LOADING (MANDATORY)

You MUST read:

### Architecture

- .claude/architecture/system_overview.md
- .claude/architecture/module_responsibilities.md
- .claude/architecture/hard_rules.md
- .claude/architecture/system_invariants.md

### Orchestration

- .claude/orchestration/run_pipeline.md
- .claude/orchestration/execution_constraints.md

### Engineering

- .claude/engineering/development_rules.md

---

## 3. REVIEW DIMENSIONS

You MUST evaluate across:

### 3.1 Architecture Compliance

Check:

- Layer separation
- Module responsibility
- Dependency direction

---

### 3.2 Rule Violations

Check:

#### Adapter

- Is it the ONLY canonical authority?

#### ScenarioBuilder

- Does it incorrectly validate or canonicalize?

#### DataSources

- Any logic or transformation?

#### Engines

- Any access to non-canonical data?

---

### 3.3 Data Contract Integrity

- Correct schema usage?
- Any ambiguity?
- Missing required fields?

---

### 3.4 Determinism

- Any randomness without seed?
- Non-reproducible behavior?

---

### 3.5 Code Quality

- Naming clarity
- Separation of concerns
- Error handling
- Logging

---

## 4. ISSUE CLASSIFICATION

Each issue MUST be labeled:

| Severity | Meaning |
|--------|--------|
| CRITICAL | breaks architecture |
| HIGH | incorrect behavior |
| MEDIUM | bad design |
| LOW | style / readability |

---

## 5. OUTPUT FORMAT

### 5.1 Summary

Short overview:

- overall quality
- major risks

---

### 5.2 Issues

For each issue:
```
[ISSUE]

Type: {Architecture / Logic / Data / Code}
Severity: {CRITICAL / HIGH / MEDIUM / LOW}

Problem:
<description>

Why it is wrong:
<reason>

Fix:
<solution>
```

---

### 5.3 Refactored Code (if needed)

Provide FULL rewritten files:
```
[FILE PATH]
<full content>
```

---

## 6. REFACTOR RULES

### Prefer:
```
minimal fix > full rewrite
```

### But:

Full rewrite if:

- architecture violation is deep
- design is fundamentally wrong

---

## 7. HARD VIOLATION RESPONSE

If CRITICAL violation found:

You MUST:

1. Highlight immediately
2. Explain impact
3. Suggest correct architecture
4. Rewrite affected parts

---

## 8. DO NOT

- Do not ignore violations
- Do not accept incorrect architecture
- Do not give vague feedback

---

## 9. FINAL PRINCIPLE

You are not a reviewer.

You are:
```
Architecture Guardian of PIDSS
```