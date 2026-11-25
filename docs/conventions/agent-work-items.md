# Agent work items: scope, issues, docs-only and already-implemented items, feature loop

Read when picking up, creating or updating a GitHub work item, when an item is docs-only or says the feature is
already implemented, or when running the feature loop. Moved from `AGENTS.md`, `CLAUDE.md` and
`.cursor/rules/cloud-agent-instructions.mdc`.

## Scope of work (cloud agents)

Only perform work within these responsibilities. Anything outside this scope requires explicit user confirmation
first.

| Responsibility | Description |
|---|---|
| Repository knowledge | Answer questions about the codebase |
| Issue tracking | Answer questions about existing GitHub issues |
| Issue management | Create and update issues (protocol below) |
| Issue implementation | Work on issues by updating code and opening PRs |

## Documentation-only work items

When the Technical Design says **README edits only** (or any docs-only change with no code, tests or build scripts):

- Make the minimal text change requested.
- Skip `dotnet build` / `dotnet test`: nothing to compile.
- Still merge `origin/master` before committing and verify `git diff --name-only origin/master...HEAD` lists only
  documentation files.
- Do NOT add test files even if the Test Design suggests them; docs-only diffs stay docs-only.

## "Feature already implemented" work items

When the body says the feature is **already fully implemented** and lists files:

1. Verify the files exist (`find src -name "<ExpectedFile>.cs"`) and match the spec.
2. `dotnet build src/ChurchBulletin.sln --configuration Release` (warnings are errors) and
   `dotnet test src/UnitTests --filter "FullyQualifiedName~ClassName"`.
3. Go straight to quality gates, self-tuning, merge and PR. Do NOT re-create files that already exist and match the
   spec.

## Creating GitHub issues (interactive)

Conduct a sequential interview, one question at a time; do not ask for all requirements upfront. If several answers
arrive at once, acknowledge them and skip to the next unanswered item.

| Step | Question | Condition |
|---|---|---|
| 1 | Description of the change | Always |
| 2 | UX requirements (or "N/A" if backend-only) | Always |
| 3 | Requires authorized user/client? | Always |
| 4 | Required roles (RBAC) | Only if step 3 = Yes |
| 5 | Resource ownership rules | Only if step 3 = Yes |
| 6 | Validation rules | Always |

Then generate a non-technical Markdown summary (no implementation details) and request confirmation before creating
the issue:

```markdown
# Conceptual Definition
**Goal:** [Summary of Goal]
**UX:** [UX Requirements]
**Authorization:** [Roles / Rules / "None (Public)"]
**Validation:** [Validation Rules]
**Acceptance Criteria:** [Derived from conversation]
```

## Updating issue bodies from scripts (GitHub REST API)

Shell variables are not visible to child processes unless **exported**. `VAR='...'` followed by
`python3 -c "... os.environ.get('VAR') ..."` yields an empty value and can PATCH the issue with only a heading; the API
still returns 200.

1. **Pass prose reliably**: `export VAR='...'` before invoking Python; or define the text inside the script /
   heredoc; or pipe it via stdin / a temp file.
2. **Verify content, not only status**: after the PATCH, `GET` the issue (or read the PATCH response) and confirm the
   section (e.g. **User Experience Design**) has substantive text. If verification fails, do **not** post "complete"
   comments or apply workflow labels; retry the update.
3. **Order**: automation labels (e.g. ready-to-move) run only after content verification succeeds.

## Feature loop

Work items live on the ClearMeasureLabs project board: https://github.com/orgs/ClearMeasureLabs/projects/1

- `/feature-loop N`: drive ONE work item across the board end to end (design → implement → verify, one column at a
  time, CI-verified merge). Cursor: `.cursor/skills/feature-loop/SKILL.md` (command
  `.cursor/commands/feature-loop.md`); Claude Code: `.claude/skills/feature-loop/SKILL.md`.
- `/feature-loop-dispatch N1 N2 ...`: orchestrate a batch: children-first tree resolution, one sub-session per item,
  epic clamp, stall watchdog. Cursor: `.cursor/skills/feature-loop-dispatch/SKILL.md` (command
  `.cursor/commands/feature-loop-dispatch.md`; Task `best-of-n-runner`, `model: inherit`, `resume`); Claude Code:
  `.claude/skills/feature-loop-dispatch/SKILL.md`.
- Board/build configuration (columns, cached board IDs, gates): `.claude/factory-loop.json` (shared).
- Stall watchdog script (shared): `.claude/skills/feature-loop-dispatch/Check-StalledLanes.ps1`.
- Board columns: [board-columns.md](../board-columns.md).
