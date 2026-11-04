# Note 314

`Employee.CanCreateWorkOrder()` in `src/Core/Model/Employee.cs` returns true when any assigned role has `CanCreateWorkOrder` (`public bool CanCreateWorkOrder() => Roles.Any(role => role.CanCreateWorkOrder);`). The same fact is stated in `docs/notes/note-7.md` and `docs/glossary/employee.md`.
