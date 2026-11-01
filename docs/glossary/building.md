# Building

**Building** is the optional name of the building where a work order's work takes place (`WorkOrder.Building`, a nullable string in [`src/Core/Model/WorkOrder.cs`](../../src/Core/Model/WorkOrder.cs)). It is not an entity and is independent of room number; a null, empty, or whitespace-only value means no building. Work-order search and the building/room summary use that name, as described in [Buildings and Rooms](../domain/buildings-and-rooms.md).
