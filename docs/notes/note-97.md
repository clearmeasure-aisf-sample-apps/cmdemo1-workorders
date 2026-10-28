# Note 97

WorkOrderStatus.IsEmpty returns true when Code equals the empty string (`return Code == ""`). WorkOrderStatus.None is constructed with an empty code (`new("", "", " ", 0)`), so that sentinel is empty. A null Code is not empty, because the comparison is string equality (`null == ""` is false). The method is `WorkOrderStatus.IsEmpty` in `src/Core/Model/WorkOrderStatus.cs`.
