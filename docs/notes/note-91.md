# Note 91

WorkOrderStringUtility.TruncateDescription returns the description unchanged when its length is at most WorkOrder.DescriptionMaxLength (4000), and otherwise returns the first 4000 characters (`description.Length <= WorkOrder.DescriptionMaxLength ? description : description[..WorkOrder.DescriptionMaxLength]`). The method is `WorkOrderStringUtility.TruncateDescription` in `src/Core/Services/WorkOrderStringUtility.cs`.
