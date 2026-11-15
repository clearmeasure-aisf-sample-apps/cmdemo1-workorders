# Note 273

`DueDateUrgencyPresenter.CssClass` in `src/UI.Shared/Models/DueDateUrgencyPresenter.cs` returns `"due-date-today"` for `DueDateUrgency.DueToday`, `"due-date-overdue"` for `DueDateUrgency.Overdue`, and `string.Empty` otherwise (`DueDateUrgency.DueToday => "due-date-today"`, `DueDateUrgency.Overdue => "due-date-overdue"`, `_ => string.Empty`). The same fact is stated in `docs/notes/note-120.md`.
