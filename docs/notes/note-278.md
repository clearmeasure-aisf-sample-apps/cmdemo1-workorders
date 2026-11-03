# Note 278

`WorkOrderStatus` operator `==` in `src/Core/Model/WorkOrderStatus.cs` returns `left?.Equals(right) ?? right is null` (`public static bool operator ==(WorkOrderStatus? left, WorkOrderStatus? right) => left?.Equals(right) ?? right is null;`). When the left operand is null, the result is true only when the right operand is also null. The same behavior is stated in `docs/notes/note-98.md`.
