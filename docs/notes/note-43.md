# Note 43

A work order's access notes are optional and at most 500 characters. WorkOrder.AccessNotesMaxLength is 500. AccessNotes is an optional string and is null when unset. WorkOrderMap maps the column with HasMaxLength(WorkOrder.AccessNotesMaxLength). Script 040_AddAccessNotesToWorkOrder.sql adds the column as NVARCHAR(500) NULL.
