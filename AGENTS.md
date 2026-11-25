# AGENTS.md

Canonical instructions for every coding agent (Claude Code imports this file from `CLAUDE.md`; Cursor, Copilot and
Codex read it directly). Situational detail lives in the on-demand docs indexed at the end.

## Project

Work Order management system. .NET 10.0 (SDK 10.0.100, `global.json`), Onion Architecture, Blazor WebAssembly +
Server, EF Core 10, SQL Server, MediatR (CQRS via `IBus`), Lamar DI, Aspire, Azure Container Apps.
Solution: `src/ChurchBulletin.sln`.

## Build and test

| Task | Command |
|---|---|
| Full private build (compile, unit tests, DB migration, integration tests) | `pwsh -NoProfile -File ./PrivateBuild.ps1` (or `. .\build.ps1 ; Build`) |
| Build only | `dotnet build src/ChurchBulletin.sln --configuration Release` |
| Unit tests | `dotnet test src/UnitTests --configuration Release` |
| One test | `dotnet test src/UnitTests --configuration Release --filter "FullyQualifiedName~TestClassName.TestMethodName"` |
| Integration tests (SQL Server: LocalDB on Windows, Docker on Linux, `DATABASE_ENGINE=SQLite` fallback) | `dotnet test src/IntegrationTests --configuration Release` |
| Acceptance tests (Playwright; browsers: `pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install`) | `pwsh ./AcceptanceTests.ps1` or `dotnet test src/AcceptanceTests --configuration Debug` |
| Run locally | `cd src/UI/Server && dotnet run` → `https://localhost:7174` (health `/_healthcheck`); Linux: see cloud-agent-setup |

- Before commit: `PrivateBuild.ps1`. Before PR: `AcceptanceTests.ps1`. Docs-only changes: skip builds.
- Warnings are errors (`TreatWarningsAsErrors`, `src/Directory.Build.props`).

## Before you push: the checks a pull request must pass

Required on every pull request to `master`: **`Build result`** (workflow `build.yml`, summarizes every gated job) and
**`secret-scan`** (gitleaks, `secret-scan.yml`). A docs-only diff (only `docs/**`, `arch/**`, `*.md`) skips the heavy
jobs and goes green in a few minutes; skip local builds for it. For any code change, run these locally first; each is
a job behind `Build result`:

| Gate | Run locally | Passes when |
|---|---|---|
| Build, unit + integration tests | `pwsh -NoProfile -File ./PrivateBuild.ps1` (or `dotnet build src/ChurchBulletin.sln --configuration Release` then `dotnet test src/UnitTests --configuration Release`) | No errors; warnings are errors (`src/Directory.Build.props`) |
| Code Analysis | `dotnet format style src/ChurchBulletin.sln --verify-no-changes --severity warn` and `dotnet format analyzers src/ChurchBulletin.sln --verify-no-changes`; `pwsh -NoProfile -File ./scripts/dead-code/Invoke-DeadCodeGates.ps1` | No changes needed, no new dead code or unused package |
| Size and nesting ratchet | `dotnet test src/UnitTests --configuration Release --filter "FullyQualifiedName~BuildGates"` | No new line in `src/UnitTests/BuildGates/size-nesting-baseline.txt` is needed: production files <= 300 lines, test files <= 600, methods <= 30 lines, <= 4 parameters, nesting <= 3. A violation you fix must also be deleted from the baseline (a stale line fails too) |
| Qodana (Community .NET) | `docker run --rm -v "$PWD":/data/project jetbrains/qodana-cdnet:latest --solution src/ChurchBulletin.sln --baseline qodana.sarif.json --fail-threshold 0` | Zero findings outside the baseline `qodana.sarif.json`. A baselined finding your change removes (ABSENT) also fails: delete its entry |
| CRAP | `pwsh scripts/crap/run-crap-audit.ps1 -SkipTests -FailOnViolations` | Under `scripts/crap/crap-gate-threshold.json` (reports in `crap-metrics/`) |
| Acceptance tests | `pwsh ./AcceptanceTests.ps1` (Playwright, 6 shards in CI) | All pass |
| NuGet package gate (own check, `nuget-package-gate.yml`) | `pwsh scripts/nuget/Test-NewNuGetPackages.ps1 -BaseRef $(git merge-base origin/master HEAD)` | No new package ID in `src/Directory.Packages.props` or a project file, unless approved in `scripts/nuget/approved-new-packages.txt` |

Usual first-push failures:

- **Qodana**: unused members/types/parameters (`UnusedMember.*`, `UnusedType.*`, `UnusedParameter.*` gate the build),
  redundant `using`, `$` on strings without interpolation, lambda parameters that shadow outer variables. Fix them, or
  suppress by-design ones on one line: `// ReSharper disable once <InspectionId> -- <reason>`. **Never add new entries
  to `qodana.sarif.json` to get green.** Never remove a baseline entry without fixing the code; refresh the baseline
  only by replacing it with a real CI scan artifact.
- **Size ratchet**: **never add a line to the baseline for new code.** Split by concept, not by line count.
- **Oversized code:** on a size-gate failure, a structure-guardrail failure (`src/UnitTests/BuildGates/Refactoring/`)
  or a Refactoring rationale failure (`refactoring-rationale.yml`), read and follow
  `docs/refactoring-oversized-code.md` before editing, and fill in the PR's **Refactoring plan** (checked by
  `scripts/refactoring/Test-RefactoringRationale.ps1`).

## Hard rules (never without explicit approval)

- Do NOT add NuGet packages or project references, change package versions, or change the .NET SDK version.
- Do NOT modify `.octopus/`, build scripts (`build.ps1`, `build.bat`, `build.sh`, `BuildFunctions.ps1`,
  `PrivateBuild.ps1`), or pipeline/workflow files.
- Never weaken, skip or delete tests or gates to get green.
- No connection string, instrumentation key or other secret in the repository (telemetry config comes from the
  platform: `APPLICATIONINSIGHTS_CONNECTION_STRING`, `OTEL_*`).
- `/_telemetry` answers numbers only: no path, query string, header, user name, SQL text, host or connection detail.

## Architecture (build errors and tests, not review comments)

- **Onion, inward only** (`ONION001`): Core → NO project references; DataAccess → Core only; UI (`src/UI/Server`,
  `src/UI/Client`, `src/UI/Api`, `src/UI.Shared`), Worker, AppHost → outer layer; Database (DbUp) is independent.
  Verify direction before adding a reference; no circular dependencies.
- ArchUnitNET layer tests in `src/UnitTests/Architecture`; namespace matches folder (`IDE0130`); no system clock in
  Core (`RS0030`; use `TimeProvider`).
- Central Package Management: versions only in `src/Directory.Packages.props` (no `Version` on a `PackageReference`,
  `VersionOverride` disabled). Details: `docs/architecture-guardrails.md`.
- Every operation goes through MediatR via `IBus`: queries in `src/Core/Queries/`, state commands
  (`IStateCommand`) in `src/Core/Model/StateCommands/`, handlers in `src/DataAccess/Handlers/`. Business logic in Core;
  UI thin.
- Feature flags: enforce only with `[RequiresFeature(...)]` on the request record (bus behavior); do not check flags in
  handlers, controllers or pages.
- DB migrations: DbUp scripts in `src/Database/scripts/Update/`, named `###_Description.sql` (highest number + 1),
  TABS for indentation.

## Code style

- PascalCase classes/methods, camelCase variables; small single-responsibility methods; nullable reference types on.
- XML documentation on public APIs (always add/update it).
- File-scoped namespaces (`namespace Foo.Bar;`); `null!` (not `default!`) for `[Inject]` and similar fields.
- Lambda parameters must not shadow outer-scope variables; no `$` on strings without `{}` interpolation.
- `nameof(…)` instead of `.ToString()` on enum values in Razor/HTML attributes; prefer `x is T { Prop: value }`;
  `nameof(T<>.Member)` is valid in C# 13+.

## Testing

- NUnit 4.x + Shouldly (NOT FluentAssertions, NOT `Assert.That`); bUnit for components; Playwright acceptance tests
  with `AcceptanceTestBase` helpers; AutoBogus for data.
- Always include unit tests for new functionality; integration and acceptance tests for new or changed behavior
  (acceptance may be skipped when UX is unchanged).
- Test doubles prefixed `Stub` (NOT `Mock`); AAA without section comments; names
  `[MethodName]_[Scenario]_[ExpectedResult]`, prefixed `Should` or `When`.
- Live-LLM tests: `[LlmTest]` (from `IntegrationTests/TestSupport`), not `[Retry(n)]`.

## Branches and pull requests

- Branch `{username}/{short-description}`; AI agents use the username of the account that started the session. If the
  branch exists, use it. When on a branch, add/commit/push without asking.
- PR body says `Closes #N` and uses `.github/pull_request_template.md`; one concern per PR; squash merge.

## Response style

No anthropomorphizing: no "I", "me", "you", "we", "us". Terse, direct statements ("Checking this file").

## On-demand docs (read when ...)

| Doc | Read when |
|---|---|
| `docs/refactoring-oversized-code.md` | A size-gate, structure-guardrail or Refactoring rationale failure; splitting classes, pages or test files |
| `docs/conventions/qodana-agent-guide.md` | Qodana fails; narrowing visibility; suppressing findings; baseline remediation or refresh; left-baselined IDs |
| `docs/architecture-guardrails.md` | Adding a reference or package; an `ONION001` / ArchUnit / `RS0030` / NuGet gate failure; requesting an exception |
| `docs/conventions/testing-patterns.md` | Writing bUnit, page, StubBus, EF Core handler, acceptance or live-LLM tests |
| `docs/conventions/cloud-agent-setup.md` | Linux / Cursor Cloud VM setup, Docker, SQL Server, SQLite, running the app on Linux, gRPC |
| `docs/conventions/codebase-reference.md` | Locating code; projects and key paths; request flow; domain model; state transitions; DI |
| `docs/conventions/dependencies.md` | Checking which packages a project already has |
| `docs/conventions/agent-work-items.md` | Docs-only or already-implemented items; creating/updating issues; scope of work; feature loop |
| `docs/feature-flags.md` | Adding, reading or enforcing a feature flag |
| `docs/api-tools.md` | Adding or changing a `/api/tools/*` or other API-layer-only endpoint |
| `docs/conventions/build-facts-and-live-telemetry.md` | Touching `/_build`, `/_telemetry`, `Write-BuildFacts.ps1` or status-counts |
| `docs/conventions/local-telemetry.md` | Telemetry / OpenTelemetry wiring |
| `docs/database-migrations.md` | Adding a DbUp migration |
| `docs/crap-score-audit.md` | CRAP gate failures |
| `.github/copilot-code-review-instructions.md` | Reviewing a pull request |
| `arch/` | Architecture diagrams (C4 PlantUML + Mermaid) |
