---
trigger: model_decision
---

# Rules — CI/CD Quality Gates [Model Decision]

- All PRs: build + analyzers must pass; unit/integration tests green; coverage published.
- Formatting/lint: dotnet format; ruff/black; markdownlint; cspell; commitlint; prettier for docs/scripts.
- Security: gitleaks for secrets; dependency checks; minimal permissions for CI tokens.
- Artifacts: publish test results and coverage; retain for troubleshooting; no secrets in logs/artifacts.
- Branch protection: require review and passing checks to merge; linear history (rebase or squash) preferred.
