# Note 238

`WorkOrderStatus.IsEmpty()` in `src/Core/Model/WorkOrderStatus.cs` returns true when `Code` equals the empty string (`return Code == "";`). A null `Code` is not empty, because `null == ""` is false. `WorkOrderStatus.None` is constructed with an empty code (`new("", "", " ", 0)`), so that sentinel is empty.
