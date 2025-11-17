---
trigger: model_decision
---

# Rules — Unit Testing (Index)

- Use this index to jump to language-specific guidance. Keep unit tests fast, deterministic, and behavior-focused (AAA or Given–When–Then).

## Language-specific

- C# / xUnit: .windsurf/rules/rules-unit-testing-csharp.md
- Python / pytest: .windsurf/rules/rules-unit-testing-python.md

## General checklist

- One behavior per test; clear naming (scenario + expected).
- Avoid real I/O/network in unit tests; mock ports, not internals.
- Deterministic time/RNG/env; order-independent.
- Coverage published; integration/E2E handled in their own suites.
