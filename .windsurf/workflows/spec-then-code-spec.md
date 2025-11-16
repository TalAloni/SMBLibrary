---
description: Spec‑Then‑Code — SPEC
auto_execution_mode: 3
---

# Goal

Author or update a design SPEC aligned to the PRD, LLM-direct (no temp orchestration).

## Inputs

- Title
- Sprint (YYYY-MM-sprint-NN)
- Linked PRD path
- Affected modules

## Steps

1. Assistant: Create or update `docs/sprints/{sprint}/SPEC.md` with:
   - Architecture, Interfaces, Telemetry, Risks, Acceptance Criteria
   - Map AC → tests/telemetry; note feature flags and rollback
1. Assistant: Confirm alignment with the PRD and list ADRs to draft.

## Acceptance checklist

- SPEC links the PRD and ADRs.
- ACs are implementable and verifiable.
