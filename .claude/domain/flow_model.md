# Production Intelligence & Decision Support System (PIDSS)

## Flow Model

PIDSS uses aggregate flow simulation, not discrete-event simulation.

The model includes:

- batch size policy
- stage capacity
- transfer delay
- reliability impact
- break behavior

Simulation must capture system-level flow dynamics while remaining computationally simple.

Key Principles

- No discrete-event queue simulation
- No per-product tracking
- No MES-level dispatching logic

However:

```
Aggregate WIP between stages MUST be modeled.
```

This allows detection of:

- blocking
- starvation
- WIP accumulation
- lead time increase

This enables system-level analysis using flow theory including Little's Law.

Little's Law relationship:

```
Lead Time ≈ WIP / Throughput
```

## WIP ESTIMATION MODEL

WIP is estimated at stage boundaries using aggregate flow metrics.

For each stage:

```
WIP_stage ≈ Throughput × Effective_Wait_Time
```

Effective wait time includes:

- batch gating delay
- transfer delay
- downstream congestion

This enables WIP estimation without discrete-event simulation.

## BATCH FLOW DYNAMICS

Batch policy strongly influences WIP and footprint.

Example:

Batch transfer:

```
batch = 600
```

Buffer range:

```
0 – 600
average ≈ 300
```

Large automation batches may cause:

```
batch_auto = 3000
batch_downstream = 600
```

This causes:

- large buffer accumulation
- unstable flow
- excessive footprint usage

PIDSS must detect and analyze this scenario.