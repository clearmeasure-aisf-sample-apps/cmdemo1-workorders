# Waiting on a vendor

WaitingOnVendor is a substate of **InProgress**, not a work-order status. The persisted statuses stay Draft, Assigned, InProgress, Complete, and Cancelled. No new status code is introduced. `WaitingOnVendorSince` marks the substate: set while the assignee waits on a vendor, and null during active InProgress. `IsWaitingOnVendor` is true only when the status is InProgress and the timestamp is set.

## Wait on vendor

`InProgressToWaitingOnVendorCommand` (present-tense verb "Wait on vendor") begins and ends at InProgress. Only the assignee can execute it, and only while `WaitingOnVendorSince` is null. Execution sets `WaitingOnVendorSince` to the command time. The persisted status does not change, so no status-history row is written.

## Resume

`WaitingOnVendorToInProgressCommand` (present-tense verb "Resume") begins and ends at InProgress. Only the assignee can execute it, and only while `WaitingOnVendorSince` is set. Execution clears `WaitingOnVendorSince`. The persisted status does not change, so no status-history row is written.

## Cancel while waiting

`WaitingOnVendorToCancelledCommand` (present-tense verb "Cancel") begins at InProgress with `WaitingOnVendorSince` set and ends at Cancelled. Only the creator can execute it. Execution clears the assignee and `AssignedDate`.

## Leaving InProgress

`ChangeStatus` clears `WaitingOnVendorSince` only when the new status is not InProgress.
