# Note 48

Shelving an in-progress work order back to Assigned requires a reason of at most 200 characters. InProgressToAssignedCommand.ReasonMaxLength is 200. InProgressToAssignedCommandValidator rejects an empty reason with "A reason is required to shelve a work order." and a longer reason with "Reason cannot exceed 200 characters." WorkOrderStatusChangeMap maps Reason with HasMaxLength(InProgressToAssignedCommand.ReasonMaxLength).
