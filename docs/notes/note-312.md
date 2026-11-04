# Note 312

`WorkOrder.DueDate` in `src/Core/Model/WorkOrder.cs` is `public DateOnly? DueDate { get; set; }`. The property summary says it is an optional calendar due date (date-only, no time) and is null when unset. `docs/glossary/due-date.md` states that a due date is the optional calendar date by which a work order should be completed (`WorkOrder.DueDate`, a nullable date with no time). `docs/due-dates.md` states that `DueDate` is an optional calendar date by which a work order should be completed.
