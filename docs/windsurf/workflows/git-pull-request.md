---
description: Pull Request - Conventional (separate flow)
auto_execution_mode: 3
---

# Goal

Open a pull request from a feature branch with a Conventional Commit-style title and a useful body, after running local quality checks.

## Prerequisites

- Logged in with GitHub CLI (`gh auth status`).
- On a feature branch (not `main`, `master`, `develop`, or `release/*`).

## Steps

// turbo

1. Verify repository and branch
   Run: `git rev-parse --is-inside-work-tree`
   Run: `git branch --show-current`

// turbo

1. Ensure working tree is clean or run /git-commit
   Run: `git status --porcelain`
   If output is not empty, run the `/git-commit` workflow to stage, Conventional-commit, and push changes. Then return to continue this PR flow.

// turbo

1. Confirm remotes and base branch
   Run: `git remote -v`
   Run: `git fetch origin --prune`
   Determine base branch (default is `main`).

1. Optional: Sync with base (manual)
   If needed, rebase/merge from the base branch to reduce PR conflicts.
   Example (manual): `git rebase origin/main`

1. Run local quality checks (recommended)
   - C#: `dotnet test -v minimal`
   - Optional: run any local linters or secret scanners you have configured.

// turbo

1. Prepare PR title (Conventional Commit style)
   Types: `feat` | `fix` | `docs` | `refactor` | `chore` | `test` | `perf` | `ci` | `build` | `style`
   Scopes: `smbcore` | `smbserver` | `adapters` | `win32` | `tests` | `docs` | `build` | `ci` | `workflows` | `scripts`
   Example: `docs(workflows): add conventional PR workflow with checks`
   Validate (optional): `echo "<title>" | npx commitlint`

1. Prepare PR body (use this template)

   ```markdown
   ## Summary

   Brief description of the change.

   ## Changes

   - Key change 1
   - Key change 2

   ## Tests

   - Unit/Integration/E2E coverage summary

   ## Checklist

   - [ ] Conventional PR title
   - [ ] Local checks passed (lint, secrets)
   - [ ] No secrets or large artifacts committed
   ```

1. Create PR
   Run: `gh pr create --title "<title>" --body "<body>" --base main --head $(git branch --show-current)`
   Or open in browser to edit body: `gh pr create --fill --web`

## Notes

- PR title lints will run in CI (semantic PR title). Keep it Conventional.
- Avoid force-pushes after review has started.
