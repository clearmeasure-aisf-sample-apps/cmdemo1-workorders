# CQRS request flow

The [request-flow overview](../request-flow.md) describes the common path:
Blazor UI → API controller → `IBus` → MediatR → handler → EF Core → database.
For a concrete read, follow an existing work order loaded by the Blazor WebAssembly client:

```text
WorkOrderManage → IBus.Send(WorkOrderByNumberQuery)
  → RemotableBus → PublisherGateway → HTTP POST → SingleApiController
  → server IBus.Send → Bus → IMediator.Send → WorkOrderQueryHandler
  → DataContext (EF Core) → database
```

1. **UI and request.** [WorkOrderManage.LoadWorkOrder](../../src/UI.Shared/Pages/WorkOrderManage.razor.cs)
   calls `Bus.Send(new WorkOrderByNumberQuery(Id!))` when opening an existing order.
   [WorkOrderByNumberQuery](../../src/Core/Queries/WorkOrderByNumberQuery.cs) is a Core
   record implementing `IRequest<WorkOrder?>` and `IRemotableRequest`.
2. **Browser transport.** The client registers
   [RemotableBus](../../src/UI/Client/RemotableBus.cs) as `IBus` in
   [UIClientServiceRegistry](../../src/UI/Client/UIClientServiceRegistry.cs).
   It forwards `IRemotableRequest` messages to
   [PublisherGateway](../../src/UI/Client/PublisherGateway.cs), which wraps the request
   in a `WebServiceMessage` and posts JSON to `api/blazor-wasm-single-api`.
3. **API boundary.** [SingleApiController.Post](../../src/UI/Server/Controllers/SingleApiController.cs)
   reconstructs the request using `GetBodyObject()` and calls the server's
   `IBus.Send`. The controller also exposes the versioned route
   `api/v1.0/blazor-wasm-single-api`. For this path, the UI already constructed the
   query; the controller unwraps it rather than creating a new query.
4. **Server dispatch.** [IBus](../../src/Core/IBus.cs) is the application abstraction.
   [UIServiceRegistry](../../src/UI/Server/UIServiceRegistry.cs) registers
   `ServerRealtimeBus`, which inherits request dispatch from
   [Bus](../../src/UI.Shared/Bus.cs). `Bus.Send` calls `IMediator.Send`, and MediatR
   selects the matching `IRequestHandler<WorkOrderByNumberQuery, WorkOrder?>`.
5. **Handler and database.** [WorkOrderQueryHandler](../../src/DataAccess/Handlers/WorkOrderQueryHandler.cs)
   reads `context.Set<WorkOrder>()` with `AsNoTracking()` and
   `SingleOrDefaultAsync(wo => wo.Number == number)`.
   [DataContext](../../src/DataAccess/Mappings/DataContext.cs) configures EF Core
   mappings and the database provider: SQLite for a connection string starting
   with `Data Source=`, otherwise SQL Server.
6. **Return path.** The result returns through MediatR and the server bus.
   `SingleApiController` wraps a non-null result in a `WebServiceMessage`;
   `PublisherGateway` deserializes the response envelope, and `RemotableBus`
   unwraps its body into the response type returned to the page.
   Although the query permits a missing order (`null`), this controller rejects
   a null result with `InvalidOperationException`.

## Commands and local dispatch

Reads use query handlers; work-order state changes use
[StateCommandHandler](../../src/DataAccess/Handlers/StateCommandHandler.cs).
That handler executes the state command, persists the order with EF Core
`SaveChangesAsync`, then publishes its state-transition event through
`IDistributedBus`. The read example above does not save changes.

The HTTP hop is specific to remoted browser requests, not every `IBus` call.
Client `RemotableBus` delegates requests without `IRemotableRequest` to its base
`Bus` for local MediatR dispatch; server callers can dispatch directly through
the server bus without going through the browser transport.
