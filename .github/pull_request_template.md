Submitter checklist
- [ ] Issue is clearly tagged
- [ ] Narrate status of the branch (feature complete, incremental build, etc)
- [ ] You expect the approval checklist to be satisfied

## Refactoring rationale

<!--
Required when this pull request changes a file on the oversized (size-gate) baseline; checked by
scripts/refactoring/Test-RefactoringRationale.ps1. One row per move. Smell and Refactoring use the names in the
Smells and Preferred moves tables of docs/refactoring-oversized-code.md (another move: "Other: <move>" plus the
reason). New abstraction is the name of the class, component, query or method the move introduced. Why: at least
8 words. Leave the table empty when no oversized file changes.
-->

| Smell | Refactoring | New abstraction | Why |
|-------|-------------|-----------------|-----|

==========================
Approver checklist
- [ ] Issue is clearly tagged
- [ ] Build and all test suites passing
- [ ] Static analysis ran and passed
- [ ] All changes delivered with accompanying tests
- [ ] Changes to libraries/dependencies expected and pre-approved
- [ ] Team coding standard adhered to
- [ ] Another item
- [ ] Another item
