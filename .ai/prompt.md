Q1: Về việc ScenarioBuilder chỉ được đọc:
- Aggregated / curated features
- Pre-computed metrics (throughput, yield, etc.)
- Static structures (BOM, process graph) 

KHÔNG được: 
- fit model 
- estimate distribution
- infer parameters 

Tất cả những ràng buộc này sẽ được giải quyết nếu quy định ScenarioBuilder chỉ được phép đọc thông qua abtract layer DataSources đúng không

Q2 — Giữ DataSources
Chốt Rule bắt buộc thêm (hiện chưa có trong prompt):

```DataSources MUST NOT:
- call external API directly
- contain business logic
- perform transformation

DataSources ONLY:
- read from storage
- map to DTO
```

Q3 — Calibration Store thuộc Data Platform → CẦN CÂN NHẮC LẠI

Về việc: 
- Vẫn giữ folder như tôi
- Nhưng về kiến trúc phải ghi rõ:

```
Calibration Store is NOT a data artifact
It is a model artifact produced by Calibration Engine
```

Có cách thiết kế nào tối ưu, phân biệt rõ ràng cả physical storage và logical artifact không?

Q4 — Adapter vs ScenarioBuilder → HIỆN TẠI BỊ SAI (quan trọng nhất)

Chốt sẽ sử dụng pipeline này

```
User Input
   ↓
ScenarioBuilder
   ↓
Scenario Snapshot (public-like, enriched)
   ↓
Adapter
   ↓
Canonical Scenario (ONLY HERE)
```
Vai trò rõ:
ScenarioBuilder
- merge:
	- user input
	- feature store
	- calibration profile
- output:
`enriched_scenario.json (still public schema compliant)`

Adapter
- ONLY place:
	- schema validation
	- versioning
	- canonicalization
	- stage_weights

Rule phải thêm:
```
ScenarioBuilder MUST NOT produce canonical model
Adapter is the ONLY component allowed to produce canonical_scenario.json
```

Q5 — Giữ FeatureEngineering --> Chốt 

Q6 — Synthetic.Mes.Api --> Chốt