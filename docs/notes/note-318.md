# Note 318

`DueDateUrgency` in `src/Core/Model/DueDateUrgency.cs` is a read-time enum that is never persisted. Its values are `None = 0`, `DueToday = 1`, and `Overdue = 2`. The type summary says "Read-time urgency for an optional work-order due date. Never persisted." `docs/notes/note-22.md` states the same values.
