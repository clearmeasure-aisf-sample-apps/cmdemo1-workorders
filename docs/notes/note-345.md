# Note 345

A new work order's `Priority` defaults to `WorkOrderPriority.Medium` (value 2). `src/Core/Model/WorkOrder.cs` initializes `Priority` to `WorkOrderPriority.Medium`, and `src/Core/Model/WorkOrderPriority.cs` defines `Medium = 2` and states that new work orders default to Medium.
