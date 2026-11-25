# Cloud agent and Linux setup

Read when setting up a Linux or Cursor Cloud VM, running the full private build, running the app on Linux, or when
Docker / SQL Server / SQLite is involved. Moved from `AGENTS.md` ("Cursor Cloud specific instructions") and
`CLAUDE.md`. See also [running-locally.md](running-locally.md) and [local-telemetry.md](local-telemetry.md).

## System dependencies

- **.NET SDK 10.0.100** (prerelease): installed via `dotnet-install.sh` (version pinned in `global.json`).
- **PowerShell 7 (pwsh)**: required for all build scripts (`build.ps1`, `PrivateBuild.ps1`, `BuildFunctions.ps1`).
- **Docker**: required for the SQL Server container on Linux; needs the `fuse-overlayfs` storage driver and
  `iptables-legacy` in the cloud VM.

## Build commands

```powershell
# Full local build (compile + unit tests + DB migration + integration tests)
. .\build.ps1 ; Build

# Individual build steps
.\build.ps1 Init             # Clean and restore
.\build.ps1 Compile          # Build solution
.\build.ps1 UnitTests        # Unit tests only
.\build.ps1 IntegrationTest  # Integration tests (requires database)
```

Full private build on Linux:

```bash
pwsh -NoProfile -ExecutionPolicy Bypass -File ./PrivateBuild.ps1
```

This runs clean, restore, compile, unit tests, Docker SQL Server setup, DB migration and integration tests. It
auto-detects the database engine (SQL-Container on Linux with Docker; LocalDB on Windows; SQLite fallback).

### Quick dotnet build / test (without PrivateBuild.ps1)

```bash
cd /workspace/src && dotnet restore ChurchBulletin.sln   # always restore first: obj/project.assets.json may be missing
dotnet build UI/Api/UI.Api.csproj
dotnet test UnitTests/UnitTests.csproj --no-build --filter "FullyQualifiedName~<ClassName>"
```

The solution file is `src/ChurchBulletin.sln`. Run `dotnet restore` from `src/` with the `.sln` path; running it from
the repository root fails because no `.sln` is at the root.

### Acceptance tests (Playwright)

```powershell
pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install   # install browsers first
dotnet test src/AcceptanceTests --configuration Debug
dotnet test src/AcceptanceTests --configuration Debug --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

## Docker daemon

In the cloud VM, Docker must be started manually:

```bash
sudo dockerd &>/tmp/dockerd.log &
sleep 5
sudo chmod 666 /var/run/docker.sock
```

## Database

- The build scripts auto-detect the database engine. On Linux with Docker, SQL Server 2022 runs in a container on
  port 1433, named `churchbulletin-mssql`, password `churchbulletin-mssql#1A` (local throwaway container only).
  `PrivateBuild.ps1` handles container creation, database creation and migration.
- **SQLite fallback:** if Docker is unavailable, set `DATABASE_ENGINE=SQLite` before running the build scripts. The
  app and integration tests use SQLite via EF Core's `EnsureCreated`. Integration tests tagged `SqlServerOnly` are
  skipped.

## Running the application on Linux

`launchSettings.json` contains a Windows-only LocalDB connection string that crashes on Linux. Bypass the launch
profile and set environment variables manually:

```bash
export ConnectionStrings__SqlConnectionString="server=localhost,1433;database=ChurchBulletin;User ID=sa;Password=churchbulletin-mssql#1A;TrustServerCertificate=true;"
export ASPNETCORE_ENVIRONMENT=Development
export AI_OpenAI_ApiKey=""
export AI_OpenAI_Url=""
export AI_OpenAI_Model=""
cd src/UI/Server && dotnet run --no-launch-profile --urls "https://localhost:7174;http://localhost:5174"
```

Windows: `cd src/UI/Server && dotnet run` → `https://localhost:7174` (health: `/_healthcheck`).

Key gotchas:

- **Must use `--no-launch-profile`** to avoid the LocalDB connection string override from `launchSettings.json`.
- **Must set `AI_OpenAI_*` vars to empty strings** so the app does not try to connect to Azure OpenAI (it degrades
  gracefully).
- The SQL Server container must already be running (created by `PrivateBuild.ps1` or manually via `docker run`).
- `appsettings.Development.json` has a LocalDB connection string; on Linux always override it via
  `ConnectionStrings__SqlConnectionString` or use the build scripts, which handle this.
- **No telemetry setting is needed to run locally.** Telemetry is OpenTelemetry
  (`src/ChurchBulletin.ServiceDefaults/Extensions.cs`). In Azure the platform sets
  `APPLICATIONINSIGHTS_CONNECTION_STRING` and `OTEL_SERVICE_NAME`, and the Azure Monitor OpenTelemetry exporter sends
  traces, metrics and logs to Application Insights. Locally, running `src/ChurchBulletin.AppHost` sets
  `OTEL_EXPORTER_OTLP_ENDPOINT` and the OTLP exporter sends to the Aspire dashboard. Without either variable no
  exporter runs and the app starts normally. The in-process counters at `/_telemetry`
  (`src/UI/Server/LiveTelemetry/`) work without an exporter. No connection string or instrumentation key belongs in
  the repository.
- NServiceBus runs in trial mode (no license): a startup warning, not a blocker.
- The HTTPS dev certificate is untrusted; browser interactions require clicking through the warning.

## Server logging

UI.Server, Worker and standalone McpServer emit **structured logs as JSON lines** on the console (Serilog
`RenderedCompactJsonFormatter`), suitable for container log aggregation (for example Azure Container Apps). Levels
and namespace overrides come from the `Serilog` section in each host's `appsettings*.json`.

## Optional services

- **Ollama** (localhost:11434): local LLM for AI agent features. Not required; "connection refused" errors about
  Ollama in logs are expected and harmless.
- **Azure OpenAI**: cloud LLM alternative. Requires `AI_OpenAI_ApiKey`, `AI_OpenAI_Url`, `AI_OpenAI_Model` env vars.

## gRPC (work orders)

- **Contract:** `src/UI/Server/Protos/workorders.proto`. Generated C# is checked in under
  `src/UI/Server/Generated/Protos/` so Linux ARM64 CI avoids `Grpc.Tools` `protoc` (which can segfault there). After
  changing the `.proto`, regenerate on an x64 machine with `Grpc.Tools` and replace those files.
- **Endpoint:** same base URL as UI.Server (for example `https://localhost:7174`). Clients use **HTTP/2** (TLS in
  development; configure ingress/proxy for HTTP/2 or h2c in production as appropriate).
- **Surface:** `workorders.WorkOrders`: `Ping` (smoke) and `GetWorkOrderByNumber` (reads via `IBus` /
  `WorkOrderByNumberQuery`, same path as the HTTP APIs).
- **Auth:** anonymous for the shipped RPCs (no `[Authorize]` on the service).
- **Rate limiting:** the sliding-window policy applies only to `/api/*` (and the Blazor single-API path); gRPC calls
  are not covered by that limiter.

```csharp
using Grpc.Net.Client;
using ClearMeasure.Bootcamp.UI.Server.Grpc;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
using var channel = GrpcChannel.ForAddress("https://localhost:7174");
var client = new WorkOrders.WorkOrdersClient(channel);
var reply = await client.GetWorkOrderByNumberAsync(
    new GetWorkOrderByNumberRequest { Number = "WO-123" });
```
