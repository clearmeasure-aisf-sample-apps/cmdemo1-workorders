# Codebase reference: projects, key paths, request flow, domain model, DI

Read when locating code, adding a query/command/handler, touching the domain model or work-order state transitions,
or wiring DI. Moved from `CLAUDE.md`, `.github/copilot-instructions.md`, `.cursorrules` and
`.cursor/rules/codebase-structure.mdc`. Always-on architecture rules stay in [AGENTS.md](../../AGENTS.md).

## Stack

.NET 10.0, Onion Architecture, Blazor WebAssembly + Server UI, EF Core 10, SQL Server (Azure SQL in production,
LocalDB for local Windows development), MediatR for CQRS, Lamar DI (StructureMap successor), Aspire, AI/LLM via Azure
OpenAI and Ollama, deployed to Azure Container Apps. Solution: `src/ChurchBulletin.sln`.

## Projects

| Project | Path | Role |
|---|---|---|
| Core | `src/Core/Core.csproj` | Domain models, interfaces, query objects; NO project references |
| DataAccess | `src/DataAccess/DataAccess.csproj` | EF Core `DataContext`, mappings, MediatR handlers, health checks; references Core only |
| Database | `src/Database/Database.csproj` | DbUp migrations; independent of application layers |
| UI.Server | `src/UI/Server/UI.Server.csproj` | Blazor Server host, Lamar DI, health checks |
| UI.Client | `src/UI/Client/UI.Client.csproj` | Blazor WASM |
| UI.Api | `src/UI/Api/UI.Api.csproj` | Web API endpoints |
| UI.Shared | `src/UI.Shared/UI.Shared.csproj` | Shared UI components |
| LlmGateway | `src/LlmGateway/LlmGateway.csproj` | Azure OpenAI / Ollama; references Core |
| McpServer | `src/McpServer/` | Standalone MCP server |
| ChurchBulletin.AppHost | `src/ChurchBulletin.AppHost/` | Aspire AppHost |
| ChurchBulletin.ServiceDefaults | `src/ChurchBulletin.ServiceDefaults/` | Aspire defaults, OpenTelemetry |
| Worker | `src/Worker/Worker.csproj` | Hosted endpoint worker service |
| UnitTests | `src/UnitTests/UnitTests.csproj` | NUnit + Shouldly + bUnit |
| IntegrationTests | `src/IntegrationTests/IntegrationTests.csproj` | NUnit, database |
| AcceptanceTests | `src/AcceptanceTests/AcceptanceTests.csproj` | NUnit + Playwright |

UI* projects are the outer layer; they may reference Core, DataAccess, UI.Shared and LlmGateway as needed.

## Key paths by concern

| Concern | Location |
|---|---|
| Domain models | `src/Core/Model/` |
| Queries (CQRS) | `src/Core/Queries/` (e.g. `WorkOrderByNumberQuery`, `EmployeeByUserNameQuery`, `WorkOrderSpecificationQuery`) |
| State commands | `src/Core/Model/StateCommands/`, `src/Core/Services/IStateCommand.cs` |
| Bus/MediatR | `src/Core/IBus.cs`; handlers in DataAccess and UI |
| EF context and mappings | `src/DataAccess/Mappings/` (`DataContext`, `*Map.cs`) |
| Query/command handlers | `src/DataAccess/Handlers/` |
| DB migrations (DbUp) | `src/Database/scripts/Update/` ([database-migrations.md](database-migrations.md)) |
| Blazor Server entry | `src/UI/Server/Program.cs`, `UIServiceRegistry.cs` |
| Blazor Client entry | `src/UI/Client/Program.cs`, `App.razor` |
| Worker entry | `src/Worker/Program.cs`, `WorkOrderEndpoint.cs` |
| API controllers | `src/UI/Api/Controllers/`, `src/UI/Server/Controllers/` |
| Health checks | DataAccess: `CanConnectToDatabaseHealthCheck`; LlmGateway: LLM; Server: aggregation |
| Telemetry (OpenTelemetry) | `src/ChurchBulletin.ServiceDefaults/Extensions.cs` ([local-telemetry.md](local-telemetry.md)) |
| Build/test scripts | Repository root: `build.ps1`, `PrivateBuild.ps1`, `AcceptanceTests.ps1` |
| Architecture docs | `arch/` (C4 PlantUML + Mermaid) |
| PR review rules | `.github/copilot-code-review-instructions.md` |

## Request flow (CQRS)

Every operation flows through MediatR via the `IBus` abstraction:

```
User → Blazor UI → API Controller → IBus.Send(query/command)
  → MediatR → Handler (in DataAccess/) → DataContext (EF Core) → SQL Server
```

- **Queries:** defined in `src/Core/Queries/`; handlers in `src/DataAccess/Handlers/`.
- **State commands:** defined in `src/Core/Model/StateCommands/`; each implements `IStateCommand` and mutates the
  domain model. Flow: `StateCommandHandler` → `command.Execute(workOrder)` → `DataContext.SaveChangesAsync()`.
- Work order state transitions: Draft → Assigned → InProgress → Complete, with InProgress → Assigned for shelving and
  cancellation from Assigned or either InProgress substate. InProgress contains active work and WaitingOnVendor
  (`WaitingOnVendorSince` null or non-null); waiting and resuming do not change the persisted status. Complete and
  Cancelled are terminal. See `arch/arch-state-workorder.md` for the state diagram and `arch/WorflowFor*.md` for
  sequence diagrams. More: [cqrs-request-flow.md](cqrs-request-flow.md), [request-flow.md](../request-flow.md).

## DI and service wiring

Lamar container configured in `src/UI/Server/UIServiceRegistry.cs`. Assembly scanning auto-registers MediatR handlers
and services. `IBus` wraps MediatR's `IMediator`.

## Domain model

- **WorkOrder**: Number (unique, `IX_WorkOrder_Number`), Title, Description, Instructions, RoomNumber, Building
  (optional string), Priority (WorkOrderPriority), DueDate, Status (WorkOrderStatus), Creator/Assignee (Employee),
  AssignedDate, CreatedDate, CompletedDate, WaitingOnVendorSince, Attachments. Methods: `ChangeStatus()`,
  `CanReassign()`, `GetDueDateUrgency()`, `IsOverdue()`.
- **WorkOrderPriority**: enum Low = 1, Medium = 2 (default), High = 3; stored as tinyint.
- **DueDateUrgency**: non-persisted, read-time enum None = 0, DueToday = 1, Overdue = 2.
- **Work Order Search queries**: `WorkOrderSpecificationQuery` (creator, assignee, status, overdue, priority,
  building, open only, completed within N days, free text; newest first), `WorkOrderSearchSummaryQuery` (summary card
  and nav badge counts), `WorkOrderBuildingsQuery` (distinct buildings), `BuildingRoomSummaryQuery` (per building and
  room: open, overdue, completed in the last 30 days and total work orders, grouped by the database with the search's
  trimming and collation and the summary cards' predicates, so each count equals the matching search),
  `RecentWorkOrdersQuery` (dashboard recent activity: the last N work orders by their latest created, assigned or
  completed date). The search page keeps its filters, sort, page and view (table, or a board with a column per
  status) in the URL (`WorkOrderSearchUrlState`).
- **Employee**: UserName, FirstName, MiddleName (optional), LastName, EmailAddress, PreferredLanguage, Roles.
  Methods: `GetFullName()`, `CanCreateWorkOrder()`, `CanFulfillWorkOrder()`.
- **WorkOrderStatus**: smart enum: None (empty sentinel), Draft, Assigned, InProgress, Complete, Cancelled; Code, Key,
  FriendlyName, SortBy. WaitingOnVendor is an InProgress substate, not an enum value. Factory methods: `FromCode()`,
  `FromKey()`.
- **Role**: Name, CanCreateWorkOrder, CanFulfillWorkOrder.
- **Team**: Name, Description (optional), Members (Employee), MemberCount. Method: `AddMember()`.
- **Vendor**: Name, Trade, ContactName, EmailAddress, PhoneNumber (optional contact fields), IsActive (defaults to
  true).
- **SavedSearchView**: Id, EmployeeId, Name, Query, CreatedDate; a named search owned by an employee.
- **WorkOrderStatusChange**: WorkOrderId, FromStatus, ToStatus, ChangedById/ChangedBy (Employee), ChangedDate; status
  history.
- **WorkOrderAttachment**: Id, WorkOrderId, FileName, ContentType, FileSize, UploadedById/UploadedBy (Employee),
  UploadedDate; attachment metadata.
- **`EntityBase<T>`**: Guid Id and identity equality; base for WorkOrder, Employee, Role, Team, Vendor and
  WorkOrderStatusChange. SavedSearchView and WorkOrderAttachment declare their own Id.

The entity/supporting-type inventory and associations are in `arch/arch-c4-class-domain-model.puml` (PNG/SVG) and
`arch/arch-c4-class-domain-model.md`. Building is a nullable WorkOrder string, not an entity. Team membership is not
a WorkOrder assignment; Vendor has no direct WorkOrder association. JSON converters, constants, messages, events and
command/result records are not entities. Per-concept docs: [../domain/](../domain/work-order-lifecycle.md).
