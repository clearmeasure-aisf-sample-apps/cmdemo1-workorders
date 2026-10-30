# Note 31

A work order's instructions cannot exceed 4000 characters. WorkOrder.InstructionsMaxLength is 4000. Setting Instructions truncates a longer value to that length, and a null value becomes an empty string. WorkOrderMap maps the instructions column with HasMaxLength(WorkOrder.InstructionsMaxLength). WorkOrderManageModel reports "Instructions cannot exceed 4000 characters."
