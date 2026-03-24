# Production Intelligence & Decision Support System (PIDSS)

## Phase 3 — Database & Run Metadata

### Target: 
Support pipeline execution tracking + reproducibility.

### Design:

#### Tables:

- `runs`
- `jobs`
- `run_metrics`
- `run_recommendations`
- `run_artifacts`

#### Include:

- Status fields + timestamps
- Job-level status tracking
- Artifact indexing fields:
	- type
	- path
	- created_at

#### Add:

Artifact Manifest
```
artifact_manifest.json
```
Contains:

- run_id
- artifact list
- timestamps
- engine versions

#### Ensure:

- No business domain duplication in DB, metadata only
- Artifacts remain source of truth

---
