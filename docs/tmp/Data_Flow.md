```
[Pidss.DataPlatform.Synthetic.Mes.Generator]						
	↓ - Generates synthetic MES database
MES Synthetic Database (SQL Server)
	↓
[Pidss.DataPlatform.Synthetic.Mes.Api]
	↓ - Exposes REST API for raw synthetic MES data access
[Pidss.DataPlatform.Ingestion] 
	↓ - Ingests raw MES data (synthetic or real) via API
[Pidss.DataPlatform.FeatureEngineering]
	↓ - Transforms raw MES data into engineered features for modeling
Feature Store (versioned)
	↓	
[Pidss.Calibration]
	↓ - 
Calibration Profile Store (versioned)
```