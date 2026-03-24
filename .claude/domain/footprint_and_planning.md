# Production Intelligence & Decision Support System (PIDSS)

## 1. Production Footprint Model

Factory footprint is a hard constraint in many manufacturing environments.

PIDSS must evaluate automation scenarios not only by throughput and ROI, but also by production floor utilization.

Production footprint includes:

```
Total Production Area =
    (Machine Area + WIP Buffer Area) × Layout Factor
```

Where:

### 1.1. Machine Area
```
Machine Area =
    Σ(machine_count × machine_footprint_m2)
```

This typically represents 60–80% of production floor area.

### 1.2. WIP Buffer Area

WIP buffers store intermediate products between stages.

Although smaller than machine area, WIP buffers may reach:

```
10–30% of production floor area
```

when batch sizes or flow imbalance increase.

WIP buffer area is calculated as:

```
WIP_area_stage =
    WIP_stage × unit_buffer_area
```

Where:

- `WIP_stage` = average units waiting between stages
- `unit_buffer_area` = storage footprint per unit

Total WIP area:
```
Total_WIP_area = Σ WIP_area_stage
```

### 1.3. Layout Factor

Factory layouts require space for:

- aisles
- operator movement
- maintenance access

Therefore a layout multiplier is applied:

```
Layout Factor = 1.2 – 1.4
```

Final production footprint:

```
Production_Footprint =
    (Machine_Area + WIP_Area) × Layout_Factor
```

## 2. Planning Model

Demand defined by:

- PlanningPeriod:
	- start_time
	- end_time
	- target_output_qty

- PlanningCalendar:

	- Shifts
	- Break definitions
	- Working days

- Break behavior:

	- manual stops during break
	- semi_auto stops during break
	- auto may continue if requires_operator_presence=false