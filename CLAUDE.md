# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Work Order management system. .NET 10.0, Onion Architecture, Blazor WebAssembly + Server UI, EF Core 10, SQL Server, MediatR for CQRS, Lamar DI, deployed to Azure Container Apps.

**Solution:** `src/ChurchBulletin.sln`

## Build and Test

```powershell
# Full local build (compile + unit tests + DB migration + integration tests)
. .\build.ps1 ; Build

# Build only
dotnet build src/ChurchBulletin.sln --configuration Release

# Unit tests
dotnet test src/UnitTests --configuration Release

# Single unit test by name
dotnet test src/UnitTests --configuration Release --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Integration tests (requires SQL Server — LocalDB on Windows, Docker container on Linux, SQLite fallback)
dotnet test src/IntegrationTests --configuration Release

# Acceptance tests (Playwright — install browsers first)
pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install
dotnet test src/AcceptanceTests --configuration Debug

# Single acceptance test
dotnet test src/AcceptanceTests --configuration Debug --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

**Run locally:** `cd src/UI/Server && dotnet run` → `https://localhost:7174` (health: `/_healthcheck`)

**Telemetry:** OpenTelemetry, wired in `src/ChurchBulletin.ServiceDefaults/Extensions.cs`. In Azure the platform sets `APPLICATIONINSIGHTS_CONNECTION_STRING` and `OTEL_SERVICE_NAME`, and the Azure Monitor OpenTelemetry exporter sends traces, metrics and logs to Application Insights. Locally, running `src/ChurchBulletin.AppHost` sets `OTEL_EXPORTER_OTLP_ENDPOINT` and the OTLP exporter sends to the Aspire dashboard. No connection string or instrumentation key in the repository.

## Onion Architecture (Strict)

Dependency flow is inward only. Violations will break the build.

- **Core** (`src/Core/`) → NO project references. Domain models, interfaces, query objects.
- **DataAccess** (`src/DataAccess/`) → references Core only. EF Core context, MediatR handlers.
- **UI layer** (`src/UI/Server/`, `src/UI/Client/`, `src/UI/Api/`, `src/UI.Shared/`) → outer layer.
- **Database** (`src/Database/`) → DbUp migrations, independent of application layers.

## Request Flow (CQRS)

Every operation flows through MediatR via the `IBus` abstraction:

```
User → Blazor UI → API Controller → IBus.Send(query/command)
  → MediatR → Handler (in DataAccess/) → DataContext (EF Core) → SQL Server
```

**Queries:** Defined in `src/Core/Queries/` (e.g., `WorkOrderByNumberQuery`). Handlers in `src/DataAccess/Handlers/`.

**State Commands:** Defined in `src/Core/Model/StateCommands/`. Each command implements `IStateCommand` and mutates the domain model. Flow: `StateCommandHandler` → `command.Execute(workOrder)` → `DataContext.SaveChangesAsync()`.

Work order state transitions: Draft → Assigned → InProgress → Complete, with InProgress → Assigned for shelving and cancellation from Assigned or either InProgress substate. InProgress contains active work and WaitingOnVendor (`WaitingOnVendorSince` null or non-null); waiting and resuming do not change the persisted status. Complete and Cancelled are terminal. See `arch/arch-state-workorder.md` for the state diagram and `arch/WorflowFor*.md` for sequence diagrams.

## Domain Model

- **WorkOrder**: Number (unique, `IX_WorkOrder_Number`), Title, Description, Instructions, RoomNumber, Building (optional string), Priority (WorkOrderPriority), DueDate, Status (WorkOrderStatus), Creator/Assignee (Employee), AssignedDate, CreatedDate, CompletedDate, WaitingOnVendorSince, Attachments. Methods: `ChangeStatus()`, `CanReassign()`, `GetDueDateUrgency()`, `IsOverdue()`
- **WorkOrderPriority**: enum Low = 1, Medium = 2 (default), High = 3; stored as tinyint
- **DueDateUrgency**: non-persisted, read-time enum None = 0, DueToday = 1, Overdue = 2
- **Work Order Search queries**: `WorkOrderSpecificationQuery` (creator, assignee, status, overdue, priority, building, open only, completed within N days, free text; newest first), `WorkOrderSearchSummaryQuery` (summary card and nav badge counts), `WorkOrderBuildingsQuery` (distinct buildings), `BuildingRoomSummaryQuery` (per building and room: open, overdue, completed in the last 30 days and total work orders, grouped by the database with the search's trimming and collation and the summary cards' predicates, so each count equals the matching search), `RecentWorkOrdersQuery` (dashboard recent activity: the last N work orders by their latest created, assigned or completed date). The search page keeps its filters, sort, page and view (table, or a board with a column per status) in the URL (`WorkOrderSearchUrlState`)
- **Employee**: UserName, FirstName, MiddleName (optional), LastName, EmailAddress, PreferredLanguage, Roles. Methods: `GetFullName()`, `CanCreateWorkOrder()`, `CanFulfillWorkOrder()`
- **WorkOrderStatus**: Smart enum — None (empty sentinel), Draft, Assigned, InProgress, Complete, Cancelled; Code, Key, FriendlyName, SortBy. WaitingOnVendor is an InProgress substate, not an enum value. Factory methods: `FromCode()`, `FromKey()`
- **Role**: Name, CanCreateWorkOrder, CanFulfillWorkOrder
- **Team**: Name, Description (optional), Members (Employee), MemberCount. Method: `AddMember()`
- **Vendor**: Name, Trade, ContactName, EmailAddress, PhoneNumber (optional contact fields), IsActive (defaults to true)
- **SavedSearchView**: Id, EmployeeId, Name, Query, CreatedDate; a named search owned by an employee
- **WorkOrderStatusChange**: WorkOrderId, FromStatus, ToStatus, ChangedById/ChangedBy (Employee), ChangedDate; status history
- **WorkOrderAttachment**: Id, WorkOrderId, FileName, ContentType, FileSize, UploadedById/UploadedBy (Employee), UploadedDate; attachment metadata
- **EntityBase<T>**: Guid Id and identity equality; base for WorkOrder, Employee, Role, Team, Vendor and WorkOrderStatusChange. SavedSearchView and WorkOrderAttachment declare their own Id

The entity/supporting-type inventory and associations are shown in `arch/arch-c4-class-domain-model.puml` (PNG/SVG) and `arch/arch-c4-class-domain-model.md`. Building is a nullable WorkOrder string, not an entity. Team membership is not a WorkOrder assignment; Vendor has no direct WorkOrder association. WeatherForecast is a DTO, and JSON converters, constants, messages, events and command/result records are not entities.

## Database Migrations

DbUp scripts in `src/Database/scripts/Update/`, numbered sequentially (`###_Description.sql`).
- Use TABS for indentation in SQL scripts.
- To add a migration: find the highest existing number, increment by 1.
- Apply locally by running `Build`.

## Key Conventions

**Architecture rules (violations are auto-rejected in PR review):**
- Do NOT add NuGet packages or change SDK versions without explicit approval
- Do NOT modify `.octopus/`, build scripts, or pipeline files without approval
- Strictly maintain onion dependency rules

**Testing:**
- Framework: NUnit 4.x with Shouldly assertions (NOT FluentAssertions, NOT Assert.That)
- Test doubles: prefix with `Stub` (NOT `Mock`)
- Pattern: AAA (Arrange, Act, Assert) without section comments
- Test naming: `[MethodName]_[Scenario]_[ExpectedResult]`, prefixed with `Should` or `When`
- Test data generation: AutoBogus
- UI component tests: bUnit
- Acceptance tests: Playwright with helpers from `AcceptanceTestBase` (`LoginAsCurrentUser()`, `Click()`, `Input()`, `Select()`)
- Live-LLM tests: mark with `[LlmTest]` (from `IntegrationTests/TestSupport`) instead of `[Retry(n)]`. The test gets 3 attempts; if none pass it is reported as a **warning**, not a build failure. Rate-limit `Assert.Ignore` and `Inconclusive` results pass through unchanged.

**Code style:**
- PascalCase for classes/methods, camelCase for variables
- XML documentation on public APIs
- Nullable reference types enabled
- File-scoped namespaces (`namespace Foo.Bar;`) — no block namespace braces
- Use `null!` instead of `default!` for `[Inject]` and similar fields
- Lambda parameters must not shadow outer-scope variables — rename to avoid `VariableHidesOuterVariable`
- Remove `$` prefix from string literals that contain no `{}` interpolation
- Use `nameof(…)` instead of `.ToString()` on enum values in Razor/HTML attributes
- Pattern-match merge: prefer `x is T { Prop: value }` over `x is T y && y.Prop == value`
- `nameof(T<>.Member)` is valid in C# 13+ — no need for a concrete type argument inside `nameof`

**Response style:**
- No anthropomorphizing — no "I", "me", "you", "we", "us"
- Terse, direct statements. Say "Checking this file" not "Let me check this file"

## Dependencies

**.NET SDK:** 10.0.100 (see `global.json`)

**Core** — FluentValidation 11.11.0, MediatR.Contracts 2.0.1, Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions 10.0.0, Microsoft.Extensions.Logging.Abstractions 10.0.0

**DataAccess** — MediatR 12.4.1, Microsoft.EntityFrameworkCore 10.0.0, Microsoft.EntityFrameworkCore.SqlServer 10.0.0, Microsoft.EntityFrameworkCore.Sqlite 10.0.0, NServiceBus.Persistence.Sql.TransactionalSession 8.3.0, ClearMeasureLabs.HostedEndpoint.SqlServerTransport 1.0.30, Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions 10.0.0, Microsoft.Extensions.Logging.Abstractions 10.0.0, SQLitePCLRaw.bundle_e_sqlite3 2.1.12, System.Security.Cryptography.Xml 10.0.11

**Database** — DbUp 5.0.41, dbup-sqlserver 6.0.16, JetBrains.Annotations 2025.2.2, Spectre.Console 0.54.0, Spectre.Console.Cli 0.53.0

**UI.Server** — Asp.Versioning.Http 8.1.0, FluentValidation.DependencyInjectionExtensions 11.11.0, Asp.Versioning.Mvc 8.1.0, Google.Protobuf 3.27.0, Grpc.AspNetCore 2.70.0, Lamar.Microsoft.DependencyInjection 15.0.1, Microsoft.AspNetCore.Components.WebAssembly.Server 10.0.0, ModelContextProtocol 1.0.0, ModelContextProtocol.AspNetCore 1.0.0, NServiceBus.Extensions.Hosting 3.0.1, OpenTelemetry.Instrumentation.AspNetCore 1.15.2, OpenTelemetry.Instrumentation.Http 1.15.1, OpenTelemetry.Instrumentation.Runtime 1.15.1, OpenTelemetry.Instrumentation.SqlClient 1.15.2

**UI.Client** — BlazorMvc 2.1.1, MediatR 12.4.1, Lamar.Microsoft.DependencyInjection 15.0.1, Microsoft.AspNetCore.Components.WebAssembly 10.0.0, Microsoft.AspNetCore.Components.WebAssembly.DevServer 10.0.0, Microsoft.AspNetCore.Components.Authorization 10.0.0, Microsoft.Extensions.Diagnostics.HealthChecks 10.0.0, Toolbelt.Blazor.SpeechRecognition 1.0.0, Toolbelt.Blazor.SpeechSynthesis 11.0.0

**UI.Api** — Asp.Versioning.Abstractions 8.1.0, Lamar.Microsoft.DependencyInjection 15.0.1

**UI.Shared** — BlazorMvc 2.1.1, MediatR 12.4.1, Microsoft.AspNetCore.Components.Authorization 10.0.0, Microsoft.AspNetCore.Components.Web 10.0.0, Toolbelt.Blazor.SpeechRecognition 1.0.0, Toolbelt.Blazor.SpeechSynthesis 11.0.0

**LlmGateway** — Azure.AI.OpenAI 2.1.0, Azure.Core 1.46.1, MediatR 12.4.1, Microsoft.Extensions.AI 9.7.0, Microsoft.Extensions.AI.OpenAI 9.7.1-preview.1.25365.4, Microsoft.Extensions.AI.Abstractions 9.7.1, Microsoft.Extensions.Diagnostics.HealthChecks 10.0.0, Microsoft.Extensions.Logging.Console 10.0.0

**McpServer** — ModelContextProtocol 1.0.0, ModelContextProtocol.AspNetCore 1.0.0, Lamar.Microsoft.DependencyInjection 15.0.1, MediatR 12.4.1, Microsoft.EntityFrameworkCore.SqlServer 10.0.0, Microsoft.EntityFrameworkCore.Sqlite 10.0.0, SQLitePCLRaw.bundle_e_sqlite3 2.1.12, OpenTelemetry.Instrumentation.AspNetCore 1.15.2, OpenTelemetry.Instrumentation.Http 1.15.1, OpenTelemetry.Instrumentation.Runtime 1.15.1, OpenTelemetry.Instrumentation.SqlClient 1.15.2

**Worker** — ClearMeasureLabs.HostedEndpoint.SqlServerTransport 1.0.30, Microsoft.Extensions.Hosting 10.0.2

**ChurchBulletin.AppHost** — MessagePack 2.5.302

**ChurchBulletin.ServiceDefaults** — Azure.Monitor.OpenTelemetry.AspNetCore 1.6.0, OpenTelemetry.Api 1.15.3, OpenTelemetry.Exporter.Console 1.15.3, OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.3, OpenTelemetry.Extensions.Hosting 1.15.3, OpenTelemetry.Instrumentation.AspNetCore 1.15.2, OpenTelemetry.Instrumentation.EntityFrameworkCore 1.0.0-beta.12, OpenTelemetry.Instrumentation.Http 1.15.1, OpenTelemetry.Instrumentation.Runtime 1.15.1, OpenTelemetry.Instrumentation.SqlClient 1.15.2, Microsoft.Extensions.Http.Resilience 9.9.0, Microsoft.Extensions.ServiceDiscovery 9.5.0, Serilog.AspNetCore 9.0.0, Serilog.Formatting.Compact 3.0.0

**UnitTests** — NUnit 4.3.2, NUnit3TestAdapter 5.0.0, NUnit.Analyzers 4.9.2, Shouldly 4.3.0, bunit 2.9.0, AutoBogus.Conventions 2.13.1, MediatR 12.4.1, coverlet.msbuild 6.0.4, coverlet.collector 6.0.4, AngleSharp 1.7.2, Microsoft.AspNetCore.Components.Authorization 10.0.11, Microsoft.AspNetCore.Mvc.Testing 10.0.0, Microsoft.NET.Test.Sdk 17.14.1, Serilog.Formatting.Compact 3.0.0, Toolbelt.Blazor.SpeechRecognition 1.0.0, Toolbelt.Blazor.SpeechSynthesis 11.0.0

**IntegrationTests** — NUnit 4.3.2, NUnit3TestAdapter 5.0.0, NUnit.Analyzers 4.9.2, Shouldly 4.3.0, Microsoft.EntityFrameworkCore 10.0.0, Microsoft.Extensions.Hosting 10.0.2, coverlet.msbuild 6.0.4, coverlet.collector 6.0.4, Grpc.Net.Client 2.70.0, Microsoft.AspNetCore.Mvc.Testing 10.0.0, Microsoft.NET.Test.Sdk 17.14.1

**AcceptanceTests** — NUnit 4.3.2, NUnit3TestAdapter 5.0.0, NUnit.Analyzers 4.9.2, microsoft.playwright.nunit 1.54.0, Azure.AI.OpenAI 2.1.0, ModelContextProtocol 1.0.0, Microsoft.Extensions.AI 9.7.0, Microsoft.Extensions.AI.OpenAI 9.7.1-preview.1.25365.4, coverlet.collector 6.0.4, coverlet.msbuild 6.0.4, Microsoft.NET.Test.Sdk 17.14.1, Microsoft.Extensions.Hosting 10.0.2, OpenTelemetry.Instrumentation.AspNetCore 1.15.2, OpenTelemetry.Instrumentation.Http 1.15.1, OpenTelemetry.Instrumentation.Runtime 1.15.1, OpenTelemetry.Instrumentation.SqlClient 1.15.2

## API Tools Endpoints

Utility endpoints in `src/UI/Api/Controllers/` — all anonymous, rate-limited, no DB access:

| Route | Method | Description |
|-------|--------|-------------|
| `/api/tools/hash` | POST | SHA-256 of `text` (UTF-8); optional MD5/SHA-1 via `includeMd5`/`includeSha1` flags |
| `/api/tools/base64` | POST | Base64 encode/decode of `text` (UTF-8); `mode` `encode` (default) or `decode` |
| `/api/tools/guid-generator` | POST | Generate 1–100 UUIDs; optional `count` query param |
| `/api/tools/random` | GET | Generate plain-text random values; required `type` query param: `number`, `string` (12 alphanumeric characters), `uuid`, or `color` (`#RRGGBB`) |
| `/api/tools/slugify` | POST | Convert `text` to URL slug (lowercase; non-alphanumeric runs become single `-`; at most 80 characters, never ending in `-`) |
| `/api/tools/case` | POST | Convert JSON `text` to the case named by `to` (`upper`, `lower`, `title`, `kebab`, or `snake`); an unknown `to` returns 400 |
| `/api/tools/timestamp-converter` | GET | Convert Unix seconds to/from ISO-8601; supply exactly one query param, `unix` or `iso`; returns Unix seconds, ISO-8601 UTC, and a human-readable UTC string |
| `/api/tools/due-date-check` | GET | Return due-date urgency for required `date` query param (`YYYY-MM-DD`), relative to today in America/Chicago, assuming an open work order |
| `/api/tools/word-count` | POST | Count whitespace-delimited words in JSON body field `text`; returns `{ "words": N, "characters": N, "lines": N }` |
| `/api/tools/uppercase` | POST | Upper-case JSON `text` with invariant culture; empty or missing `text` returns `{ "text": "" }` |
| `/api/tools/work-order-statuses` | GET | List work order statuses (`code`, `key`, `friendlyName`, `sortBy`) in sort order |

All routes also available under the versioned prefix `/api/v1.0/tools/`.

## Feature Flags

Runtime feature flag status endpoint — read-only, no DB access, no MediatR:

| Route | Method | Description |
|-------|--------|-------------|
| `/api/features/flags` | GET | Returns `{ "FlagName": bool, ... }` for all flags |

Also available as `/api/v1.0/features/flags`.

**Catalog:** `src/UI/Api/FeatureFlagsCatalog.cs` — static `IReadOnlyDictionary<string,bool>`. Add/remove flags there only.
**Controller:** `src/UI/Api/Controllers/FeatureFlagsController.cs` — calls `ConditionalGetEtag.JsonContent(FeatureFlagsCatalog.All)`.
**Helper:** `src/UI/Api/ConditionalGetEtag.cs` — serializes with `JsonSerializerDefaults.Web` and returns `ContentResult`; reuse for any read-only GET that returns JSON.

Pattern: no `IBus`, no query, no handler — pure static data. API-key middleware guards automatically. Rate-limited by `ApiRateLimiting.PolicyName`.

## Build Facts

What the deployed process was built from and how good that build is (the demo-environment kit's CAP-079); anonymous,
no DB access, no MediatR, outside `/api`:

| Route | Method | Description |
|-------|--------|-------------|
| `/_build` | GET | `build-facts.json` of the content root. `Access-Control-Allow-Origin: *`, `Cache-Control: public, max-age=300` |

**Endpoint:** `src/UI/Server/BuildFacts/` (`AddBuildFacts`, `MapBuildFacts` in `ServerApplication`). The answer always has
`version`, `commit`, `commitUrl`, `builtAt`, `buildUrl`, `code` (lines of code per language), `tests`, `coverage`,
`complexity`, `crap`, `analysis`, each null when the build could not tell. Without the file (a local run) it has the
version of the assembly and nulls.

**Script:** `scripts/Write-BuildFacts.ps1` writes the file: the code from the checkout (`git ls-files`, non-blank
lines), the rest from the Build run's artifacts (`test-results-linux`, `test-results-acceptance`,
`code-coverage-linux`, `crap-metrics-linux`, `qodana-report`); a missing artifact is a null section, never a failed
release. The Release workflow (the kit's `release.yml`, step "Stamp the build facts into the image's files") runs it
with `-OutputPath built/build-facts.json` before `docker build`, so the image carries the file at its content root
(`/app/build-facts.json`; the Dockerfile copies `built/` to `/app`).
Locally: `pwsh -NoProfile -File scripts/Write-BuildFacts.ps1`.

## Live Telemetry

The process's own counts of the last minute, for a health dashboard in another origin (the demo-environment kit's
CAP-077). Counted in-process, whether or not a telemetry exporter is configured:

| Route | Method | Description |
|-------|--------|-------------|
| `/_telemetry` | GET | Rolling 60 s counts: requests (Front Door / direct / errors / p95), probes, SQL commands (during requests / background / p95), outgoing HTTP calls; and `process`: the vitals of the process. Anonymous, outside `/api`, `Access-Control-Allow-Origin: *`, `Cache-Control: no-store` |
| `/api/work-orders/status-counts` | GET | Work-order count per status through `IBus` → `WorkOrderCountByStatusQuery` → EF Core: representative read traffic for the dashboard's traffic button. Anonymous (a public leaf in `ApiPublicPathRules`), not rate-limited, `no-store`. Also `/api/v1.0/work-orders/status-counts` |

**Code:** `src/UI/Server/LiveTelemetry/` (`AddLiveTelemetry`, `LiveTelemetryMiddleware`, `MapLiveTelemetry` in
`ServerApplication`); `src/UI/Api/Controllers/WorkOrderStatusCountsController.cs`.

**Numbers only.** `/_telemetry` answers counters, timings and process figures: no path, no query string, no header,
no user name, no SQL text, no host or connection detail. Keep it that way: any origin may read it without signing in.

Request classes (`RequestClassifier`): header `X-FD-HealthProbe: 1` → Front Door probe; `/_*` (except `/_framework`,
`/_content`), `/health`, `/alive` → probe; otherwise traffic, through Front Door when `X-Azure-FDID` is present.

`sql`: `perMinute` = `requests` (commands executed while an HTTP request was being handled, traffic or probe: the
flow `LiveTelemetryMiddleware` marks with `RequestFlow`) + `background` (the process's own: NServiceBus SQL-transport
polling, hosted services); `p95Ms` covers both. The source is Microsoft.Data.SqlClient's diagnostic events
(`DependencyCallListener`), so SQLite commands (tests, local fallback) are not counted.

`http`: completed outgoing HTTP calls (the runtime's `System.Net.Http` meter); telemetry export is left out.

`process`: `cpuPercent` (share of all processors over the last sampling interval: at least 2 s, sampled when a
request completes or the endpoint is read, never per request), `workingSetMb`, `gcHeapMb`, `threads` (thread pool),
`inFlight` (requests executing, the reading one included), `exceptionsPerMinute` (requests that ended in an unhandled
exception: escaped the pipeline, or answered by the exception handler; not first-chance exceptions) and
`uptimeSeconds` (since `startedAt`).

One process counts for itself: with more than one instance, an answer is one instance's numbers.

## DI and Service Wiring

Lamar container configured in `src/UI/Server/UIServiceRegistry.cs`. Assembly scanning auto-registers MediatR handlers and services. The `IBus` interface wraps MediatR's `IMediator`.

## Branch Naming

Format: `{username}/{branch-description}`. AI agents use the username of the account that initiated the session. When on a branch, add/commit/push automatically without asking.

## Quality Gates

| When | Command |
|------|---------|
| Before commit | `.\privatebuild.ps1` (or `. .\build.ps1 ; Build`) |
| Before PR | `.\acceptancetests.ps1` |
| Docs-only changes | Skip builds |

CRAP gate (crap4dotnet): `pwsh scripts/crap/run-crap-audit.ps1 -SkipTests -FailOnViolations`. Threshold in `scripts/crap/crap-gate-threshold.json`; reports land in `crap-metrics/` (gitignored) and CI publishes them as the `crap-metrics-linux` artifact plus job summary. See `docs/crap-score-audit.md`.

**Qodana static analysis:** findings are tracked in `qodana.sarif.json`. When fixing Qodana P1 mechanical cleanups, read the SARIF to get the exact snippet context — SARIF line numbers may differ from the current file when the file has changed since the baseline scan. Use `python3 -c "import json; ..."` to extract the `contextRegion.snippet` for each finding. After all fixes, run `dotnet build ... -warnaserror` (0 warnings required) before committing.

**Qodana baseline refresh:** After a remediation batch is merged and CI has run a full Qodana scan, download the new `qodana.sarif.json` artifact from the CI run and replace the file at the repo root. Do NOT manually edit the fingerprints — always replace from a real scan output. Removing a finding from the baseline WITHOUT fixing it causes `failThreshold: 0` to fire (ABSENT counts as a new finding). Safe workflow: fix code → commit → CI green → download artifact → replace `qodana.sarif.json` → commit baseline.

**Left-baselined by design (~75 findings):** The following inspection IDs are intentionally retained in `qodana.sarif.json`. Fixing them would require mass rewrites with negative ROI. Do NOT remove these baseline entries without first fixing the underlying code.

| Inspection ID | Count | Reason left |
|---|---|---|
| `ConvertToPrimaryConstructor` | 26 | Mass rewrite; style only; breaks constructor-injection readability |
| `AutoPropertyCanBeMadeGetOnly.Global` | 11 | NHibernate / STJ / test-object-init requires mutable setters |
| `UseNameOfInsteadOfToString` | 4 | Minor; fix batch TBD |
| `PropertyCanBeMadeInitOnly.Local` | 4 | NHibernate/STJ set-by-convention requires mutable setters |
| `MergeIntoPattern` | 3 | Style; low value |
| `MethodSupportsCancellation` | 3 | Requires API-surface change; deferred |
| `ParameterOnlyUsedForPreconditionCheck.Local` | 3 | Style; low value |
| `PreferConcreteValueOverDefault` | 3 | Style; low value |
| `ArrangeNamespaceBody` | 3 | Formatter style; no semantic change |
| `AsyncMethodWithoutAwait` | 2 | Override stubs required by interface contracts |
| `FieldCanBeMadeReadOnly.Global` | 2 | Reflection/NHibernate access |
| `RedundantUsingDirective` | 2 | Already excluded in prior batches; residual |
| `VariableHidesOuterVariable` | 2 | Style; low value |
| `InconsistentlySynchronizedField` | 1 | Deferred to concurrency review |
| `PartialTypeWithSinglePart` | 1 | Blazor codegen pattern |
| `RedundantStringInterpolation` | 1 | Style; trivial |
| `RedundantTypeArgumentsInsideNameof` | 1 | Style; trivial |
| `RedundantVerbatimStringPrefix` | 1 | Style; trivial |
| `TooWideLocalVariableScope` | 1 | Style; low value |
| `UsingStatementResourceInitialization` | 1 | Style; low value |

The remaining baselined findings (`InconsistentNaming` ×4, `MemberCanBePrivate.Global` ×2, `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` ×1, `Html.AttributeValueNotResolved` ×1, `ReturnTypeCanBeNotNullable` ×1, `CSharpWarnings::CS0618` ×1) are targeted in dedicated sibling work items and will be removed from the baseline when those fixes are merged.

## Feature Loop

Work items live on the ClearMeasureLabs project board: https://github.com/orgs/ClearMeasureLabs/projects/1

- `/feature-loop N` — drive ONE work item across the board end-to-end (design → implement → verify, one column at a time, CI-verified merge).
  - **Cursor:** `.cursor/skills/feature-loop/SKILL.md` (slash command: `.cursor/commands/feature-loop.md`)
  - **Claude Code:** `.claude/skills/feature-loop/SKILL.md`
- `/feature-loop-dispatch N1 N2 ...` — orchestrate a batch: children-first tree resolution, one sub-session per item, epic clamp, stall watchdog.
  - **Cursor:** `.cursor/skills/feature-loop-dispatch/SKILL.md` (slash command: `.cursor/commands/feature-loop-dispatch.md`); uses Task `best-of-n-runner`, `model: inherit` (Auto, cost-optimized), `resume`
  - **Claude Code:** `.claude/skills/feature-loop-dispatch/SKILL.md`
- Board/build configuration (columns, cached board IDs, gates): `.claude/factory-loop.json` (shared)
- Stall watchdog script (shared): `.claude/skills/feature-loop-dispatch/Check-StalledLanes.ps1`

## Further Reference

- Architecture diagrams: `arch/` (C4 PlantUML + Mermaid)
- Copilot standards: `.github/copilot-instructions.md`
- PR review rules: `.github/copilot-code-review-instructions.md`
- Codebase structure: `.cursor/rules/codebase-structure.mdc`
