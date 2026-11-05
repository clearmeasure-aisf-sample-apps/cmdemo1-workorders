# Note 340

`Employee.CanFulfillWorkOrder()` in `src/Core/Model/Employee.cs` returns true when any assigned role has `CanFulfillWorkOrder` (`public bool CanFulfillWorkOrder() => Roles.Any(role => role.CanFulfillWorkOrder);`).
