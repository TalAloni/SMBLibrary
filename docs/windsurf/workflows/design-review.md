---
description: Design Review — PRD/SPEC/ADR/Roadmap (LLM‑direct, cross‑platform)
auto_execution_mode: 3
---

# Purpose

A concise, repeatable workflow to review design artifacts (PRD, SPEC, ADR, Roadmap) using an LLM‑direct approach. Cross‑platform. No shell steps. Optimized for the repo’s Spec‑Then‑Code method and rule set under `.windsurf/rules`.

# Inputs

- **ArtifactType**: PRD | SPEC | ADR | Roadmap
- **ArtifactPaths**: One or more markdown files to review
- **Context**: Sprint/milestone, linked PRs/issues, related ADRs/specs/roadmaps
- **Depth**: Quick scan | Full review
- **QualityGates**: Use default gates below or specify additional gates

# Quick Start

1. Gather inputs (type, paths, context, depth).
1. Read the artifact(s) and the applicable rule files (see References).
1. Run the “LLM Review Prompt” (below) tailored to the artifact type.
1. Produce a structured review comment with:
   - Summary, blocking issues, suggestions, questions, nits
   - Decision (Approve / Approve w/ nits / Request changes)
   - Actionable checklist with owners and due dates
   - Citations to repo files/sections
1. Share review with authors. Iterate until gates pass.

# Quality Gates (Default)

- **Requirements (PRD)**
  - Problem, outcomes (≤5), non‑goals present
  - Clear, testable acceptance criteria (Given/When/Then)
  - Success metrics defined and measurable
- **Design (SPEC)**
  - Architecture fits module boundaries; ports/adapters; no UI→infra shortcuts
  - Interfaces, contracts, and data flows explicit; diagrams referenced if needed
  - Feature flags and rollback plan defined
  - Telemetry plan: spans, tags, logs, and metrics mapped to key flows
- **Decision Record (ADR)**
  - Context, options, decision, consequences; scoped and reversible if needed
  - Links to SPEC/PRD; flags noted; rollback considerations
- **Roadmap**
  - Milestones map to outcomes; dependencies and risks called out
  - Phasing aligns with feature flags and rollback strategy
- **Testing & Quality**
  - Unit/integration/e2e/perf coverage plan tied to acceptance criteria
  - Test data, fixtures, and environments feasible (e.g., Testcontainers)
- **Security & Data Governance**
  - No secrets in docs; data classification noted; PII handling if relevant
  - Threats and mitigations acknowledged for critical paths
- **Observability**
  - OpenTelemetry strategy (traces/logs/metrics); health checks (`/healthz`, `/readyz`)
- **Release**
  - Versioning strategy and release notes plan; rollout gates defined

# Review Steps

1. Scope the review
   - **Confirm inputs**: type/paths/context/depth.
   - **Identify dependencies**: linked ADRs/specs/roadmaps; affected modules.

1. Read artifact(s) and rules
   - Apply relevant rule files (architecture, testing, observability, security, CI/CD, release, branching, performance, data governance, RAG norms).
   - Note inconsistencies, ambiguities, and missing sections.

1. Cross‑artifact consistency
   - **PRD → SPEC**: Map outcomes and acceptance criteria to SPEC components, handlers, and contracts.
   - **SPEC → ADR**: Verify decisions are captured; consequences/rollbacks noted.
   - **SPEC ↔ Roadmap**: Ensure milestones deliver measurable outcomes and include flags/rollbacks.

1. Quality gate evaluation
   - For each gate above, mark Pass / Risk / Fail with a short note and citation.
   - Record blocking issues and suggested remediations.

1. Produce the review comment
   - Summary, major findings, blocking issues, suggestions, questions, nits.
   - Decision: Approve / Approve w/ nits / Request changes.
   - Actionable checklist with owners and due dates.
   - Citations to repo documents and sections.

1. Follow‑ups
   - If changes are required, propose concrete edits or outline a SPEC/ADR delta.
   - Re‑review only the delta and re‑run gates.

# LLM Review Prompt (copy/paste and fill)

```text
You are reviewing a {ArtifactType} for the doc-search-platform repo. Follow Spec‑Then‑Code, cite files/sections, and apply the Quality Gates.

Inputs:
- ArtifactType: {PRD|SPEC|ADR|Roadmap}
- ArtifactPaths: {relative paths}
- Context: {sprint/milestone, linked PRs/issues, related ADRs/specs/roadmaps}
- Depth: {Quick scan|Full review}

Task:
1) Summarize the artifact in 3-5 bullets.
2) Evaluate against the Quality Gates; mark each Gate as Pass/Risk/Fail with 1-2 sentences and citations.
3) Check cross‑artifact consistency (PRD↔SPEC↔ADR↔Roadmap) and list any mismatches.
4) List blocking issues (if any), suggestions (ranked), questions, and nits. Keep bullets short.
5) Provide a decision: Approve / Approve with nits / Request changes.
6) Provide an actionable checklist (owner, due date) for required changes.

Output format:
- Summary
- Gate Evaluations
- Cross‑Artifact Consistency
- Findings
  - Blocking
  - Suggestions
  - Questions
  - Nits
- Decision
- Actionable Checklist
- Citations
```

# References (rules and norms)

- **Architecture (.NET)**: `.windsurf/rules/rules-architecture-dotnet.md`
- **Architecture (Python)**: `.windsurf/rules/rules-architecture-python.md`
- **Testing (index)**: `.windsurf/rules/rules-unit-testing.md`
- **Unit testing (C#)**: `.windsurf/rules/rules-unit-testing-csharp.md`
- **Unit testing (Python)**: `.windsurf/rules/rules-unit-testing-python.md`
- **Integration testing (.NET)**: `.windsurf/rules/rules-integration-testing-dotnet.md`
- **Integration testing (Python)**: `.windsurf/rules/rules-integration-testing-python.md`
- **Observability**: `.windsurf/rules/rules-observability.md`
- **Security**: `.windsurf/rules/rules-security.md`
- **LLM & Prompting**: `.windsurf/rules/rules-llm-prompting.md`
- **CI/CD Quality**: `.windsurf/rules/rules-ci-cd.md`
- **Release & Versioning**: `.windsurf/rules/rules-release-versioning.md`
- **Branching Strategy**: `.windsurf/rules/rules-branching-strategy.md`
- **Performance Testing**: `.windsurf/rules/rules-performance-testing.md`
- **Data Governance**: `.windsurf/rules/rules-data-governance.md`
- **Code Review**: `.windsurf/rules/rules-code-review.md`
- **RAG norms**: `.windsurf/rules/rules-retrieval.md`
- **Enrichment norms**: `.windsurf/rules/rules-enrichment.md`

# Output Checklist (exit criteria)

- **Structured review comment** produced (per prompt format)
- **Gate statuses** recorded with citations
- **Decision** made and communicated
- **Actionable checklist** assigned
- **Links** to related PRD/SPEC/ADR/Roadmap included

# Tips

- **Small deltas**: prefer iterative changes and focused ADRs.
- **Feature flags + rollback**: always plan for safe rollout.
- **RAG‑first with citations**: ensure retrieval/enrichment choices are justified.
- **Respect module boundaries**: no UI → infra shortcuts; keep handlers and ports explicit.

# Optional follow‑ups (no automation)

- **/code-review**: Use for scoped code diffs after the design is implemented.
- **/git-commit** and **/git-pull-request**: Use to commit and open PRs for doc changes.
