# Work order cancellation

A work order can be cancelled only from **Assigned** or **InProgress**. The creator is the only person who can cancel it. Cancellation changes the status to **Cancelled**, clears the assignee and `AssignedDate`, and does not set `CompletedDate`.

## Assigned

`AssignedToCancelledCommand` (present-tense verb "Cancel") begins at Assigned and ends at Cancelled. Only the creator can execute it.

## InProgress

InProgress has two substates. Waiting on a vendor is not its own status: it is InProgress with `WaitingOnVendorSince` set.

- Active work (`WaitingOnVendorSince` is null): `InProgressToCancelledCommand`.
- Waiting on a vendor (`WaitingOnVendorSince` is set): `WaitingOnVendorToCancelledCommand`.

Both commands use the present-tense verb "Cancel", end at Cancelled, and can be executed only by the creator. Both clear the assignee and `AssignedDate`.

## Statuses that cannot be cancelled

**Draft**, **Complete**, and **Cancelled** have no cancel command. **Complete** and **Cancelled** are terminal: a work order cannot leave either status.
