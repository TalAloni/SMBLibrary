---
trigger: model_decision
---

# Rules — Commits (Conventional Commits)

- Format: type(scope): subject
- Allowed scopes: docs, infra, search, contracts, dotnet, python, ci, tests, workflows, scripts
- Subject: lower-case, ≤72 chars. Body explains what/why. Footer: references/Breaking-Change.
- Make atomic commits; avoid mixing refactors with behavior changes.
- Examples: docs(docs): add rules index; dotnet(retrieval): add OTEL span

Reference: conventionalcommits.org v1.0.0
