---
description: Git Commit - Conventional (feature branches)
auto_execution_mode: 3
---

# Goal

Commit local changes using Conventional Commits, ensuring all non-ignored files are staged and we are committing from a feature branch.

## References

- Conventional Commits: [conventionalcommits.org](https://www.conventionalcommits.org/)
- Windsurf Workflows: [docs.windsurf.com](https://docs.windsurf.com/windsurf/cascade/workflows)

## Steps

// turbo

1. Verify repository and remotes (read-only)
   Run: `git rev-parse --is-inside-work-tree`
   Run: `git remote -v`

// turbo

1. Ensure you are on a feature branch (not main/master/release)
   Run: `git branch --show-current`
   If branch is `main`, `master`, `develop`, or matches `release/*`, create a feature branch first:
   Example: `git checkout -b feat/docs-markdownlint`

// turbo

1. Review working tree
   Run: `git status --porcelain`
   Run: `git ls-files --others --exclude-standard` (untracked, respects .gitignore)

1. Stage all changes that are not ignored
   Run: `git add -A`

// turbo

1. Show staged summary (verify before committing)
   Run: `git status --short --branch`
   Run: `git diff --staged --name-status`

1. Compose Conventional Commit message
   - Types: `feat` | `fix` | `docs` | `refactor` | `chore` | `test` | `perf` | `ci` | `build` | `style`
   - Scope: prefer top-level area from paths: `smbcore` | `smbserver` | `adapters` | `win32` | `tests` | `docs` | `build`
   - Subject: imperative, <= 72 chars
   Body (optional): what/why, not how; wrap at ~72 cols
   Footer (optional): refs/issues, breaking changes
   Example: `feat(docs): add markdownlint config and fix project-structure lints`
   Commit: `git commit -m "type(scope): subject" -m "body (optional)"`
   If there are no staged changes, skip commit.

// turbo

1. Validate commit message with commitlint
   Run: `git log -1 --pretty=%B | npx commitlint`

1. Push branch to remote
   First push: `git push -u origin $(git branch --show-current)`
   Subsequent: `git push`

## Safety & Notes

- Staging uses `git add -A` which respects `.gitignore`.
- Do not commit directly to `main`/`master`; use feature branches per repo policy.
- If hooks (e.g., lint/format) are configured, they may run on commit.
