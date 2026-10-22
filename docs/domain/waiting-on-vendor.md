# Waiting on a Vendor

WaitingOnVendor is a substate of **InProgress**, not a separate work order status. The work order's status remains InProgress while the assignee waits for vendor work, parts, or a quote.

The substate is represented by `WaitingOnVendorSince`: it is set when waiting begins and cleared when the assignee resumes work. Both transitions keep the status as InProgress, so waiting and resuming do not change the persisted status.
