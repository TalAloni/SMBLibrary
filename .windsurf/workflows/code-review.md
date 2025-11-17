---
description: Code Review — Scoped
auto_execution_mode: 3
---

# Goal

Run a consistent code-review routine for any scope:

- `pr` – branch delta vs. remote base (`origin/HEAD` by default).
- `working` – staged + unstaged changes vs. `HEAD`.
- `filewise` – working tree review broken down per file.

Outputs live under `.tmp/code-review/` and feed `/code-review-apply` when ready.

## Inputs

- **scope**: `pr` | `working` | `filewise`
- **base** (optional, PR scope only): remote branch to diff against (default: detected via `origin/HEAD`).

## Prep

Temporary artifacts are written to `.tmp/code-review/` and should be deleted between runs.

### Steps (cross-platform)

// turbo

1. Ensure clean temp directory

   Run: `pwsh -NoProfile -Command "Remove-Item -Recurse -Force .tmp/code-review -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Path .tmp/code-review | Out-Null; Write-Host 'prepared .tmp/code-review'"`

// turbo

1. Validate repository state

   Run: `git rev-parse --verify HEAD`

   Run: `git status --porcelain`
   - If the repository has no commits, run `/git-commit` before reviewing.
   - If `scope=pr`, consider committing local WIP first so the delta reflects committed changes.

---

### Scope actions

Run the block matching your chosen `scope`.

#### scope = `pr`

// turbo

1. Detect base branch (if not provided)

   Run: `git fetch origin --prune`

   Run: `git rev-parse --abbrev-ref origin/HEAD`

   Use the printed branch (e.g., `origin/main`) unless you supplied a custom `base` input.

// turbo

1. Generate deltas vs. base (`<base>`) and summarize files

   Run: `git diff <base>...HEAD > .tmp/code-review/git-delta-commits.md`

   Run: `git diff <base> > .tmp/code-review/git-delta-working-tree.md`

   Run: `git status -sb > .tmp/code-review/git-status.txt`

   Run: `git diff --name-only <base>...HEAD > .tmp/code-review/files.txt`

#### scope = `working`

// turbo

1. Capture staged + unstaged changes vs. HEAD

   Run: `git diff HEAD > .tmp/code-review/git-delta-working-tree.md`

   Run: `git status -sb > .tmp/code-review/git-status.txt`

   Run: `git diff --name-only HEAD > .tmp/code-review/files.txt`

#### scope = `filewise`

// turbo

1. Produce per-file diffs for the working tree

   Run: `pwsh -NoProfile -Command "git diff --name-only HEAD | ForEach-Object { $safe = ($_ -replace '[^\w\.-]','_'); git diff HEAD -- $_ | Out-File -Encoding utf8 ('.tmp/code-review/' + $safe + '-diff.md') }"`

---

## Checklist (all scopes)

- **Protocol & Wire Correctness**: Parsing/encoding uses existing helpers; offsets/lengths guarded; NTSTATUS semantics (no exception control flow on protocol paths).
- **Compatibility**: Changes keep `net20; net40; netstandard2.0` builds compiling; no file-scoped namespaces/modern-only APIs in core.
- **Allocation & Performance**: Avoid LINQ/alloc-heavy patterns on hot paths; reuse buffers; adhere to existing codec style.
- **Dependencies**: No new core dependencies (logging/telemetry/etc.). Platform-specific code stays in `SMBLibrary.Win32` or adapters.
- **Testing**: MSTest; deterministic vectors; add/adjust tests for new behaviors; naming `Method_Scenario_ExpectedResult`.
- **Logging**: Use event-based logging; no raw payloads or secrets in logs.
- **Docs**: Update docs where public behaviors or contracts change.

Flag findings as **Blocker**, **Suggestion**, or **Nit**.

## Report

1. Generate review report

   Assistant: Summarize deltas and checklist findings into `.tmp/code-review/report.md` using the template below.

```markdown
# Code Review Report

- **Scope:** <scope>
- **Base:** <base or HEAD>
- **Files Changed:** <count>
- **Reviewer:** Cascade

## Summary

- Key features or fixes
- Risk areas or regressions

## Findings

- **Blockers:**
  1. <File:path:line> – Issue + fix suggestion
- **Suggestions:**
  1. <File:path:line> – Improvement + rationale
- **Nits:**
  1. <File:path:line> – Minor polish

## Architecture & Quality Snapshot

- Module boundaries: ✅/⚠️/❌
- CQRS patterns: ✅/⚠️/❌
- Telemetry/Security: ✅/⚠️/❌

## Follow-up

- [ ] Blockers resolved
- [ ] Suggestions tracked or addressed
- [ ] Tests green (unit/integration/E2E)
- [ ] Docs updated

**Verdict:** ✅ Approve / ⚠️ Approve w/ follow-up / ❌ Needs rework
```

// turbo

1. Prepare aggregated apply workspace (optional)

   Run: `node scripts/workflow/review-aggregate.mjs .tmp/code-review-apply/all-reports.md .tmp`

// turbo

1. Apply approved suggestions

   Run: `/code-review-apply`

## Wrap Up

- Share report highlights and verdict with the team.
- Offer help filing issues or tasks for follow-ups.
- Re-run `/code-review` after fixes to confirm blockers resolved.
- Clean temp artifacts:

  Run: `node -e "require('fs').rmSync('.tmp/code-review',{recursive:true,force:true}); console.log('cleaned .tmp/code-review');"`
