# Query

A **Query** is a read request in this application's CQRS flow. [`WorkOrderByNumberQuery`](../../src/Core/Queries/WorkOrderByNumberQuery.cs) in `src/Core/Queries` implements Mediator `IRequest<WorkOrder?>` and is sent with `IBus.Send`; the source-generated mediator dispatches it to a handler in `DataAccess`. The path is described in [CQRS request flow](../conventions/cqrs-request-flow.md).
