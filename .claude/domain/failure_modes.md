# Production Intelligence & Decision Support System (PIDSS)

# SYSTEM FAILURE MODES

PIDSS must detect and analyze system-level automation failure modes that frequently occur in real manufacturing environments.

The purpose is to prevent local optimization (machine-level) that harms system-level performance.

Failure detection is performed by:

- C++ Simulator → generating raw metrics
- Python Analytics → detecting failure patterns
- Recommender → generating corrective actions

The following 10 failure modes MUST be supported in PIDSS v1.

## 1. Downstream Blocking

Automation increases upstream output beyond downstream capacity.

Effects:

- Buffer WIP increases
- Upstream machine becomes blocked
- Effective utilization drops

Detected using:

- blocking_time
- downstream utilization
- WIP accumulation rate

Possible recommendations:

- increase downstream capacity
- reduce auto batch size
- allow partial batch transfer
- add intermediate buffer

## 2. Upstream Starvation

Automated machine requires high input rate but upstream cannot supply enough material.

Effects:

- auto machine idle time
- utilization below expected level

Detected using:

- starvation_time
- upstream utilization near 100%
- auto utilization below threshold

Possible recommendations:

- increase upstream machines
- reduce auto batch size
- redesign feeding logic

## 3. Batch Size Mismatch

Batch sizes between stages are incompatible.

Example:

```
auto stage batch = 3000
downstream stage batch = 600
```

Effects:

- transfer delay
- uneven WIP accumulation
- flow instability

Possible recommendations:

- harmonize batch sizes
- split batches
- change auto machine batch policy

## 4. Bottleneck Migration

Automation removes an existing bottleneck but creates a new one downstream.

Effects:

- throughput increase smaller than expected
- system bottleneck shifts to another stage

Detected using:

- bottleneck stage change
- marginal throughput gain analysis

Possible recommendations:

- reinforce new bottleneck stage
- multi-stage automation

## 5. WIP Explosion (Lead Time Increase)

Flow imbalance causes uncontrolled WIP growth.

According to Little’s Law:

```
Lead Time ≈ WIP / Throughput
```

Effects:

- production lead time increases
- inventory cost increases

Detected using:

- WIP accumulation
- WIP / throughput ratio

Possible recommendations:

- rebalance stage capacity
- reduce batch sizes
- introduce intermediate buffers

## 6. Reliability Dominance

Highly automated machines may have lower reliability.

Effects:

- downtime dominates system throughput
- production volatility increases

Detected using:

- availability analysis
- MTBF / MTTR impact simulation

Possible recommendations:

- redundancy machines
- preventive maintenance strategy
- hybrid manual fallback

## 7. Single Point of Failure

Integrated automated cell covers multiple stages.

If the cell fails:

- multiple SOP stages stop simultaneously
- system resilience decreases

Detected using:

- stage coverage by single unit
- lack of redundancy

Possible recommendations:

- parallel automation cells
- maintain legacy backup equipment

## 8. Footprint Constraint Violation

Automation scenario exceeds factory space limit.

Detected using:

- total_footprint_m2 > factory_footprint_limit

Possible recommendations:

- retire legacy machines
- replace multiple benches with integrated cell
- optimize layout

## 9. Labor Utilization Imbalance

Automation changes labor requirements unevenly across stages.

Effects:

- operator idle time
- operator overload in other stages

Detected using:

- operator utilization variance
- staffing imbalance

Possible recommendations:

- reallocate operators
- cross-skill training
- rebalance staffing levels

## 10. ROI Illusion

Automation increases capacity beyond actual demand.

Effects:

- low equipment utilization
- long payback period

Detected using:

```
system_capacity >> demand
```

Possible recommendations:

- postpone investment
- smaller automation cell
- phased deployment strategy

---