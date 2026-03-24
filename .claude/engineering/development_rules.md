# Production Intelligence & Decision Support System (PIDSS)

## DEVELOPMENT RULES (Refined)

### Architecture Rules
- ScenarioBuilder MUST be interface-based
- DataSources MUST be pluggable
- Adapter MUST be isolated in Platform
- Engines MUST be stateless (per run)

### Build Rules
- Use Visual Studio solution (.sln)
- C++ via .vcxproj ONLY
- Python via CLI ONLY
- No CMake unless explicitly required

### Data Rules
- Artifacts are append-only
- No overwrite allowed
- Canonical is immutable
- Snapshot is immutable

### Documentation Rules
- Markdown only
- Provide bilingual documentation:
	- English → FILE_NAME.md
	- Vietnamese → FILE_NAME_VI.md
- Include header:
```
<p align="right">
  🇺🇸 <a href="FILE_NAME.md">English</a>
  | 🇻🇳 <a href="FILE_NAME.md">Tiếng Việt</a>
</p>
```