# Note 47

Shelving an in-progress work order requires a reason, and that reason is at most 200 characters. InProgressToAssignedCommand.ReasonMaxLength is 200. InProgressToAssignedCommandValidator requires the reason with the message "A reason is required to shelve a work order." and applies MaximumLength(ReasonMaxLength) with the message "Reason cannot exceed 200 characters."
