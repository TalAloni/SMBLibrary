---
trigger: model_decision
---

# Rules — Branching Strategy [Model Decision]

- Mainline (trunk-based): keep feature branches short-lived; frequent small PRs.
- Naming: feat/_, fix/_, docs/_, chore/_, refactor/\*.
- Merge: require review + green checks; prefer squash or rebase to keep history clean.
- Hotfix: branch from main, tag on release; backport if necessary with explicit PRs.
