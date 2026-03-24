# Production Intelligence & Decision Support System (PIDSS)

## 1. Stage-Centric SOP Identity

- Stage = SOP step
- Stage NEVER deleted
- Stage NEVER converted
- Stage contains NO execution logic
- Stage only defines:
	- `stage_id`
	- `order`
	- `name`

## 2. Equipment-Centric Execution (Core Design Principle)

Execution is defined by Work Units (Equipment Units).

Rules:

- `covered_stage_ids` is mandatory
- Single-stage = one element array
- Multi-stage = integrated execution
- `stage_id` field is NOT used

Automation level (manual/semi_auto/auto) is independent from integration scope.

## 3. Stage Attribution (Integrated Units)

When a WorkUnit covers multiple stages:

- Platform Adapter must compute `stage_weights`
- `stage_weights` must sum to 1
- Engines must not compute attribution logic

Attribution ensures:

- Bottleneck reporting per stage
- A/B comparability
- Traceability preservation
