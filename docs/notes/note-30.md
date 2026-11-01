# Note 30

A work order description cannot exceed 4000 characters. WorkOrder.DescriptionMaxLength is 4000. SaveDraftCommandValidator rejects a longer description with "Description cannot exceed 4000 characters." WorkOrderMap maps the description column with HasMaxLength(4000).
