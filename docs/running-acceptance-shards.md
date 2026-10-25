# Running Acceptance Test Shards Locally

Acceptance tests run in shards to speed up CI. The CI pipeline splits tests across **3 shards** (matrix `[1, 2, 3]`), with 2 job variants for a total of 6 job runs: `acceptance-tests` on ubuntu with SQL Server container, and `acceptance-tests-arm` on ubuntu-24.04-arm with SQLite. This document shows how to run shards locally.

## How CI Shards Tests

Each shard receives a filter from `Get-ShardTestFilter` in `build.ps1`:

- **Shard 0:** All tests (no filter) — for full-suite runs
- **Shard 1:** `WorkOrderSearchTests`, `WorkOrderSpeechTests` (~27 tests)
- **Shard 2:** `WorkOrderSearchRedesignTests`, `WorkOrderDueDateTests`, `WorkOrderDictationTests`, `WorkOrderSaveDraftTests`, `AIAgents` (~34 tests)
- **Shard 3:** `Api`, `Extensions`, `NServiceBus`, and small WorkOrder classes (~31 tests), by exclusion of shards 1–2

The CI workflow verifies full coverage with the `acceptance-tests-coverage-check` job.

## Prerequisites

- .NET 10.0 SDK
- PowerShell 7+
- Browser drivers: Run `. ./build.ps1 ; Invoke-AcceptanceTests -ShardNumber 0` once to install Playwright Chromium
- Database:
  - **Windows:** LocalDB (included with Visual Studio or SQL Server Developer Edition)
  - **Linux with Docker:** Docker and Docker daemon running
  - **Linux without Docker:** SQLite (automatic fallback)
- Application server and worker running locally (optional; tests start them if not running)

## Run One Shard Locally

### Windows (LocalDB)

```powershell
. ./build.ps1
Invoke-AcceptanceTests -ShardNumber 1
```

Optionally override the database server:

```powershell
. ./build.ps1
Invoke-AcceptanceTests -ShardNumber 1 -databaseServer ".\SQLEXPRESS" -databaseName "ChurchBulletin_local"
```

### Linux with Docker (SQL Server Container)

```powershell
. ./build.ps1
Invoke-AcceptanceTests -ShardNumber 1
```

The script detects Docker and creates a SQL Server container automatically. To connect to an external SQL Server instead, set `SQL_EXTERNAL=true` and `SQL_SERVER_HOST`:

```powershell
$env:SQL_EXTERNAL = "true"
$env:SQL_SERVER_HOST = "sql-host,1433"
. ./build.ps1
Invoke-AcceptanceTests -ShardNumber 1
```

### Linux without Docker (SQLite Fallback)

```powershell
. ./build.ps1
Invoke-AcceptanceTests -ShardNumber 1 -UseSqlite
```

Or force SQLite on any platform:

```powershell
$env:DATABASE_ENGINE = "SQLite"
. ./build.ps1
Invoke-AcceptanceTests -ShardNumber 1
```

## Run All Shards or a Different Order

### Run All Shards Sequentially

```powershell
. ./build.ps1
Invoke-AcceptanceTests -ShardNumber 1
Invoke-AcceptanceTests -ShardNumber 2
Invoke-AcceptanceTests -ShardNumber 3
```

### Run a Single Test by Name

Use the `dotnet test` filter syntax directly. For a single test:

```powershell
dotnet test src/AcceptanceTests --configuration Release --filter "FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSearchTests.TestMethodName"
```

### Run All Tests in a Class

```powershell
dotnet test src/AcceptanceTests --configuration Release --filter "FullyQualifiedName~ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSearchTests"
```

### Run Tests by Category (Smoke Test Example)

```powershell
dotnet test src/AcceptanceTests --configuration Release --filter "TestCategory=Smoke"
```

### Run a Shard with a Different Order

To run tests in a different order (e.g., randomized), use `dotnet test` directly with the shard filter. First, get the shard filter from `Get-ShardTestFilter`:

```powershell
. ./build.ps1
$filter = Get-ShardTestFilter -ShardNumber 1
dotnet test src/AcceptanceTests --configuration Release --filter "$filter"
```

## Reading Results

### Test Results Files (TRX)

Results are written to `build/test/**/*.trx` (NUnit TRX format). Each shard produces its own TRX file:

```
build/test/AcceptanceTests/TestResults_<guid>.trx
```

Parse or view in Visual Studio Test Explorer or with the dotnet-trx test reporter.

### CI Artifacts

CI publishes results under these artifact names (2-day retention):

- `test-results-acceptance-linux-shard-1`, `-shard-2`, `-shard-3` (ubuntu, SQL Server)
- `test-results-acceptance-arm-shard-1`, `-shard-2`, `-shard-3` (ubuntu-24.04-arm, SQLite)

### Screenshots

Screenshots are captured on test failure via `TakeScreenshotAsync` in test code and attached to the test result. They are skipped by default (for speed) unless `SkipScreenshotsForSpeed` is set to `false` in `appsettings.acceptancetests.json`:

```json
{
  "SkipScreenshotsForSpeed": "false"
}
```

When enabled, screenshots are written to the working directory as `<TestClass>-<TestMethod>-<step><guid>.png`.

### Playwright Traces

Playwright browser traces are recorded on test failure and written to the NUnit work directory under `playwright-traces/`. Use Playwright Inspector to view:

```bash
npx playwright show-trace playwright-traces/<trace>.zip
```

## Database Engine Resolution

The database engine is resolved automatically based on platform and capabilities via `Get-ResolvedDatabaseEngine` in `BuildFunctions.ps1`:

| Platform | Docker Available | Engine | Connection |
|----------|------------------|--------|------------|
| Windows  | N/A              | LocalDB | `(LocalDb)\MSSQLLocalDB` |
| Linux    | Yes              | SQL-Container | `localhost` (or `$SQL_SERVER_HOST`) |
| Linux    | No               | SQLite | `ChurchBulletin.db` |

Override with `DATABASE_ENGINE` environment variable:

```powershell
$env:DATABASE_ENGINE = "SQLite"
```

Or via `-UseSqlite` switch to `Invoke-AcceptanceTests`:

```powershell
. ./build.ps1
Invoke-AcceptanceTests -ShardNumber 1 -UseSqlite
```

## Test Data Isolation

Each test is isolated from others and runs independently. Tests do NOT share state and must NOT depend on the execution order of other tests.

### Per-Test Isolation

Each test gets its own:
- Browser and browser context (no shared cookies or local storage)
- Page instance
- User account (created at test start)
- **Test tag:** A unique GUID prefix (visible in test code as `TestTag`)

The test tag filters data to the current test only.

### Data Creation and Cleanup

Tests create their own work orders via the application API or direct database calls:

```csharp
var workOrder = await CreateAndSaveNewWorkOrder(
    title: "Test work order",
    priority: WorkOrderPriority.High,
    testTag: TestTag  // Unique per test
);
```

Work orders created in the test are filtered by `testTag` so only this test sees them. After the test completes, the test's database connection closes and the database is reset for the next shard run (or reused unchanged for a full-suite local run).

### Rules for Test Data

- **Create isolated data:** Each test creates its own work orders, users, or seed data via `CreateAndSaveNewWorkOrder`. Never rely on another test's rows or a specific test execution order.
- **Never mutate shared seed data:** Do not modify seeded work orders (e.g., demo data) that other tests depend on. Create new ones instead.
- **Filter by test tag:** Queries within a test must filter by `TestTag` so concurrent shards do not interfere.
- **No cross-test dependencies:** Tests must pass when run in any order or in isolation.

## Parameters and Headful Mode

### AcceptanceTests.ps1 Parameters

```powershell
AcceptanceTests.ps1 `
    -ShardNumber <int>          # Shard 0–3 (0 = all tests)
    -ShardCount <int>           # Total shard count (default 6)
    -databaseServer <string>    # Override database server
    -databaseName <string>      # Override database name
    -Headful                    # Run with visible browser windows (default headless)
```

### Run with Visible Browser

```powershell
.\AcceptanceTests.ps1 -ShardNumber 1 -Headful
```

### AcceptanceTests.runsettings

Test parallelization is configured in `src/AcceptanceTests/AcceptanceTests.runsettings`:

```xml
<NUnit>
  <NumberOfTestWorkers>4</NumberOfTestWorkers>
</NUnit>
```

The value (4) matches the core count of CI runners to avoid browser launch oversubscription.

## Troubleshooting

### "Server is already running" Warning

If you run acceptance tests with a server already in the background, you may see:

```
Warning: ClearMeasure.Bootcamp.UI.Server is already running in background (PID: ####).
This may interfere with acceptance tests.
```

Stop the background process or use a different port. Acceptance tests start their own server if needed.

### Database Connection Failures

- **LocalDB not found (Windows):** Install SQL Server Developer Edition or Visual Studio with LocalDB, or explicitly pass `-databaseServer`.
- **Docker not running (Linux):** Start Docker daemon, or pass `-UseSqlite` to fall back to SQLite.
- **Port conflicts:** If SQL Server container startup fails, ensure port 1433 is available.

### Playwright Browser Failures

If browser installation fails:

```powershell
pwsh src/AcceptanceTests/bin/Release/net10.0/playwright.ps1 install chromium --with-deps
```

### Timeout or Flaky Tests

- Increase NUnit `NumberOfTestWorkers` if some tests hang (resource contention).
- Run one shard at a time: `-ShardNumber 1` (isolates each shard's database).
- Check browser process count: too many concurrent browsers may exhaust system resources.

---

**No schema change.**
