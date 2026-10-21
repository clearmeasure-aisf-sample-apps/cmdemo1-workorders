# Work Order Statuses

**Draft** — The work order has been created but is not yet assigned. Its next status is Assigned.

**Assigned** — The work order is ready for fulfillment. Work can begin by moving it to InProgress, or it can be cancelled.

**InProgress** — Work is actively underway. It can be completed, cancelled, or shelved back to Assigned. WaitingOnVendor is an InProgress substate, not a separate status; waiting and resuming do not change the persisted status.

**Complete** — The work order has been finished. Complete is a terminal status.

**Cancelled** — The work order will not be completed. It can be cancelled from Assigned or InProgress, including while waiting on a vendor. Cancelled is a terminal status.
