# Runtime architecture

This document describes the runnable projects and their connections from the repository's host and release
configuration. A project reference is not a network connection, and a runnable project is not necessarily a
separately deployed container.

## Deployment boundary

[Release](../.github/workflows/release.yml) extracts the `ChurchBulletin.UI` package, builds the root
[Dockerfile](../Dockerfile), pushes its versioned image to Azure Container Registry, and creates an Octopus
release. The container starts `dotnet ClearMeasure.Bootcamp.UI.Server.dll`; it is not an AppHost or Worker
container. The Dockerfile exposes ports 8080 and 80; deployed ingress, resource names, replica counts and
environment-specific connections are platform configuration, not established by `EXPOSE`.

Release also pushes `ChurchBulletin.Database` and the Linux x64 `ChurchBulletin.AcceptanceTests` package to
Octopus. Deployment execution and environment promotion belong to the Octopus deployment process. This
workflow does not publish separate Worker, McpServer or LlmGateway images. In particular, MCP is co-hosted in
UI.Server, and LlmGateway is a class library, not an independent LLM proxy service.

The live Azure Container Apps topology (resource groups, container apps, database, supporting resources, and
arrows with request counts) is maintained outside this repository. The platform's health dashboard regenerates
it from the system definition and the environments' resources at every deployment of the dashboard:

- prod: https://blue-bush-041023510.1.azurestaticapps.net/#runtime/prod (also `#runtime/tdd`, `#runtime/uat`)
- files on the same site: `/runtime/<env>.svg`, `/runtime/<env>.puml`, `/runtime/<env>.json`; the node list is `/topology.json`

Use that view for deployed resource identities and current routing; the connections below describe the
application's capabilities, not a claim that every executable runs in every environment.

## Application hosts and connections

### UI.Server: the released web process

[ServerApplication](../src/UI/Server/ServerApplication.cs) hosts the Blazor WebAssembly files, Razor pages,
controllers from UI.Server and the UI.Api library, work-order gRPC, embedded MCP, and background services
such as `AutoReformatAgentService`. Local launch URLs are `https://localhost:7174` and
`http://localhost:5174` ([launch settings](../src/UI/Server/Properties/launchSettings.json)); deployed callers
use the platform's ingress URL.

- **Inbound:** browsers fetch the UI and call `/api/*`, including POST `/api/blazor-wasm-single-api` for
  remoted requests/events. WebSocket clients subscribe to real-time notifications at `/ws/notifications`. gRPC clients call
  `workorders.WorkOrders` using HTTP/2. MCP clients call `/mcp` over HTTP. Probes and monitoring call
  `/health`, `/alive`, `/_healthcheck`, `/_healthcheck/detailed`, `/_version`, `/_telemetry` and `/_build`.
- **Outbound:** in-process `IBus`/MediatR handlers use EF Core to read/write the configured database.
  NServiceBus endpoint `UI.Server` uses SQL Server transport in the `nServiceBus` schema to publish domain
  events. Configured AI clients call Azure OpenAI over HTTPS. Application-chat tool discovery makes an HTTP
  loopback call to this same process's `/mcp`; tool execution goes through its handlers to the database.
  Telemetry exporters run only when their configuration is present (see below).

Sources: [service registry](../src/UI/Server/UIServiceRegistry.cs),
[single API controller](../src/UI/Server/Controllers/SingleApiController.cs),
[distributed bus](../src/DataAccess/Messaging/DistributedBus.cs),
[notification WebSocket middleware](../src/UI/Server/Notifications/RealtimeNotificationWebSocketMiddleware.cs),
[MCP tool provider](../src/McpServer/ToolProvider.cs).

### UI.Client: browser-hosted execution

[UI.Client](../src/UI/Client/Program.cs) runs Blazor WebAssembly in the user's browser after UI.Server serves
its static assets; it is not a second server container.

- **Inbound:** browser navigation, user interaction, downloaded assets and HTTP responses.
- **Outbound:** its `HttpClient` uses the hosting site's base address. `RemotableBus` forwards
  `IRemotableRequest`/`IRemotableEvent` messages through the HTTP publisher to UI.Server; ordinary
  non-remotable messages dispatch locally. Registered health checks use server remoting and, when configured,
  the Azure OpenAI chat client. The server offers a notification WebSocket endpoint, but this client bootstrap
  does not wire a WebSocket subscriber. The browser does not connect directly to SQL or NServiceBus transport.

Sources: [client registry](../src/UI/Client/UIClientServiceRegistry.cs),
[client remoting bus](../src/UI/Client/RemotableBus.cs),
[HTTP publisher](../src/UI/Client/PublisherGateway.cs).

### Worker: background message process

[Worker](../src/Worker/Program.cs) is a generic host running `WorkOrderEndpoint`, not a web host. Its
NServiceBus endpoint is `WorkOrderProcessing`; it runs work-order message handlers and AI work-order sagas.

- **Inbound:** NServiceBus messages delivered through SQL Server transport; there is no configured HTTP
  listener or browser ingress.
- **Outbound:** SQL-backed queues, saga/subscription persistence and outbox use the configured SQL connection
  and `nServiceBus` schema. Its `IBus` is a remoting HTTP client, not a local EF handler bus: requests and
  events POST to `RemotableBus:ApiUrl` on UI.Server. `ChatClientFactory` retrieves its AI configuration through
  that bus and can then call the configured Azure OpenAI endpoint. Service-default telemetry is conditional.

[WorkOrderEndpoint](../src/Worker/WorkOrderEndpoint.cs) requires both the SQL connection and
`RemotableBus:ApiUrl`. [Worker remoting](../src/Worker/Messaging/RemotableBus.cs) defines the HTTP hop.
AppHost can start Worker locally, but the release workflow above does not deploy a Worker image. Worker
requires SQL Server transport; the UI's SQLite/LearningTransport fallback does not make Worker SQLite-capable.

### McpServer: optional standalone host, also shared by UI.Server

[McpServerApplication](../src/McpServer/McpServerApplication.cs) exposes work-order, employee, team and vendor
tools and reference resources. The released UI.Server registers these tools/resources in its own HTTP host.
The standalone executable provides another hosting option; it is not started by AppHost or separately
published by this repository's release workflow.

- **Inbound:** standalone mode defaults to MCP over standard input/output from a parent MCP client. Passing
  `--http` or setting `Transport=http` selects HTTP `/mcp` at the configured ASP.NET Core listening URL.
  Embedded mode receives HTTP MCP requests on UI.Server's base URL, including loopback AI tool discovery.
- **Outbound:** standalone tools use their own `IBus`/MediatR and EF Core database connection. Its
  `NullDistributedBus` deliberately does not publish NServiceBus events; it does not host the UI's message
  endpoint. Embedded tools instead use UI.Server's registered services. Standalone logging goes to the
  console as structured JSON; its bootstrap does not register the full service-default telemetry exporters.

Sources: [MCP registry](../src/McpServer/McpServiceRegistry.cs),
[MCP database configuration](../src/McpServer/DatabaseConfiguration.cs),
[loopback endpoint resolver](../src/McpServer/McpEndpointResolver.cs).

## Stores and AI boundary

`ConnectionStrings:SqlConnectionString` supplies the database connection (environment-variable form:
`ConnectionStrings__SqlConnectionString`). UI.Server and standalone McpServer use it through EF Core;
Worker uses it for transport and message persistence. Work-order domain data and attachments' metadata are
database-owned. The SQL database is shared with **app2-ui**; the `ChurchBulletin.Database` package owns
schema migrations, and app2 does not migrate it. This documentation change alters neither schema nor data.

For local builds/tests without SQL Server, [DataContext](../src/DataAccess/Mappings/DataContext.cs) supports
SQLite and UI.Server selects NServiceBus LearningTransport for a `Data Source=` connection. This local
fallback is not evidence that the deployed store is SQLite or that Worker uses it.

[LlmGateway](../src/LlmGateway/LlmGateway.csproj) contains chat handlers, tools, health checks and
[ChatClientFactory](../src/LlmGateway/ChatClientFactory.cs); it has no executable entry point, listening port
or released container. Callers invoke it in-process. The current factory builds an Azure OpenAI SDK client
from `AI_OpenAI_ApiKey`, `AI_OpenAI_Url` and `AI_OpenAI_Model`; the network hop is from the calling host to
that configured provider, not to a separate gateway container. Configuration presence and provider health
govern AI availability. Application chat uses the embedded MCP loopback path for its tool calls; work-order
chat also has in-process tool wrappers that dispatch via `IBus`. The factory does not configure an Ollama
client merely because older documentation mentions one.

## Local orchestration and operational executables

### ChurchBulletin.AppHost

The actual AppHost path is [src/ChurchBulletin.AppHost/AppHost.cs](../src/ChurchBulletin.AppHost/AppHost.cs),
not `src/AppHost`. It declares an external connection string named `SqlConnectionString` and starts exactly
`UI_Server` as `ui-server` and `Worker` as `worker`, passing that connection reference to both.

- **Inbound:** a developer launches the host; its local Aspire dashboard/resource services receive browser
  monitoring and configured telemetry from child hosts.
- **Outbound:** it launches/manages the two child processes and supplies the SQL reference. It does not
  provision SQL, start standalone McpServer, or add a separate LLM resource. Worker still needs its
  `RemotableBus:ApiUrl` configured separately; AppHost does not wire an HTTP reference from Worker to UI.Server.

AppHost is development orchestration, not the entry point of the released Docker image.

### Database CLI

[Database/Program.cs](../src/Database/Program.cs) is the short-lived schema-management executable packaged as
`ChurchBulletin.Database`.

- **Inbound:** a command-line invocation (`baseline`, `rebuild` or `update`), options and SQL script files;
  no HTTP listener.
- **Outbound:** DbUp connects to SQL Server, applies or journals the selected migrations, and records schema
  versions. It returns a process exit code and writes upgrade logs. The release publishes the package for
  deployment tooling to run; it is not a long-running application container.

### Test runners

The remaining runnable solution projects are tooling, not deployed user-facing services:

| Project | Inbound | Outbound |
| --- | --- | --- |
| `UnitTests` | `dotnet test`/test adapter invocation | In-process unit and bUnit execution, stubs and isolated test resources; no application ingress. |
| `IntegrationTests` | `dotnet test`/build invocation | Database connections (SQL Server or local SQLite as configured), migrations and in-process HTTP/MCP test hosts; no independently deployed service. |
| `AcceptanceTests` | Local/CI test invocation or the package run by deployment tooling | Playwright browsers, HTTP/WebSocket/MCP calls to `ApplicationBaseUrl`, health probes and test setup. Its fixture can start UI.Server and, when enabled with SQL Server, Worker; SQLite mode skips Worker. |

Sources: [solution inventory](../src/ChurchBulletin.sln), [build packaging](../build.ps1),
[acceptance fixture](../src/AcceptanceTests/ServerFixture.cs). `Core`, `DataAccess`, `UI.Api`, `UI.Shared`,
`LlmGateway` and `ChurchBulletin.ServiceDefaults` are libraries loaded into the relevant hosts, not separate
runtime processes.

## Telemetry connections

[ServiceDefaults](../src/ChurchBulletin.ServiceDefaults/Extensions.cs) registers OpenTelemetry and structured
JSON console logging for UI.Server and Worker. `APPLICATIONINSIGHTS_CONNECTION_STRING` enables export to
Azure Monitor/Application Insights; `OTEL_EXPORTER_OTLP_ENDPOINT` enables OTLP export (normally to Aspire
locally). Both can be active, and neither is required to start the app. `OTEL_SERVICE_NAME` and
`OTEL_RESOURCE_ATTRIBUTES` label the exporting host. UI.Server's `/_telemetry` exposes in-process counters
without an exporter. These are outbound observability connections, not new work-order services.

## Architecture diagrams

This folder holds the application's own architecture: domain model, state, command workflows, component
dependencies and AI chat sequences.

Removed on 2026-10-11 (still in git history): `runtime-c4.puml`, `runtime-c4.png`, `runtime-c4-deployment.png` and
`arch-c4-container-deployment.*`. They described the upstream bootcamp's own Azure resources and pipeline
(`churchbulletingithubacr`, `bootcamp-tdd/uat/prod`), not this system. When taking an upstream change that touches
those files, keep this removal.
