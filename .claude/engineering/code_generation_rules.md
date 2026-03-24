# Production Intelligence & Decision Support System (PIDSS)

## Code Generation Rules (Critical for AI)

When generating code:

- Each file in the system MUST be produced as a separate artifact.
- Never merge multiple source files into a single output block.
- For every file generated:
	- Output the exact relative file path.
	- Output the full file content.
	- Do not truncate content.
- Do not generate placeholder files unless explicitly requested.
- Do not generate unrelated files outside the defined repository structure.

This ensures clean integration into the Visual Studio solution and preserves repository consistency.

