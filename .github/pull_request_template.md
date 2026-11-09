Submitter checklist
- [ ] Issue is clearly tagged
- [ ] Narrate status of the branch (feature complete, incremental build, etc)
- [ ] You expect the approval checklist to be satisfied

## Refactoring plan

<!--
Required when this pull request changes a file on the size baseline
(src/UnitTests/BuildGates/size-nesting-baseline.txt); checked by scripts/refactoring/Test-RefactoringRationale.ps1.
Delete this section when no size-baselined file changes. See docs/refactoring-oversized-code.md.

Refactoring rationale: one row per move. Smell and Refactoring use the names in the Smells and Preferred moves tables
of the playbook (another move: "Other: <move>" plus the reason). New abstraction is the class, component, query or
method the move introduced. Why: at least 8 words.

Before and after: one ```mermaid pair per extracted concept cluster, classDiagram or flowchart. The line above each
fence starts with "Before" or "After". Every type the pull request adds to production code is named in a diagram.

Line counts: generated, never typed. Paste the output of
  pwsh scripts/refactoring/Test-RefactoringRationale.ps1 -BaseRef origin/master -WriteLineCountTable
and regenerate it after every push.
-->

### Refactoring rationale

| Smell | Refactoring | New abstraction | Why |
|-------|-------------|-----------------|-----|

### Before and after

Before

```mermaid
classDiagram
```

After

```mermaid
classDiagram
```

### Line counts

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
