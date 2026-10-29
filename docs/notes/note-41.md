# Note 41

A work order's external reference is optional and at most 100 characters. WorkOrder.ExternalReferenceMaxLength is 100. ExternalReference is an optional string. WorkOrderMap maps the column with HasMaxLength(WorkOrder.ExternalReferenceMaxLength). Script 039_AddExternalReferenceToWorkOrder.sql adds the column as NVARCHAR(100) NULL.
