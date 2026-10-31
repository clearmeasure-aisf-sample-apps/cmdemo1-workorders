# Note 129

WorkOrderPriorityExtensions.ToLabel returns "Low" for WorkOrderPriority.Low, "Medium" for WorkOrderPriority.Medium, and "High" for WorkOrderPriority.High, and throws ArgumentOutOfRangeException for any other value (`Low => "Low"`, `Medium => "Medium"`, `High => "High"`, `_ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)`). The method is `ToLabel` in `src/Core/Model/WorkOrderPriorityExtensions.cs`.
