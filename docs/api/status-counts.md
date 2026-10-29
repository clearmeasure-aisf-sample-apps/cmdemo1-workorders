# Work Order Status Counts

The read-only endpoint returns the number of work orders in each status.

Routes:

- `GET /api/work-orders/status-counts`
- `GET /api/v1.0/work-orders/status-counts`

No request body or query parameters are required. The endpoint allows anonymous
access, is exempt from API-key authentication and the API sliding-window rate
limit, and disables response caching.

A successful response is HTTP 200 with a JSON object keyed by status key. Every
status is included, even when its count is zero. For example:

```json
{
  "Draft": 3,
  "Assigned": 2,
  "InProgress": 1,
  "Complete": 4,
  "Cancelled": 0
}
```

## Query path

`WorkOrderStatusCountsController` sends `WorkOrderCountByStatusQuery` through
`IBus`. The query returns a `Dictionary<string, int>`.
`WorkOrderCountByStatusQueryHandler` reads work orders through EF Core with
`AsNoTracking()`. For each status from `WorkOrderStatus.GetAllItems()`, it applies
the same status filter as Work Order Search and calls `CountAsync`. No creator
or assignee filter is applied, so each count matches a search filtered only by
that status.

The endpoint provides representative database-backed read traffic for the live
telemetry dashboard, rather than returning static or in-memory counts.

## Source

- [Controller](../../src/UI/Api/Controllers/WorkOrderStatusCountsController.cs)
- [Query](../../src/Core/Queries/WorkOrderCountByStatusQuery.cs)
- [Handler](../../src/DataAccess/Handlers/WorkOrderCountByStatusQueryHandler.cs)
- [Statuses](../../src/Core/Model/WorkOrderStatus.cs)
- [API-key public paths](../../src/UI/Server/ApiKeyAuthenticationMiddleware.cs)
