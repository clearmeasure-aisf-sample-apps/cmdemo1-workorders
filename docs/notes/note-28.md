# Note 28

A work-order floor cannot exceed 20 characters. WorkOrder.FloorMaxLength is 20. SaveDraftCommandValidator rejects a longer floor with "Floor cannot exceed 20 characters." WorkOrderManageModel reports the same message. An unset floor is stored as null.
