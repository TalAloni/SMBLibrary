---
description: Spec‑Then‑Code — ADR
auto_execution_mode: 3
---

# Goal

Create a MADR‑lite ADR documenting a significant decision.

## Inputs

- Title
- Status: proposed|accepted|rejected|superseded
- Context, Decision, Consequences, Alternatives

## Steps

1. Assistant: Create `docs/decisions/NNNN-title.md` with frontmatter and sections above, and update `docs/decisions/index.md` if present.
1. Assistant: Link this ADR from PRD/SPEC and relevant code.

## Acceptance checklist

- ADR status set; consequences/alternatives discussed.
