---
trigger: model_decision
---

# Rules — Test-Driven Development

- Red → Green → Refactor. Write a failing test, implement minimally, then refactor safely.
- Outside-in for feature slices; mock ports, not behavior. Keep tests independent and fast.
- Commit after green; keep refactor-only commits separate from behavior changes.
- Use property/fuzz testing selectively for critical invariants.

References: xUnit/pytest docs; TDD literature.
