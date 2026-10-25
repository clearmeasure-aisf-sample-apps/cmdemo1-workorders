# Handler

A **Handler** is the MediatR `IRequestHandler` that receives a query or command sent with `IBus.Send`. [`WorkOrderQueryHandler`](../../src/DataAccess/Handlers/WorkOrderQueryHandler.cs) in `src/DataAccess/Handlers` implements `IRequestHandler<WorkOrderByNumberQuery, WorkOrder?>`; work-order state changes use [`StateCommandHandler`](../../src/DataAccess/Handlers/StateCommandHandler.cs). The path is described in [CQRS request flow](../conventions/cqrs-request-flow.md).
