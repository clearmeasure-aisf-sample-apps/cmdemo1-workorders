# Note 272

`WorkOrderStatus` defines `operator !=` as the negation of `operator ==` (`!(left == right)`). The operator is `public static bool operator !=(WorkOrderStatus? left, WorkOrderStatus? right) => !(left == right);` in `src/Core/Model/WorkOrderStatus.cs`. The same behavior is stated in `docs/notes/note-115.md`.
