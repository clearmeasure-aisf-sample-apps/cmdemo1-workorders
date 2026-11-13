# AGENTS.md

See `CLAUDE.md` for full project overview, solution structure, architecture, coding standards, and build/test commands.

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
| Qodana (Community .NET) | `docker run --rm -v "$PWD":/data/project jetbrains/qodana-cdnet:latest --solution src/ChurchBulletin.sln --baseline qodana.sarif.json --fail-threshold 0` | Zero findings outside the baseline `qodana.sarif.json`. A baselined finding your change removes (ABSENT) also fails: delete its entry (see the Qodana sections below) |
| CRAP | `pwsh scripts/crap/run-crap-audit.ps1 -SkipTests -FailOnViolations` | Under `scripts/crap/crap-gate-threshold.json` |
| Acceptance tests | `pwsh ./AcceptanceTests.ps1` (Playwright, 6 shards in CI) | All pass |
| NuGet package gate (own check, `nuget-package-gate.yml`) | `pwsh scripts/nuget/Test-NewNuGetPackages.ps1 -BaseRef $(git merge-base origin/master HEAD)` | No new package ID in `src/Directory.Packages.props` or a project file, unless approved in `scripts/nuget/approved-new-packages.txt` |

The usual first-push failures, so check them before pushing:

- **Qodana**: unused members/types/parameters (`UnusedMember.*`, `UnusedType.*`, `UnusedParameter.*` gate the build),
  redundant `using`, `$` on strings without interpolation, lambda parameters that shadow outer variables. Fix them,
  or suppress by-design ones on one line: `// ReSharper disable once <InspectionId> -- <reason>`. Never add new
  entries to `qodana.sarif.json` to get green.
- **Size ratchet**: never add a line to the baseline for new code. Split by concept, not by line count: read
  `docs/refactoring-oversized-code.md` and fill in the PR's **Refactoring plan** when a baselined file changes.

Architecture rules are build errors and tests, not review comments: Onion project references (`ONION001`), the
ArchUnitNET layer tests in `src/UnitTests/Architecture`, namespace matches folder (`IDE0130`), no system clock in
Core (`RS0030`), and Central Package Management (versions only in `src/Directory.Packages.props`). See
`docs/architecture-guardrails.md`.

Pull requests: branch `{username}/{short-description}`; the body says `Closes #N`; use the template in
`.github/pull_request_template.md`; one concern per PR; squash merge. Do not add NuGet packages, change SDK versions or
edit `.octopus/`, build scripts or workflows without approval (`CLAUDE.md`, Key Conventions).

## Cursor Cloud specific instructions

### System Dependencies

- **.NET SDK 10.0.100** (prerelease) - installed via `dotnet-install.sh`
- **PowerShell 7 (pwsh)** - required for all build scripts (`build.ps1`, `PrivateBuild.ps1`, `BuildFunctions.ps1`)
- **Docker** - required for SQL Server container on Linux; needs `fuse-overlayfs` storage driver and `iptables-legacy` in the cloud VM

### Size-gate failures and oversized code

On a size-gate failure, a structure-guardrail failure (`src/UnitTests/BuildGates/Refactoring/`) or a Refactoring
rationale failure (workflow `refactoring-rationale.yml`), **read and follow `docs/refactoring-oversized-code.md`
before editing**. Diagnose the smell and the emerging concept, use a preferred move (never `partial`, helper/util
classes, own-type extensions, numbered names or line-count splits), give every extracted class its own fixture, write
tests as visible procedural steps, delete the fixed line from the baseline, and fill in the pull request's
**Refactoring plan** section: the rationale table, a Before and an After `mermaid` diagram (`classDiagram` or
`flowchart`) per concept cluster naming every new production type, and the line-count table pasted from
`pwsh scripts/refactoring/Test-RefactoringRationale.ps1 -BaseRef origin/master -WriteLineCountTable` (regenerate after
every push). Check locally:
`pwsh scripts/refactoring/Test-RefactoringRationale.ps1 -BaseRef origin/master -PrBodyPath pr-body.md`.

### Running the Full Build

```bash
pwsh -NoProfile -ExecutionPolicy Bypass -File ./PrivateBuild.ps1
```

This runs clean, restore, compile, unit tests, Docker SQL Server setup, DB migration, and integration tests. It auto-detects the database engine (SQL-Container on Linux with Docker).

### Server logging

UI.Server, Worker, and standalone McpServer emit **structured logs as JSON lines** on the standard console (Serilog `RenderedCompactJsonFormatter`), suitable for container log aggregation (for example Azure Container Apps). Levels and namespace overrides are driven by the `Serilog` section in each host’s `appsettings*.json`.

### Running the Application

The `launchSettings.json` contains a Windows-only LocalDB connection string that crashes on Linux. To run the app on Linux, bypass the launch profile and set environment variables manually:

```bash
export ConnectionStrings__SqlConnectionString="server=localhost,1433;database=ChurchBulletin;User ID=sa;Password=churchbulletin-mssql#1A;TrustServerCertificate=true;"
export ASPNETCORE_ENVIRONMENT=Development
export AI_OpenAI_ApiKey=""
export AI_OpenAI_Url=""
export AI_OpenAI_Model=""
cd src/UI/Server && dotnet run --no-launch-profile --urls "https://localhost:7174;http://localhost:5174"
```

Key gotchas:
- **Must use `--no-launch-profile`** to avoid the LocalDB connection string override from `launchSettings.json`.
- **No telemetry setting is needed to run locally.** Telemetry is OpenTelemetry (`src/ChurchBulletin.ServiceDefaults/Extensions.cs`). In Azure the platform sets `APPLICATIONINSIGHTS_CONNECTION_STRING` and `OTEL_SERVICE_NAME`, and the Azure Monitor OpenTelemetry exporter sends traces, metrics and logs to Application Insights. Locally, running `src/ChurchBulletin.AppHost` sets `OTEL_EXPORTER_OTLP_ENDPOINT` and the OTLP exporter sends to the Aspire dashboard. Without either variable no exporter runs and the app starts normally. The in-process counters at `/_telemetry` (`src/UI/Server/LiveTelemetry/`) work without an exporter. No connection string or instrumentation key belongs in the repository.
- **Must set `AI_OpenAI_*` vars to empty strings** to prevent the app from trying to connect to Azure OpenAI (it degrades gracefully).
- The SQL Server Docker container must already be running (created by `PrivateBuild.ps1` or manually via `docker run`). The container name is `churchbulletin-mssql` and the password is `churchbulletin-mssql#1A`.

### gRPC (work orders)

- **Contract:** `src/UI/Server/Protos/workorders.proto`. Generated C# is checked in under `src/UI/Server/Generated/Protos/` so Linux ARM64 CI avoids `Grpc.Tools` `protoc` (which can segfault on that platform). After changing the `.proto`, regenerate on an x64 machine with `Grpc.Tools` and replace those files.
- **Endpoint:** Same base URL as UI.Server (for example `https://localhost:7174`). Clients should use **HTTP/2** (TLS in development; configure ingress/proxy for HTTP/2 or h2c in production as appropriate).
- **Surface:** `workorders.WorkOrders` — `Ping` (smoke) and `GetWorkOrderByNumber` (reads via `IBus` / `WorkOrderByNumberQuery`, same path as HTTP APIs).
- **Auth:** Anonymous for the shipped RPCs (no `[Authorize]` on the service).
- **Rate limiting:** The sliding-window policy applies only to `/api/*` (and the Blazor single-API path); gRPC calls are not covered by that limiter.
- **Client example (.NET):**

```csharp
using Grpc.Net.Client;
using ClearMeasure.Bootcamp.UI.Server.Grpc;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
using var channel = GrpcChannel.ForAddress("https://localhost:7174");
var client = new WorkOrders.WorkOrdersClient(channel);
var reply = await client.GetWorkOrderByNumberAsync(
    new GetWorkOrderByNumberRequest { Number = "WO-123" });
```

### Docker Daemon

In the cloud VM, Docker needs to be started manually:

```bash
sudo dockerd &>/tmp/dockerd.log &
sleep 5
sudo chmod 666 /var/run/docker.sock
```

### Database

The build scripts auto-detect the database engine. On Linux with Docker, SQL Server 2022 runs in a container on port 1433. The container is named `churchbulletin-mssql` with password `churchbulletin-mssql#1A`. The `PrivateBuild.ps1` script handles container creation, database creation, and migration automatically.

### SQLite Fallback

If Docker is unavailable, set `DATABASE_ENGINE=SQLite` before running the build scripts. The app and integration tests will use SQLite via EF Core's `EnsureCreated`. Some integration tests tagged `SqlServerOnly` will be skipped.

### Optional Services

- **Ollama** (localhost:11434): Local LLM for AI agent features. Not required; errors in logs about Ollama connection refused are expected and harmless.
- **Azure OpenAI**: Cloud LLM alternative. Requires `AI_OpenAI_ApiKey`, `AI_OpenAI_Url`, `AI_OpenAI_Model` env vars.

### GitHub issue updates from scripts

When appending issue bodies via `python3` or other subprocesses: **export** any variable the child reads (`export VAR=...`), or embed the text in the script. Unexported shell variables appear **empty** in the child, which can produce a successful API response with **blank** content. See **GitHub REST API — Issue body updates** in `.cursor/rules/cloud-agent-instructions.mdc`.

### Quick dotnet build / test (without full PrivateBuild.ps1)

When a full PowerShell build is unnecessary (e.g. for a pre-built or docs-only feature), run the targeted sequence:

```bash
cd /workspace/src && dotnet restore ChurchBulletin.sln   # always restore first — obj/project.assets.json may be missing
dotnet build UI/Api/UI.Api.csproj
dotnet test UnitTests/UnitTests.csproj --no-build --filter "FullyQualifiedName~<ClassName>"
```

The solution file is at `src/ChurchBulletin.sln`. Run `dotnet restore` from `/workspace/src` with the `.sln` path; running it from `/workspace` fails because no `.sln` is at the root.

### Documentation-Only Work Items

When the work item's Technical Design says **README edits only** (or any docs-only change with no code, tests, or build scripts touched):
- Make the minimal text change requested.
- Skip `dotnet build` / `dotnet test` — there is nothing to compile and the quality gate is irrelevant.
- Still merge `origin/master` before committing and verify `git diff --name-only origin/master...HEAD` lists only the documentation file(s).
- Do NOT add test files even if the Test Design section suggests them; docs-only diffs must stay docs-only.

### "Feature already implemented" work items

When the work item body says the feature is **already fully implemented**, verify with:

```bash
find /workspace/src -name "<ExpectedFile>.cs" 2>/dev/null
```

If all listed files exist and compile cleanly, the only remaining tasks are quality gates, self-tuning, merge, and PR — no code to write.

### Gotchas

- NServiceBus runs in trial mode (no license). This produces a warning at startup but does not block functionality.
- The HTTPS dev certificate is untrusted. Browser interactions require clicking through the security warning.
- The `appsettings.Development.json` has a LocalDB connection string; on Linux, always override via the `ConnectionStrings__SqlConnectionString` environment variable or use the build scripts which handle this automatically.

### bUnit test patterns for WorkOrderSearch

When adding a new `[Inject]` property to `WorkOrderSearch` (or any `AppComponentBase` component), **all** bUnit tests for that component must register the matching service or they will throw at render time. For `AuthenticationStateProvider`, use `CustomAuthenticationStateProvider` with a `StubUserSessionStore`:

```csharp
var store = new StubUserSessionStore { Username = "someuser" };
var authProvider = new CustomAuthenticationStateProvider(store);
authProvider.Login("someuser").GetAwaiter().GetResult();
ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);
```

**Prefer a `CreateContext()` factory helper** in the test class over repeating the service registrations in every test. Pass the `IBus` override as a parameter when a test needs a custom stub:

```csharp
private static BunitContext CreateContext(IBus? bus = null, string loggedInAs = "defaultuser")
{
    var ctx = new BunitContext();
    ctx.Services.AddSingleton(bus ?? new StubBus());
    ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
    ctx.Services.AddSingleton(TimeProvider.System);
    // ... add AuthenticationStateProvider
    return ctx;
}
```

**bUnit checkbox state:** Use `element.HasAttribute("checked")` to assert whether a checkbox is checked — `IElement` does not have an `IsChecked()` method.

**`@bind-Value:after` callback pattern:** Use `@bind-Value:after="HandlerMethod"` on an `<InputCheckbox>` to trigger async side-effects after the binding updates. The handler runs after the value is bound, so it sees the new value and can drive secondary effects (like updating a related filter).

**`[Inject]` property visibility in Blazor components:** Declare `[Inject]` properties as `private` (not `public`) unless they need to be accessed from tests or cross-component. Blazor DI injects into `private` properties just as well as `public`. Using `private` prevents Qodana `MemberCanBePrivate.Global` findings. Example: `[Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;`. The `WorkOrderManage.razor.cs` uses this pattern with `[Inject] private NavigationManager`.


### "Feature Already Implemented" Work Items

When a work item's Technical Design section says **"The feature is fully implemented"** and lists checked-off files, verify those files exist (`find /workspace/src -name "FileName.cs"`), confirm they match the spec, run `dotnet build src/ChurchBulletin.sln --configuration Release` (warnings are already errors) and `dotnet test src/UnitTests --filter "FullyQualifiedName~ClassName"`, then proceed directly to the self-tuning/commit/PR steps. Do NOT re-create files that already exist and match the spec — doing so wastes tokens and risks introducing divergence.

### API-Layer-Only Features (Pure Utility Endpoints)

For endpoints in `src/UI/Api/Controllers/` that have no Core/Domain/DataAccess changes:
- Mirror the dual `[Route]` pattern: `"api/tools/<name>"` + `$"{ApiRoutes.VersionedApiPrefix}/tools/<name>"` (see `ToolsGuidGeneratorController`, `ToolsHashController`, `ToolsRandomController`).
- Use `[ApiVersion("1.0")]`, `[AllowAnonymous]`, `[EnableRateLimiting(ApiRateLimiting.PolicyName)]`.
- Return plain text via `ContentResult` with `ContentType = "text/plain; charset=utf-8"`.
- Return structured errors via `Problem(detail: "...", statusCode: 400)`.
- No DI needed for stateless generators — use `Random.Shared` (thread-safe) and static helpers.
- Build verification: `dotnet build src/ChurchBulletin.sln --configuration Release` — warnings are already errors.

### Qodana Static Analysis — Known Conventions

Qodana runs in CI with `failThreshold: 0`. Common P2 findings to pre-empt:

**InconsistentNaming (`non_field_members_should_use_upper_camel_case`)**
- Abbreviations in PascalCase method names must capitalize only the first letter: `EnUs` not `EnUS`, `Id` not `ID` (unless the framework demands it), etc.
- Affects test method names too — rename consistently across all test files.

**Nullability (`ReturnTypeCanBeNotNullable`, `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract`)**
- If a private/helper method always returns a non-null value, declare its return type as non-nullable (`T` not `T?`).
- Update the corresponding local variable declarations to match.

**Visibility / MemberCanBePrivate (`MemberCanBePrivate.Global`, `MemberCanBeProtected.Global`)**
- Before narrowing any property or method, run these **four** checks, in order:
  1. **Blazor `[Parameter]`** — any property decorated with `[Parameter]` or `[SupplyParameterFromQuery]` must stay `public`.  Razor templates in the same partial class can access `private` members fine; only cross-component parameters need `public`.
  2. **JSON serialization** — `System.Text.Json` (used by `WebServiceMessage` remoting) cannot populate `private set` properties during deserialization. Any property on a class that participates in the remoting round-trip (`IRemotableRequest`) must keep `public set`. Verify with the `ShouldSerialize` / `AssertRemotable` unit test in `RemotableRequestTests`.
  3. **Cross-assembly callers** — `grep -r "MemberName" src/` before narrowing. A `public static` test-helper called from `IntegrationTests` or other test projects must stay `public`; a helper only used within the same class can be `private`.
  4. **Object-initializer setters** — `new SomeType { Property = value }` syntax requires at minimum `internal set` (or `public set`); `private set` breaks this. Search for `{ Property =` across the solution before narrowing.
- For members that cannot be narrowed due to the above constraints, add a single-line suppression: `// ReSharper disable once MemberCanBePrivate.Global -- <reason>`.
- **Private field naming**: When narrowing a `public` field to `private`, rename it to `_camelCase` (underscore prefix) to satisfy `InconsistentNaming`. Update ALL references in both the `.cs` file and the `.razor` template (they are the same partial class and share the name).
- **Private auto-property setters**: When narrowing visibility, if the setter is never assigned after construction, change `{ get; set; }` to `{ get; }` to avoid `AutoPropertyCanBeMadeGetOnly.Local`. If the property was previously `protected virtual` (`.Global` in baseline), also remove that baseline entry — it becomes absent and fails the gate.
- **`virtual` keyword**: Removing `virtual` from a `protected virtual` member is often required when narrowing to `private` — `private virtual` is illegal and `private` overrides are invisible to subclasses.
- **After all visibility changes**: Update `qodana.sarif.json` baseline to remove entries for every finding that is now fixed or suppressed. Absent findings (in baseline but not in scan) count against `failThreshold: 0` just like new findings. Remove them using a Python script that filters by `partialFingerprints.equalIndicator/v1`. Keep only findings that are genuinely still present in the code (e.g., cross-assembly public helpers that cannot be narrowed).
- After all changes: `dotnet build src/ChurchBulletin.sln --configuration Release` must pass (warnings are already errors), followed by the full `UnitTests` run.

After any rename that touches test methods, verify no callers outside the file reference the old name (use `grep -r "OldName" src/`).

### Qodana P4: Dead / Unused Symbol Suppressions (`ClassNeverInstantiated.Global`, `UnusedMember.*`, etc.)

When Qodana flags symbols as dead but they are required by DI, reflection, NServiceBus, or interface contracts, prefer a single-line suppression over deletion. Use this decision tree:

1. **DI-registered validators** (FluentValidation `AbstractValidator<T>` subclasses): suppress `ClassNeverInstantiated.Global` — FluentValidation assembly-scans and DI resolves them.
2. **Assembly-anchor marker classes** (`WebApplicationFactory<TEntryPoint>`): suppress `ClassNeverInstantiated.Global`.
3. **NServiceBus message handlers** (`IHandleMessages<T>`): suppress `ClassNeverInstantiated.Global` — NServiceBus pipeline instantiates via reflection.
4. **OTel / JSON deserialization constructors** (default constructors on `EventEntry`, `MetricEntry`, etc.): suppress `UnusedMember.Global`.
5. **Blazor state-notification events** (`event Action? OnChange`): suppress `EventNeverSubscribedTo.Global` — consumers subscribe at runtime.
6. **Interface members flagged as `UnusedMemberInSuper.Global`**: suppress on the interface (not each implementor) — these are called via interface dispatch.
7. **Abstract test-base members overridden by all subclasses**: suppress `UnusedMember.Global` on the abstract declaration.
8. **IUiBus `Notify(object)` in test stubs**: suppress `UnusedMember.Global` — interface contract requires the method even though `Notify<T>` is the runtime call path.
9. **`UnusedAutoPropertyAccessor.Global` / setter never externally assigned**: change `{ get; set; }` → `{ get; }` when the class controls all mutation (private constructor + static singleton).

**fingerprint removal**: always use the full `equalIndicator/v1` value (64+ hex chars) from `qodana.sarif.json` — the Python `-c` truncation trick at 40 chars will produce partial matches that don't filter. Use `fp=r.get(...,'')` and print untruncated.

**Suppression format** (project convention):
```csharp
// ReSharper disable once ClassNeverInstantiated.Global -- registered by DI (FluentValidation assembly scan)
public sealed class MyValidator : AbstractValidator<MyType>;
```

### Qodana Baseline Refresh Workflow (for remediation work items)

When working on a Qodana baseline remediation batch (e.g., #9432 "remediate UNCHANGED findings"):

1. **Audit what's actually absent vs present before touching code.** The baseline may be stale — check each finding's `charOffset + snippet` against current source files before deciding what to fix vs just remove:
   ```python
   python3 << 'EOF'
   import json, os
   with open('/workspace/qodana.sarif.json') as f: data = json.load(f)
   for r in data['runs'][0]['results']:
       for loc in r.get('locations',[]):
           pl = loc['physicalLocation']
           uri, offset, snippet = pl['artifactLocation']['uri'], pl['region'].get('charOffset',-1), pl['region'].get('snippet',{}).get('text','')
           path = os.path.join('/workspace/src', uri)
           if os.path.exists(path) and offset >= 0:
               with open(path) as f: content = f.read()
               status = 'PRESENT' if content[offset:offset+len(snippet)]==snippet else 'ABSENT'
               print(f"{status}|{r['ruleId']}|{uri}:{pl['region'].get('startLine','?')}")
   EOF
   ```
   ABSENT entries must be **removed from the baseline** (not just left). ABSENT in baseline = penalty identical to NEW.

2. **For each PRESENT finding, choose fix or suppress:**
   - **Fix** if trivial (remove async, add CancellationToken.None, narrow scope, remove redundant prefix).
   - **Suppress** with `// ReSharper disable once <RuleId> -- <reason>` when the finding is by-design (ConvertToPrimaryConstructor, ParameterOnlyUsedForPreconditionCheck.Local, IOptions binding setters, etc.).
   - **Removing an entry from baseline without fixing or suppressing is wrong** — the next Qodana run will report it as NEW.

3. **After all code changes, rebuild the baseline** using only fingerprints that remain genuinely present AND not suppressed:
   ```python
   # Remove fps from baseline by building an explicit remove set
   remove_fps = { '<fp1>', '<fp2>', ... }  # all absent + all fixed/suppressed
   kept = [r for r in data['runs'][0]['results']
           if r.get('partialFingerprints',{}).get('equalIndicator/v1','') not in remove_fps]
   data['runs'][0]['results'] = kept
   with open('/workspace/qodana.sarif.json', 'w') as f: json.dump(data, f, indent=2)
   ```
   Do **not** compare against modified source files — compute the remove set from the pre-change audit + your own change list.

4. **`Html.AttributeValueNotResolved` at `Settings.razor:16`** is in the baseline AND excluded in `qodana.yaml`. Qodana CDnet respects `exclude` blocks in baseline comparison, so this entry is safe to leave in baseline.

5. **`ConvertToPrimaryConstructor` is an epic guardrail** — do NOT convert; suppress with `// ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion`.

6. **`AutoPropertyCanBeMadeGetOnly.Global` on IOptions classes** — properties need `set` for IConfiguration binding. Suppress: `// ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global -- required for IOptions<T> configuration binding`.

7. **`ParameterOnlyUsedForPreconditionCheck.Local` on test stubs** — constructor parameters used as `if (flag) throw` guards. Use `// ReSharper disable/restore ParameterOnlyUsedForPreconditionCheck.Local` around the class.

### Blazor Page Code-behind Pattern

When adding logic to an existing `.razor` page that has only an `@code` block:
- Create a `.razor.cs` partial class alongside the `.razor` file (e.g., `Index.razor.cs`)
- The partial class declares `public partial class PageName : AppComponentBase, IListener<Event>`
- **Do NOT add `@inherits AppComponentBase` to the `.razor` file** — it's already in `src/UI.Shared/_Imports.razor` globally
- **Do NOT add `[Route(...)]` to the `.razor.cs`** if the route is already defined via `@page "/"` in the razor template
- The `AuthorizeView` component needs `IAuthorizationPolicyProvider` and `Task<AuthenticationState>` in bunit tests — use `ctx.AddAuthorization()` (bunit helper) not `ctx.Services.AddAuthorization()`
- When adding new `IBus.Send(...)` calls to a page, add the corresponding handler to `StubBus.cs` in `UnitTests/UI.Shared/Pages/` to prevent `NotImplementedException` in existing bunit tests

### EF Core Handler Gotchas

- **`GroupBy` on value-converter properties** (like `wo.Status.Key`) **cannot be translated** to SQL. Use `.Select(wo => wo.Status).ToListAsync()` and group in memory instead.
- **SQLite in-memory unit tests**: `Data Source=:memory:` is a new DB per connection — `EnsureCreated()` schema is lost as soon as the context is disposed. Use a **temp file DB** (`Path.GetTempPath() + Guid.NewGuid() + ".db"`) in `[SetUp]` and delete it in `[TearDown]`.
- `IDatabaseConfiguration` has two members: `GetConnectionString()` and `ResetConnectionPool()` — implement both in any test stub.
- The `WorkOrderStatusConverter` stores `Code` (e.g., `DRT`) not `Key` (e.g., `Draft`) in the database. `WorkOrderStatus.FromCode(code)` converts back.

### StubBus Maintenance

`src/UnitTests/UI.Shared/Pages/StubBus.cs` is the shared stub used by all bunit page tests. Whenever a new `IRequest<TResponse>` type is added to a page's `OnInitializedAsync`, add a matching `if (request is NewQueryType) { ... return ...; }` branch before the `throw new NotImplementedException()` to prevent existing page tests from failing.
