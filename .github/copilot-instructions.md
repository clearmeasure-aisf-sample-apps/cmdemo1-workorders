# Copilot instructions

All project rules, commands, gates and conventions are in [`AGENTS.md`](../AGENTS.md) (canonical; the Copilot coding
agent also reads it directly). Read it first. Review checklist: [`copilot-code-review-instructions.md`](copilot-code-review-instructions.md).

## Copilot-only rules

- **Pull request readiness (REQUIRED for the Copilot SWE agent): ALWAYS run `gh pr ready` when finished.** Do NOT
  leave PRs in draft.
  - Code files changed (`*.cs`, `*.razor`, `*.sql`, ...): `pwsh ./PrivateBuild.ps1` must pass, then
    `pwsh ./AcceptanceTests.ps1` must pass, then `gh pr ready`. On a failure: diagnose, fix, re-run both from the
    beginning; do NOT mark ready until both pass.
  - Only docs/config changed (`*.md`, `*.yml`, ...): `gh pr ready` immediately.
- **Acceptance tests from issues:** when the issue body has an "Acceptance Test Scenarios" section, implement each
  scenario in the named fixture (e.g. `WorkOrderManageTests.cs`), follow its steps with `AcceptanceTestBase` helpers
  (`LoginAsCurrentUser()`, `Click()`, `Input()`, `Select()`, `Expect()`), and run the acceptance tests. Workflow:
  feature code → unit tests → integration tests → acceptance tests → all pass → commit. Patterns:
  [`docs/conventions/testing-patterns.md`](../docs/conventions/testing-patterns.md).
- After generating code, offer to generate a test next.
- No pronouns, no personality, no follow-up questions; do not refer to the user. Say "Checking this file", not
  "Let me check this file".
