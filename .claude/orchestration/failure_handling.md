# Production Intelligence & Decision Support System (PIDSS)

## Failure Handling Model

| Stage | Failure Behavior |
| --- | --- |
| Validation | Stop immediately |
| ScenarioBuilder | Fail run |
| Adapter | Fail run |
| Simulation | Stop pipeline |
| Analytics	| Mark partial failure |