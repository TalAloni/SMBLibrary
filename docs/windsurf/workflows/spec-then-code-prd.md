---
description: Spec‑Then‑Code — PRD
auto_execution_mode: 3
---

# Goal

Author or update a Product Requirements Document (PRD) for the current sprint, LLM-direct (no temp orchestration).

## Inputs

- Title
- Sprint (YYYY-MM-sprint-NN)
- Problem
- Desired outcomes (≤5, measurable)
- Non-goals
- Acceptance criteria (Given/When/Then)
- Success metrics (North Star + 2–3 KPIs)

## Steps

1. Assistant: Create or update `docs/sprints/{sprint}/PRD.md` with frontmatter and the sections above.
1. Assistant: Ensure acceptance criteria are testable and map to modules/tests.
1. Assistant: Summarize next steps and any open questions.

## Acceptance checklist

- PRD links to related ADRs (if any) and SPEC (when available).
- ACs are concrete and testable; metrics are measurable.
