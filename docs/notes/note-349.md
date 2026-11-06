# Note 349

`WorkOrderBuilder.CreateNewWorkOrder` in `src/Core/Services/Impl/WorkOrderBuilder.cs` sets `Number` from `IWorkOrderNumberGenerator.GenerateNumber()`, sets `Creator` to the employee argument, and sets `Status` to `WorkOrderStatus.Draft`. `docs/glossary/creator.md` states that `WorkOrder.Creator` is set by `WorkOrderBuilder.CreateNewWorkOrder` when the work order starts in Draft.
