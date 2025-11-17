---
description: Address PR review comments and feedback
auto_execution_mode: 3
---

# Goal

Systematically address feedback from a pull request review, implement requested changes, and push updates to the PR branch.

## Inputs

- **PR URL or number**: GitHub pull request to address (e.g., `https://github.com/owner/repo/pull/123` or just `123`)

## Prerequisites

- GitHub CLI authenticated (`gh auth status`)
- On the correct feature branch for the PR
- Repository is clean or changes are committed

## Steps

// turbo

1. Verify GitHub CLI and repository

   Run: `gh auth status`

   Run: `git rev-parse --is-inside-work-tree`

// turbo

1. Fetch PR details and review comments

   Run: `gh pr view <PR_NUMBER> --json number,title,url,reviewDecision,reviews,comments,files`

   This provides:
   - PR metadata (number, title, URL)
   - Review decision (APPROVED, CHANGES_REQUESTED, etc.)
   - Review comments with file/line context
   - General PR comments
   - Changed files list

// turbo

1. Parse review feedback into categories

   Categorize feedback as:
   - **Blockers**: Must fix before merge (from CHANGES_REQUESTED reviews)
   - **Suggestions**: Recommended improvements
   - **Questions**: Need clarification or discussion
   - **Nits**: Minor polish items

   For each item, extract:
   - File path and line number (if applicable)
   - Reviewer name
   - Comment text
   - Category/severity

// turbo

1. Export structured feedback summary

   Run: `gh pr view <PR_NUMBER> --comments > .tmp/pr-feedback/comments.txt`

   Run: `gh pr diff <PR_NUMBER> --name-only > .tmp/pr-feedback/changed-files.txt`

1. Create action plan

   For each feedback item, determine:
   - What needs to change (specific files/lines)
   - Implementation approach
   - Priority (Blocker → Suggestion → Nit)
   - Estimated complexity (Simple/Medium/Complex)

   Document plan in `.tmp/pr-feedback/action-plan.md`:

   ```markdown
   # PR Feedback Action Plan

   **PR**: #<number> - <title>
   **URL**: <pr-url>
   **Review Decision**: <decision>

   ## Blockers (Must Fix)

   - [ ] **Item 1** - <file>:<line>
     - Reviewer: @username
     - Comment: <feedback>
     - Action: <what to do>

   ## Suggestions (Recommended)

   - [ ] **Item 2** - <file>:<line>
     - Reviewer: @username
     - Comment: <feedback>
     - Action: <what to do>

   ## Questions (Need Discussion)

   - [ ] **Item 3**
     - Reviewer: @username
     - Comment: <question>
     - Response: <your response or "Reply on PR">

   ## Nits (Polish)

   - [ ] **Item 4** - <file>:<line>
     - Comment: <feedback>
     - Action: <what to do>
   ```

1. Ensure you're on the PR branch

   Run: `git branch --show-current`

   If not on the PR branch:

   Run: `gh pr checkout <PR_NUMBER>`

// turbo

1. Review current working tree status

   Run: `git status --porcelain`

   If there are uncommitted changes, either:
   - Commit them with `/git-commit` if they're part of addressing feedback
   - Stash them with `git stash` if unrelated

1. Address feedback items sequentially

   For each item in priority order (Blockers → Suggestions → Nits):

   a. **Read the relevant code context**
   - View the file and surrounding lines
   - Understand the current implementation

   b. **Implement the change**
   - Make the requested modification
   - Follow project conventions and style
   - Add tests if needed

   c. **Mark as completed** in action plan

   d. **Stage changes incrementally**

   Run: `git add <modified-files>`

1. For questions that need discussion

   Reply directly on the PR:

   Run: `gh pr comment <PR_NUMBER> --body "<your-response>"`

   Or reply to specific review comments:

   Run: `gh pr review <PR_NUMBER> --comment --body "<response-text>"`

// turbo

1. Commit addressed feedback

   Group related changes into logical commits:

   Run: `git commit -m "fix(<scope>): address PR feedback - <brief-summary>" -m "<detailed-changes>"`

   Example:

   ```
   git commit -m "fix(python): address PR review feedback" -m "- Add integration tests for ingestion pipeline
   - Fix type hint for ParsedDocument.file_path
   - Add docstring examples for embedding generator
   - Update error handling in retrieval module

   Addresses feedback from @reviewer in PR #123"
   ```

// turbo

1. Run validation checks
   - Format: `npm run format` (if applicable)
   - Lint: `npm run lint:md`, `ruff check python/`, etc.
   - Tests: `pytest python/tests/` or relevant test command
   - Build: Verify the project still builds/runs

// turbo

1. Push updates to PR branch

   Run: `git push`

   This automatically updates the PR with your changes.

// turbo

1. Add a summary comment to the PR

   Run: `gh pr comment <PR_NUMBER> --body "**Feedback Addressed** ✅

   Updated the PR to address review feedback:
   - ✅ Fixed <item-1>
   - ✅ Improved <item-2>
   - ✅ Added <item-3>
   - 💬 Responded to <question>

   All blockers resolved. Ready for re-review."`

// turbo

1. Request re-review (optional)

   Run: `gh pr edit <PR_NUMBER> --add-reviewer <reviewer-username>`

1. Clean up temporary artifacts

   Run: `node -e "const fs=require('fs'); fs.rmSync('.tmp/pr-feedback', {recursive:true, force:true}); console.log('cleaned .tmp/pr-feedback');"`

## Advanced: Batch addressing multiple PRs

If you have multiple PRs to address:

```bash
# List your open PRs
gh pr list --author @me --state open

# For each PR, run this workflow
# Workflow handles one PR at a time for better focus
```

## Tips

- **Focus on blockers first**: Address CHANGES_REQUESTED items before suggestions
- **Ask for clarification**: If feedback is unclear, reply on PR before implementing
- **Keep commits atomic**: Group related feedback into logical commits
- **Test thoroughly**: Run full test suite before pushing
- **Be responsive**: Acknowledge all feedback, even if you disagree (explain why)
- **Link to docs**: Reference specs/ADRs when explaining design decisions

## Common Feedback Patterns

### "Add tests for X"

- Identify the module/function that needs testing
- Create test file if needed: `tests/test_<module>.py`
- Add unit tests covering edge cases
- Verify coverage increased

### "Fix type hints"

- Review the function signature
- Add missing type hints for parameters and return values
- Use `Optional`, `Union`, `Literal` where appropriate
- Run `mypy` to verify

### "Improve error handling"

- Identify failure modes
- Add try/except blocks with specific exceptions
- Chain exceptions with `from e`
- Add telemetry/logging for failures
- Update docstrings with Raises section

### "Add documentation"

- Check if docstrings exist and are complete
- Add examples to docstrings if helpful
- Update README if public API changed
- Add inline comments for complex logic

### "Performance concern"

- Add benchmarks or timing tests
- Profile the code if needed
- Consider algorithmic improvements
- Add telemetry to measure in production

## Notes

- This workflow uses GitHub CLI extensively; ensure it's installed and authenticated
- The workflow supports both review comments (file-specific) and general PR comments
- Commits are pushed immediately; use feature flags if changes need gradual rollout
- Large refactors from feedback should be discussed before implementing

---

**Status after completion**: PR updated with addressed feedback, ready for re-review
