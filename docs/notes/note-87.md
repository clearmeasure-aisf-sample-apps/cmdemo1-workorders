# Note 87

WorkOrderNumberGenerator.GenerateNumber returns the first seven characters of a new GUID, converted to upper case (`Guid.NewGuid().ToString().Substring(0, 7).ToUpper()`). The method is `WorkOrderNumberGenerator.GenerateNumber` in `src/Core/Services/Impl/WorkOrderNumberGenerator.cs`.
