# Waiting on a Vendor

WaitingOnVendor is a substate of **InProgress**, not a work order status. The persisted statuses are Draft, Assigned, InProgress, Complete, and Cancelled. `WaitingOnVendorSince` is that substate: set while the assignee waits on a vendor (parts, contractor, or quote), and null during active InProgress. `IsWaitingOnVendor` is true only when the status is InProgress and the timestamp is set; a timestamp on any other status is ignored. The database column is a nullable `WaitingOnVendorSince`, not a status.

Wait on vendor sets `WaitingOnVendorSince` to the command time, and Resume clears it. Only the assignee can run either command. Both begin and end as InProgress, so the persisted status does not change and no status-history row is written. `ChangeStatus` clears the timestamp only when the new status is not InProgress.
