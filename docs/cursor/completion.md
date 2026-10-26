# Work order completion

The assignee completes a work order that is **InProgress** through `InProgressToCompleteCommand` (present-tense verb "Complete", past-tense "Completed"). Only the assignee can execute the command. It begins at InProgress and ends at **Complete**.

Execution sets `CompletedDate` to the command context's current date and time, then moves the status to Complete. Waiting on a vendor is still InProgress (`WaitingOnVendorSince` is set). Completing from that substate uses the same command; leaving InProgress clears `WaitingOnVendorSince`.

**Complete** is terminal: a work order cannot leave that status.
