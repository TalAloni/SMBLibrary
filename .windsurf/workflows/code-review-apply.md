---
description: Code Review - Apply Suggestions
auto_execution_mode: 3
---

# Goal

Apply code review suggestions with **immediate fix-as-found** approach for safe changes, followed by manual review for complex items. Prioritizes quick resolution of mechanical issues (constants, formatting, simple refactoring) before deeper work.

## Prep

Temporary artifacts are written to `.tmp/code-review-apply/` and should be deleted between runs.

### Steps (cross-platform)

// turbo

1. Ensure clean temp directory

   Run: `node -e 'const fs=require("fs"); fs.rmSync(".tmp/code-review-apply", { recursive:true, force:true }); fs.mkdirSync(".tmp/code-review-apply", { recursive:true }); console.log("prepared .tmp/code-review-apply");'`

// turbo

1. Verify repository has commits (HEAD exists)

   Run: `git rev-parse --verify HEAD`

   If this fails, run `/git-commit` to create an initial commit, then retry this workflow.

1. Optional: Create a short-lived apply branch

   Run: `node -e 'const { execSync } = require("child_process"); const p=n=>String(n).padStart(2,"0"); const d=new Date(); const ts=`${d.getFullYear()}${p(d.getMonth()+1)}${p(d.getDate())}-${p(d.getHours())}${p(d.getMinutes())}${p(d.getSeconds())}`; execSync(`git checkout -b review/apply-${ts}`, { stdio:"inherit" });'`

// turbo

1. Locate available review reports
   Run: `node -e 'const fs=require("fs"),p=require("path");function walk(d){let out=[];if(fs.existsSync(d)){for(const e of fs.readdirSync(d,{withFileTypes:true})){const fp=p.join(d,e.name); if(e.isDirectory()) out=out.concat(walk(fp)); else if(e.isFile()&&e.name==="report.md") out.push(fp);} } console.log(out.join("\\n"));'`

## Phase 1: Immediate Safe Fixes

Apply mechanical/safe suggestions immediately after review generation.

1. **Identify and apply safe suggestions** (e.g., extract constants, formatting, simple refactoring)

   **Safe changes criteria:**
   - Extract duplicate string literals to constants
   - Add/improve helper functions for path resolution
   - Extract configuration constants
   - Auto-fix linter issues (ruff --fix, prettier)
   - Add missing imports or docstrings

   **Manual implementation:**
   - Review suggestions in `.tmp/code-review/report.md`
   - Apply each safe suggestion using edit tools
   - Run tests after each change: `pytest -x` or equivalent
   - Auto-fix linters: `ruff check --fix`, `ruff format`

   **Validation:**
   - All tests must pass after each change
   - Linters must be clean
   - No regressions

## Phase 2: Manual Review (Complex Changes)

1. Process reports sequentially (for remaining complex items)

   Run: `node scripts/workflow/review-iterate-reports.mjs .tmp`

   Tip: Set `PLAN_ONLY=1` env var or add `--plan-only` to evaluate without applying.

// turbo

1. Aggregate reports into a single view (optional)

   Run: `node scripts/workflow/review-aggregate.mjs .tmp/code-review-apply/all-reports.md .tmp`

// turbo

1. Derive executable plan from reports

   Run: `node scripts/workflow/review-derive-plan.mjs .tmp/code-review-apply/all-reports.md .tmp/code-review-apply/plan.json`

1. Execute plan (apply structured operations)

   Run: `node scripts/workflow/review-exec-plan.mjs .tmp/code-review-apply/plan.json`

// turbo

1. Dry-run apply any suggested patches found in reports

   Run: `node scripts/workflow/review-apply-patches.mjs .tmp/code-review-apply/all-reports.md 1`

// turbo

1. Apply patch suggestions (after successful dry-run)

   Run: `node scripts/workflow/review-apply-patches.mjs .tmp/code-review-apply/all-reports.md 1 apply`

// turbo

1. Build target file list from reviews

   Run: `node scripts/workflow/review-gather-files.mjs`

// turbo

1. Auto-fix mechanical issues (format, lint, basic style) and stage

   Run: `node scripts/workflow/review-auto-fix.mjs .tmp/code-review-apply/targets.txt`

1. Curate your selection

   Create and maintain `.tmp/code-review-apply/selected.md` with the items you plan to apply:

   ```markdown
   # Apply Plan

   - [ ] Item 1 — <short title>
     - Files: path/to/file1.ext, path/to/file2.ext
     - Summary: <what to change>
     - Category: Blocker/Suggestion/Nit
     - Notes: <rationale or acceptance criteria>

   - [ ] Item 2 — ...
   ```

1. Apply changes per item
   - Open each file and implement the change described in the selection.
   - For renames/moves: `git mv old/path new/path`
   - Stage changes incrementally:
     - Run: `git add -p` (interactive) or `git add path/to/file`

// turbo

1. Validate formatting and linting
   - Prettier (repo root): Run: `npm run format:check` (or `npm run format` to write changes)
   - Markdownlint (quick): Run: `npx --yes markdownlint-cli2 "**/*.md" "#node_modules"`
   - .NET format (if a solution exists):
     - POSIX: `sln=$(git ls-files "*.sln" | head -n 1); [ -n "$sln" ] && dotnet format "$sln" --verify-no-changes || true`
     - Windows (PowerShell): `pwsh -NoProfile -Command "$sln=(git ls-files '*.sln' | Select-Object -First 1); if ($sln) { dotnet format $sln --verify-no-changes }"`
   - Python (if installed):
     - Run: `ruff check .`
     - Run: `black --check .`

// turbo

1. Commit
   - Compose a Conventional Commit (commitlint enforced):
     - Example: `git commit -m "fix(dotnet): apply review suggestions for telemetry docs"`
   - If you prefer to review staged changes first: Run: `git status -sb && git diff --cached`

---

## Sources this workflow supports

- `.tmp/code-review/report.md` (delta flow)
- `.tmp/code-review-wt/report.md` (working tree flow)
- `.tmp/code-review-wt-fw/report.md` (file-wise working tree flow)

You can also reference per-file artifacts under `.tmp/code-review-wt-fw/files/` while applying.

---

## Wrap Up

- Re-run the relevant code review workflow to verify issues are resolved.
- If you created a branch, open a PR using `/git-pull-request`.
- Clean up temp artifacts:
  - Run: `node -e 'require("fs").rmSync(".tmp/code-review-apply", { recursive:true, force:true }); console.log("cleaned .tmp/code-review-apply");'`
