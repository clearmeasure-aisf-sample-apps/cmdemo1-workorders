# Note 305

A new work order's status starts as Draft. `WorkOrder.Status` in `src/Core/Model/WorkOrder.cs` is initialized with `public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;`. `WorkOrderStatus.Draft` in `src/Core/Model/WorkOrderStatus.cs` is `public static readonly WorkOrderStatus Draft = new("DRT", "Draft", "Draft", 1);` (code `DRT`, key `Draft`, friendly name `Draft`, sort order `1`).
