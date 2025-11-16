---
trigger: always_on
---

---

## trigger: always_on

# Copilot Instructions — SMBLibrary

---

## 1) What this project is

- A C# implementation of SMB 1.0/CIFS, SMB 2.0/2.1, and SMB 3.0 server and client.
- Goals: protocol correctness, backward compatibility, low allocations, and cross‑platform support.
- Design: layered server core (transport, sessions, shares), dialect‑specific handlers (SMB1 vs SMB2/3), storage via `INTFileStore` ports with adapters.
- Logging: event‑driven (`LogEntryAdded`), no external logging dependencies in core.

## 2) What this file is / isn't

- **Is**: a compact brief Copilot reads on every chat—overview, _stable_ structure, stack, norms, and guardrails.
- **Isn't**: a product spec or long agent workflow. Place step‑by‑step prompts in **`/src/Prompts`** and use custom chat modes for specialized tasks.

---

---

## Rules Index

- Architecture (.NET): .windsurf/rules/rules-architecture-dotnet.md
- Architecture (Python): .windsurf/rules/rules-architecture-python.md
- Unit testing (index): .windsurf/rules/rules-unit-testing.md
- Unit testing (C#): .windsurf/rules/rules-unit-testing-csharp.md
- Unit testing (Python): .windsurf/rules/rules-unit-testing-python.md
- Integration testing (.NET): .windsurf/rules/rules-integration-testing-dotnet.md
- Integration testing (Python): .windsurf/rules/rules-integration-testing-python.md
- Code review: .windsurf/rules/rules-code-review.md
- Commits: .windsurf/rules/rules-commits.md
- Pull requests: .windsurf/rules/rules-pull-requests.md
- Code style: .windsurf/rules/rules-code-style.md
- Documentation: .windsurf/rules/rules-documentation.md
- Test-driven development: .windsurf/rules/rules-tdd.md
- Observability: .windsurf/rules/rules-observability.md
- Security: .windsurf/rules/rules-security.md
- LLM & Prompting: .windsurf/rules/rules-llm-prompting.md
- CI/CD Quality: .windsurf/rules/rules-ci-cd.md
- Release & Versioning: .windsurf/rules/rules-release-versioning.md
- Branching Strategy: .windsurf/rules/rules-branching-strategy.md
- Performance Testing: .windsurf/rules/rules-performance-testing.md
- Data Governance: .windsurf/rules/rules-data-governance.md
- UI (C#, Blazorise/MAUI): .windsurf/rules/rules-ui-csharp-blazorise-maui.md

- Retrieval: .windsurf/rules/rules-retrieval.md
- Enrichment: .windsurf/rules/rules-enrichment.md

## Spec‑Then‑Code — How we work

- PRD/SPEC/ADR are created directly by the assistant in docs/, no temp orchestration.
- PRD: concise problem, outcomes (≤5), non‑goals, acceptance criteria (Given/When/Then), success metrics.
- SPEC: Architecture, Interfaces, Telemetry, Risks, Acceptance Criteria. Map AC → tests and telemetry. Note flags/rollback.
- ADRs (MADR‑lite) for significant decisions; keep changes small and iterative.
- PRs link PRD/SPEC/ADRs. Reviewers assess alignment to SPEC.

## Workflows

### Planning

- `/sprint-planning` — Orchestrate sprint planning. Creates/updates PRD and SPEC, drafts ADRs as needed, updates roadmap, runs design review, slices/prioritizes backlog, and kicks off `/start-development` for the first item.
- `/spec-then-code-prd` — Create/update PRD (outcomes, non‑goals, ACs, success metrics).
- `/spec-then-code-spec` — Create/update SPEC (architecture, interfaces, telemetry, risks, ACs; map AC → tests/telemetry; note flags/rollback).
- `/spec-then-code-adr` — Draft ADRs for significant decisions.
- `/spec-then-code-roadmap` — Align sprint scope and dependencies in the version plan.
- `/design-review` — Run a structured design review for PRD/SPEC/ADRs.
- `/spec-then-code-tasks` — Decompose SPEC into `TASKS.md` mapped to ACs with tests and telemetry plans.

### Development

- `/start-development` — TDD flow: write tests first per rules; implement minimal code; keep PRs small (<500 LOC target); add OTEL spans/tags; run linters/tests.

### Quality

- `/code-review` — Scoped code review of working tree or PR; produce review report and checklist.
- `/code-review-apply` — Apply approved review suggestions.

### Git

- `/git-commit` — Conventional commit helper.
- `/git-pull-request` — Create a conventional PR.
- `/pr-address-feedback` — Address PR review comments and feedback.

---

## 3) Solution layout (SMBLibrary)

```text
/                           # repo root
├─ SMBLibrary/              # Core protocol, server, client, codecs, NTFileStore contracts
├─ SMBLibrary.Adapters/     # INTFileStore adapters over filesystem abstractions
├─ SMBLibrary.Win32/        # Windows-specific helpers and providers
├─ SMBServer/               # WinForms host for running the server
├─ SMBLibrary.Tests/        # MSTest unit/integration tests
├─ Utilities/               # Shared utilities (endianness, buffers, helpers)
├─ docs/                    # Documentation (incl. windsurf rules/workflows mirror)
├─ .windsurf/               # (Upstream) — do not edit; use docs/windsurf for overrides
├─ Readme.md, License.txt
```

## 4) Tech stack (assume)

- **Runtime**: .NET Framework 2.0/4.0 and .NET Standard 2.0 (multi‑target in core), plus .NET Framework 4.7.2 / .NET 6 for tests.
- **Testing**: MSTest (`Microsoft.NET.Test.Sdk`, `MSTest.TestFramework`, `MSTest.TestAdapter`).
- **Packaging**: ILRepack in Release to merge `Utilities` into `SMBLibrary.dll`.
- **Logging**: event‑based (`LogEntryAdded`); avoid external logging frameworks in core.

No Python, web UI, or OpenTelemetry assumed for this project.

---

## 5) Retrieval & RAG norms

See: .windsurf/rules/rules-retrieval.md

## 6) Enrichment norms

See: .windsurf/rules/rules-enrichment.md

## 7) LLM contract (behavioural guardrails)

See: .windsurf/rules/rules-llm-prompting.md

## 8) Testing & quality

See: .windsurf/rules/rules-unit-testing.md (index), plus integration/performance rules

## 9) Observability

See: .windsurf/rules/rules-observability.md

## 10) MCP servers (available)

Check and understand available MCP servers, especially for enhancing context.

---

## 11) Working agreements for Copilot

- Maintain legacy compatibility: avoid introducing language/runtime features that break `net20`/`net40` targets.
- Prefer small, reviewable PRs. Keep changes focused; avoid large rewrites without tests.
- Protocol paths return `NTStatus`; avoid exception‑driven control flow.
- Use existing patterns: Little‑Endian helpers, explicit buffer management, event‑driven logging.
- Tests: add MSTest unit tests with deterministic vectors; expose internals via `InternalsVisibleTo` only when needed.
- No new core dependencies without discussion; platform‑specific code belongs in `SMBLibrary.Win32` or adapters.

---

## 12) Anti‑patterns here

- Adding external logging/telemetry frameworks to core.
- Introducing features that break multi‑target builds (e.g., file‑scoped namespaces, Span<T> in core without guards).
- Alloc‑heavy parsing or unchecked buffer math; follow existing codec patterns.
- Logging secrets or raw payloads; committing keys to source.
- Cross‑cutting changes without tests; mixing unrelated refactors with fixes.

---

## 13) Small tasks Copilot can accelerate

- New CQRS handler + port + validator skeleton for a module.
- Draft a prompt file for a new critique profile (e.g., line‑editing).
- Add OpenTelemetry spans/tags to a retrieval or ingestion path.
- Create a readiness probe for OpenSearch/embeddings keys.
- Minimal tests for ingestion idempotence or citation presence.
- Add new Harness CLI command for ops/testing scenario

## 14) Development Flow Preferences

During active development (when working through a sprint or feature):

- **Be autonomous**: Select and proceed with the next logical task from the plan
- **Don't ask for direction**: Choose the highest-priority incomplete task
- **Provide brief updates**: State what you're doing and continue
- **Ask only when blocked**: Request input only if there's genuine ambiguity or missing requirements
- **Use update_plan**: Keep the task list current as you progress

When to ask vs. proceed:

- ✅ Proceed: Next task is clear from PRD/SPEC/TASKS
- ✅ Proceed: Continuing established pattern (e.g., migrating more tests)
- ❌ Ask: Architectural decision needed
- ❌ Ask: User explicitly says "wait for my input"
