---
description: Spec‑Then‑Code — Version Roadmap
auto_execution_mode: 3
---

# Goal

Author or update a roadmap for a minor or major version (LLM‑direct; no temp orchestration).

## Inputs

- Version: vX.Y (minor) or vX (major)
- Type: minor | major
- Timeframe (start → target)
- Themes / initiatives (epics)
- Compatibility/breaking changes (if any)
- Migration expectations (if major)
- Feature flags & rollout strategy
- Risks / assumptions / dependencies
- Metrics & success criteria

## Steps

1. Assistant: Create or update `docs/versions/{version}/ROADMAP.md` with frontmatter:
   - `version`, `type`, `timeframe`, `status: draft|proposed|accepted`
   - Sections:
     - Overview & Objectives
     - Scope & Non‑goals
     - Themes & Epics (brief bullet per theme)
     - Timeline & Milestones (phases with rough dates)
     - Dependencies & Risks (mitigations)
     - Compatibility & Breaking Changes (explicitly state “none” if minor)
     - Migration & Rollout (feature flags, progressive delivery, rollback)
     - Telemetry & Quality Gates (what to measure, gates to proceed)
     - References (link related PRDs, SPECs, ADRs)
1. Assistant: If `type=major`, also create `docs/versions/{version}/MIGRATION.md` covering:
   - Deprecated/removed features
   - Migration guides and code modifications
   - Flag strategy, rollback, and validation checklist
1. Assistant: Ensure links to PRDs/SPECs/ADRs are present; list any to-be-authored docs.
1. Assistant: Summarize open questions and next actions in the roadmap footer.

## Acceptance checklist

- ROADMAP includes objectives, themes, milestones, risks, and telemetry gates.
- Major versions include a clear migration plan.
- Compatibility policy is explicit; breaking changes are justified and isolated.
- Feature flags and rollback strategy are defined.
- References to PRDs/SPECs/ADRs are included or noted as TODO.
