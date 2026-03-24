# Production Intelligence & Decision Support System (PIDSS)

## Phase 6 — Python Analytics v1

### Target: 
Compute decision-support metrics.

### Implement:

#### 1. KPI Computation
- throughput
- lead time (via Little’s Law)
- WIP
- utilization

#### 2. Advanced Metrics
- footprint
- throughput_per_m2
- operator utilization

#### 3. Failure Mode Detection

10 system-level failure modes:

- blocking
- starvation
- batch mismatch
- bottleneck migration
- WIP explosion
- reliability dominance
- single point of failure
- footprint violation
- labor imbalance
- ROI illusion

### Scenario Comparison

#### Input:

- baseline_run_id
- candidate_run_id

#### Constraints:

- MUST use stored artifacts
- NO recomputation

#### Output

- `analysis_response.json`
- `recommendation.json`

---