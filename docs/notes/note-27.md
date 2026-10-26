# Note 27

A work-order building cannot exceed 100 characters. WorkOrder.BuildingMaxLength is 100. SaveDraftCommandValidator rejects a longer building with "Building cannot exceed 100 characters." WorkOrderMap maps the building column with HasMaxLength(WorkOrder.BuildingMaxLength). Building stays optional and is null when unset.
