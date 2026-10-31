# Work Order Statuses

The persisted work-order statuses and their valid exits are:

- **Draft** — Created but not yet assigned. It can remain Draft when saved or move to Assigned.
- **Assigned** — Ready for fulfillment. Work can begin by moving to InProgress, or the work order can be cancelled.
- **InProgress** — Work is actively underway. It can be completed or shelved back to Assigned from either substate (active work, or waiting on a vendor), and it can be cancelled from active work or while waiting. WaitingOnVendor is an InProgress substate, not a separate status: waiting and resuming do not change the persisted status.
- **Complete** — The work order has been finished. This is a terminal status.
- **Cancelled** — The work order will not be completed. This is a terminal status.

Complete and Cancelled cannot be exited. Leaving InProgress clears the vendor-waiting substate; returning from Assigned starts active work.
