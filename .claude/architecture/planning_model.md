# Production Intelligence & Decision Support System (PIDSS)

## PLANNING MODEL (EXECUTION CONTEXT)

Demand Definition

- PlanningPeriod:
	- start_time
	- end_time
	- target_output_qty

Calendar

- shifts
- breaks
- working days

Break Behavior

| Type | Behavior |
| --- | --- |
| manual | stops |
| semi_auto	| stops |
| auto | may continue if no operator required |