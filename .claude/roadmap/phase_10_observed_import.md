# Production Intelligence & Decision Support System (PIDSS)

## Phase 10 — Observed Import (Optional / Future Extension)

### Target: 
Bridge simulation with real production data.

### Implement:

Future MES integration:

- MUST be API-based
- MUST NOT connect directly to MES DB in production architecture
- Observed vs simulated KPI comparison
- Gap analysis report

File-based integration:

- CSV observed data import
- Normalization to internal format
- Observed vs simulated KPI comparison
- Gap analysis report

### Constraint
- NO direct MES integration
- MES only via API 