---
description: Sprint Planning — Orchestrate Spec‑Then‑Code
auto_execution_mode: 3
---

# Goal

Plan a sprint by defining scope and triggering Spec‑Then‑Code workflows (PRD, SPEC, ADR, Roadmap, Design Review). LLM‑direct, no temp orchestration. Repeatable per sprint.

## Inputs

- **Sprint**: `YYYY-MM-sprint-nn` (naming: year-month-sprint-sequence; e.g., 2025-11-sprint-08)
- **Theme**: High-level direction for the sprint
- **Outcomes**: ≤5 measurable outcomes
- **Non-goals**: Explicitly out of scope
- **Candidate features**: Bulleted list
- **Affected modules**: .NET/Python modules
- **Risks/Dependencies**: External constraints

## Steps

### 1. Initialize Sprint Docs

1. **Assistant**: Ensure `docs/sprints/{sprint}/` exists with stubs:
   - `PRD.md`
   - `SPEC.md`
   - `IMPLEMENTATION_LOG.md`
   - `RELEASE_NOTES_DRAFT.md`
   - Link any ADRs under `docs/decisions/`

### 2. Author PRD (LLM‑direct)

1. **Assistant**: Run `/spec-then-code-prd` with inputs:
   - Title: `Sprint {sprint}: {theme}`
   - Outcomes, Non‑goals
   - Acceptance Criteria (Given/When/Then)
   - Success Metrics

### 3. Author SPEC (LLM‑direct)

1. **Assistant**: Run `/spec-then-code-spec` with inputs:
   - Sprint: `{sprint}`
   - Linked PRD path: `docs/sprints/{sprint}/PRD.md`
   - Affected modules
   - Architecture, Interfaces, Telemetry, Risks, Acceptance Criteria
   - Map AC → tests/telemetry; note feature flags and rollback

### 3.5 Generate Tasks (LLM‑direct)

1. **Assistant**: Run `/spec-then-code-tasks` to create/update `docs/sprints/{sprint}/TASKS.md`:
   - Map each SPEC Acceptance Criterion to 1–3 tasks (≤2 days each)
   - Each task includes test strategy (unit/integration/eval) and telemetry plan
   - Note feature flags, rollback, and observability verification where applicable

### 4. Identify and Draft ADRs (as needed)

1. **Assistant**: For significant decisions, run `/spec-then-code-adr` and create ADRs under `docs/decisions/`:
   - Clear context, decision, alternatives, consequences
   - Link ADRs from the SPEC

### 5. Roadmap Alignment (optional)

1. **Assistant**: Run `/spec-then-code-roadmap` to reflect the sprint scope and dependencies in the version plan.

### 6. Design Review

1. **Assistant**: Run `/design-review` targeting the new SPEC and ADRs.

### 7. Backlog and Slicing

1. **Assistant**: Create a backlog mapped to SPEC Acceptance Criteria:
   - Items sized ≤2 days each
   - Each item must include test strategy (unit/integration) and telemetry plan
   - Note feature flags and rollback plan where applicable
1. **Assistant**: Prioritize items (must‑have → should‑have → could‑have) for the sprint.

### 8. Ready to Start Development

1. **Assistant**: Propose initial implementation order.
1. **Assistant**: For the first item, run `/start-development` to follow TDD.

## Guardrails and References

- **TDD**: `.windsurf/rules/rules-tdd.md`
- **Architecture**: `.windsurf/rules/rules-architecture-dotnet.md`, `.windsurf/rules/rules-architecture-python.md`
- **Testing**: `.windsurf/rules/rules-unit-testing-csharp.md`, `.windsurf/rules/rules-unit-testing-python.md`
- **Observability**: `.windsurf/rules/rules-observability.md`
- **Security**: `.windsurf/rules/rules-security.md`

## Acceptance Checklist

- [ ] PRD created/updated and committed
- [ ] SPEC authored with AC → tests/telemetry mapping
- [ ] TASKS.md created/updated via `/spec-then-code-tasks` (≤2 days per item, with tests and telemetry)
- [ ] Required ADRs drafted and linked
- [ ] Design review completed
- [ ] Roadmap updated (if applicable)
- [ ] Backlog sliced (≤2 days per item) and prioritized
- [ ] First item ready for `/start-development`
