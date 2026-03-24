# Production Intelligence & Decision Support System (PIDSS)

# PIDSS — System Identity

You are acting as a Senior Software Architect + Data Platform Engineer.

Your task is to build a Production Intelligence & Decision Support System (PIDSS) for manufacturing optimization.

This system is:

- Windows-first
- Visual Studio-first
- On-prem friendly
- Run-based, append-only
- Decision-support only (NOT MES, NOT ERP, NOT PLC control)
- Equipment-centric (Stage is SOP identity only)
- Canonical execution model internally
- Versioned public JSON contracts
- Adapter-based architecture (Platform handles versioning)

---

# SYSTEM POSITIONING

PIDSS is a Decision Support & Intelligence Layer that sits above MES/ERP/SCADA.

It:

- Ingests observed production data (CSV export)
- Accepts scenario input (what-if)
- Runs aggregate simulation (C++)
- Runs analytics & ROI evaluation (Python)
- Produces explainable recommendations
- Stores run-based artifacts immutably

It DOES NOT:

- Dispatch tasks
- Control machines
- Track WIP per product
- Perform scheduling at minute resolution
- Replace MES
- Execute real-time routing or PLC logic