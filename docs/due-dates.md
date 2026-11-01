# Work Order Due Dates

`DueDate` is an optional calendar date by which a work order should be completed. A work order without a due date has no due-date urgency.

Due-date urgency is calculated using the current date in the America/Chicago time zone:

- **On track** — The due date is in the future.
- **Due today** — The due date is today.
- **Overdue** — The due date is before today.

Only open work orders (Draft, Assigned, and InProgress) can be marked due today or overdue. A work order is not overdue on its due date; it becomes overdue when the Chicago calendar date advances beyond that date. Complete and Cancelled work orders do not show due-date urgency.
