# Note 190

A new work order starts in Draft. `WorkOrder.Status` in `src/Core/Model/WorkOrder.cs` is initialized with `= WorkOrderStatus.Draft`. `WorkOrderTests.PropertiesShouldInitializeToProperDefaults` in `src/UnitTests/Core/Model/WorkOrderTests.cs` asserts that `new WorkOrder()` has `Status` equal to `WorkOrderStatus.Draft`.
