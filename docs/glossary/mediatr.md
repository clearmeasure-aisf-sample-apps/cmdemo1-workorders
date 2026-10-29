# MediatR

**MediatR** is the library this application uses to dispatch a request to its handler. [`IBus`](../../src/Core/IBus.cs) in `src/Core` sends MediatR `IRequest<TResponse>` values and publishes `INotification` values; [`Bus`](../../src/UI.Shared/Bus.cs) in `src/UI.Shared` calls `IMediator.Send` and `IMediator.Publish`, and handlers such as [`WorkOrderQueryHandler`](../../src/DataAccess/Handlers/WorkOrderQueryHandler.cs) in `src/DataAccess/Handlers` implement `IRequestHandler`. The path is described in [CQRS request flow](../conventions/cqrs-request-flow.md).
