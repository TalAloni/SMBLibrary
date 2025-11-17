---
trigger: model_decision
---

# Rules — Architecture (Python) — Not Applicable

This repository does not contain Python services. If Python utilities are introduced later (e.g., tooling scripts), prefer:

- Standalone CLI scripts with clear entry points; no cross‑import into C# core.
- Typed code (PEP 484) and minimal dependencies.
- Tests under `python/tests/` with `pytest` if a subproject is added.

Otherwise, ignore Python‑specific workflows/rules for this project.
