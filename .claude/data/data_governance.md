# Production Intelligence & Decision Support System (PIDSS)

## Data Governance

### 1. Purpose

Data governance is centered around `data/` folder, which contains all versioned contracts, 
schemas, validation logic, transformation scripts, lineage policies, and documentation.

`data/` = governance layer, NOT runtime storage

```
data/ defines HOW data should look
data_storage/ contains ACTUAL data artifacts
```

### 2. Definitions:

| Folder | Purpose |
| --- | --- |
| contracts | Example scenarios & outputs |
| schemas | Validation rules (Draft-07) |
| validation | Schema + semantic validation |
| transforms | Offline transformations (NOT runtime) |
| lineage | Artifact tracking policies |
| documentation | Domain explanation |
