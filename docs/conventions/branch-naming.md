# Branch Naming and Quality Gates

## Branch names

Use `{username}/{branch-description}` for contributor branches. The username is the account creating the branch; AI agents use the
username of the account that initiated the session.

Examples:

- `jeffreypalermo/fix-work-order-status`
- `johnsmith/add-employee-search`

## Quality gates

- Before committing code changes, run `.\privatebuild.ps1` to compile and run unit and integration tests.
- Before submitting a pull request with code changes, run `.\acceptancetests.ps1` for the full system acceptance tests.
- For documentation-only changes, skip builds and tests.
