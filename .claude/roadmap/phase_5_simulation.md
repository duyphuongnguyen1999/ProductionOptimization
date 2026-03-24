# Production Intelligence & Decision Support System (PIDSS)

## Phase 5 — C++ Simulation v1 (Aggregate Model)

### Target: 
Implement deterministic aggregate simulation.

### Implement:

- Canonical parser
- Capacity computation (equipment pool)
- Integrated cell handling
- Batch gating
- Transfer delay
- Break logic

### Output:
- `production_records.csv`
- `simulation_result.json`

### MUST Support
- Throughput
- Stage utilization
- Blocking / starvation
- WIP per stage
- Total WIP
- Bottleneck stage
- Machine area
- WIP area
- Production footprint

### Constraints
- No discrete-event simulation
- Must support:
	- WIP estimation
	- flow stability
	- footprint evaluation