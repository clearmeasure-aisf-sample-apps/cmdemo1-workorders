# Note 320

`WorkOrder.IsOpen()` in `src/Core/Model/WorkOrder.cs` returns true unless the status is Complete or Cancelled (`public bool IsOpen() => Status != WorkOrderStatus.Complete && Status != WorkOrderStatus.Cancelled`). The XML comment on that method is "Returns true unless the status is Complete or Cancelled." The same fact is stated in `docs/notes/note-317.md`.
