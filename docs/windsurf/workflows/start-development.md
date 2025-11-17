---
description: Start Development — TDD Flow
auto_execution_mode: 3
---

# Goal

Begin implementation for SPEC acceptance criteria following TDD best practices. Build incrementally on a feature branch, batching related work into meaningful PRs (not tiny individual changes).

## Inputs

- **Feature Area**: High-level completion item (e.g., "SMB2 Client", "Server IOCTLs", "DFS scaffolding")
- **Scope**: Related behaviors to implement together (aim for cohesive, reviewable PRs)

## Prerequisites

Before starting development, ensure you have at least a short description of the change (issue, TODO, or docs note) and understand the expected behavior on the wire (protocol references/tests).

## Steps

### 1. Review Planning Artifacts

1. **Assistant**: Read the SPEC, PRD, and linked ADRs for context
   - Identify the specific AC(s) you're implementing
   - Note mapped tests and telemetry requirements from SPEC
   - Understand interfaces, dependencies, and risks

### 2. Design Tests First (TDD)

1. **Assistant**: Following `.windsurf/rules/rules-tdd.md` and C# testing rules:
   - `.windsurf/rules/rules-unit-testing-csharp.md`, `.windsurf/rules/rules-integration-testing-dotnet.md`

1. **Assistant**: Create test files BEFORE implementation:
   - **Unit tests**: Cover protocol logic, mapping, and edge cases in isolation.
   - **Integration tests** (if needed): Use `SMBLibrary.Tests` to exercise server/client flows.

1. **Test naming**: Follow `Method_Scenario_ExpectedResult` pattern

1. **Coverage targets**:
   - Unit: 100% on new business logic
   - Integration: Cover happy path + key error paths

1. **Assistant**: Write failing tests that define expected behavior
   - Use Given/When/Then structure
   - Assert on outcomes, not implementation details
   - Include edge cases and error scenarios

// turbo

1. **Verify tests fail correctly**

   Run: `dotnet test --filter "FullyQualifiedName~{FeatureName}" --verbosity normal`

   Confirm tests fail for the right reasons (not-implemented or missing behavior).

### 3. Implement Incrementally

1. **Assistant**: Implement the minimal code to make tests pass
   - Follow `.windsurf/rules/rules-architecture-dotnet.md`.
   - Follow `.windsurf/rules/rules-code-style.md`.
   - Respect existing layering (server/client, NTFileStore, adapters).
   - Add logging via `LogEntryAdded` where it materially helps diagnostics (see `.windsurf/rules/rules-observability.md`).

1. **Keep changes focused**:
   - One feature or AC at a time
   - Aim for <500 LOC changed per PR
   - If scope grows, split into multiple dev cycles

1. **Refactor as needed**:
   - Extract helpers, interfaces, or value objects
   - Keep domain framework-free
   - Document any architectural decisions as ADRs

// turbo

1. **Run tests after implementation**

   Run: `dotnet test --filter "FullyQualifiedName~{FeatureName}" --verbosity normal`

   Confirm all tests pass.

### 4. Verify Quality Standards

1. **Assistant**: Self-review against checklist:
   - [ ] All new tests passing
   - [ ] Coverage targets met (unit: 100%, integration: key paths)
   - [ ] OTEL spans/tags added for new flows
   - [ ] No secrets or PII committed
   - [ ] Docs/contracts updated (if public APIs changed)
   - [ ] Feature flags documented (if applicable)
   - [ ] Follows code style and architecture rules

// turbo

1. **Run full test suite**

   Run: `dotnet test`

// turbo

1. **Run build/analyzers**

   Run: `dotnet build --no-incremental`

### 5. Document and Commit

1. **Assistant**: Update implementation notes
   - Add brief summary to `docs/sprints/{sprint}/IMPLEMENTATION_LOG.md` (create if missing):

     ```markdown
     ## [{date}] {Feature/AC ID} - {Brief Title}

     **Implemented**: {1-2 line summary}
     **Tests Added**: {count unit, count integration}
     **Files Changed**: {key files}
     **AC Status**: {Fully met | Partially met | Blocked}
     ```

1. **Commit changes** (use `/git-commit` workflow for conventional commits):
   - Atomic commits (tests + implementation together)
   - Link to SPEC AC or PRD in commit message
   - Follow `.windsurf/rules/rules-commits.md`

### 6. Prepare for PR (Optional)

If this completes a reviewable unit of work:

1. **Assistant**: Run `/code-review` with scope=`working`
   - Address any blockers before pushing

1. **Assistant**: Create PR using `/git-pull-request`
   - Link to SPEC and PRD
   - Note which ACs are satisfied
   - Include test coverage summary

## Acceptance Checklist

Before considering this cycle complete:

- [ ] Tests written BEFORE implementation
- [ ] All tests passing (unit + integration)
- [ ] Coverage targets met (new code is exercised)
- [ ] Logging added where appropriate (no secrets or raw payloads)
- [ ] No blockers from self-review
- [ ] Implementation log updated
- [ ] Changes committed with conventional message
- [ ] PR size reasonable (<500 LOC if possible)

## Iterating

Run this workflow again for the next feature/AC:

- Each run should target 1-2 ACs max
- Keep PRs small and reviewable
- Build incrementally toward sprint goals

## Notes

- **TDD is non-negotiable**: Tests MUST be written first
- **Small batches**: Better to run this 5 times for 5 small features than once for a large one
- **Integration tests**: Only add when testing I/O or external dependencies (network, filesystem, OS APIs)
- **Blocked?**: If stuck on dependencies, document in IMPLEMENTATION_LOG and discuss with team
