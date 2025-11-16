---
trigger: model_decision
---

# Rules — Release & Versioning [Model Decision]

- Versioning: Semantic Versioning (SemVer); tags as vX.Y.Z.
- Notes: maintain CHANGELOG and release notes; link PRD/SPEC/ADRs.
- Pre-release: use -alpha / -beta / -rc suffixes; gate behind feature flags.
- Automation: derive next version from commits (feat/fix + breaking); verify CI green before tagging.
