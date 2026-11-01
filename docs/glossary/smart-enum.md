# Smart enum

**Smart enum** is the name this application uses for `WorkOrderStatus` in [`src/Core/Model/WorkOrderStatus.cs`](../../src/Core/Model/WorkOrderStatus.cs): a class of static instances (None as an empty sentinel, Draft, Assigned, InProgress, Complete, and Cancelled), each with a code, key, friendly name, and sort order, rather than a C# enum. `FromCode()` and `FromKey()` return the matching instance. Waiting on a vendor is an InProgress substate, not a status value ([Work order statuses](../domain/work-order-statuses.md)).
