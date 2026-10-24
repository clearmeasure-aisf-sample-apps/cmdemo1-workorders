# Work Order Lifecycle

A work order starts in **Draft** and normally follows:

**Draft → Assigned → InProgress → Complete**

## Transitions

| From | Action | To | Performed by |
| --- | --- | --- | --- |
| Draft | Save | Draft | Creator |
| Draft | Assign | Assigned | Creator |
| Assigned | Begin | InProgress | Assignee |
| InProgress | Complete | Complete | Assignee |
| InProgress | Shelve | Assigned | Assignee |
| Assigned | Cancel | Cancelled | Creator |
| InProgress | Cancel | Cancelled | Creator |

Saving preserves the draft. Assigning records the assigned date; completing
records the completed date. Shelving returns unfinished work to Assigned,
where the assignee can begin it again.

## Waiting on a vendor

**WaitingOnVendor is a substate of InProgress, not a separate status.** The
assignee can choose **Wait on vendor** to set `WaitingOnVendorSince`, then
**Resume** to clear it without changing the status.

Completion and shelving are available from either InProgress substate. The
creator can cancel from either substate as well. Leaving InProgress clears
`WaitingOnVendorSince`; beginning work again starts active work.

## Cancellation and terminal states

Cancellation is available from Assigned or InProgress, not Draft or Complete.
It clears the assignee and assigned date. **Complete** and **Cancelled** are
terminal: no outgoing lifecycle transitions are defined.

## References

- [Status descriptions](work-order-statuses.md)
- [State diagram](../../arch/arch-state-workorder.md)
- [State command implementations](../../src/Core/Model/StateCommands/)
