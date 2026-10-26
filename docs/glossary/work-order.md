# Work order

A work order is the request an employee creates and tracks in this application. It has a unique number, a title, a status (Draft, Assigned, InProgress, Complete, or Cancelled), a creator, and an optional assignee. The type is `WorkOrder` in [src/Core/Model/WorkOrder.cs](../../src/Core/Model/WorkOrder.cs); the status path is described in [docs/domain/work-order-lifecycle.md](../domain/work-order-lifecycle.md).
