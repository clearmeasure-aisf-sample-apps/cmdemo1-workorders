# Note 275

`Employee.CanFulfillWorkOrder` in `src/Core/Model/Employee.cs` returns whether any assigned role can fulfill work orders (`public bool CanFulfillWorkOrder() => Roles.Any(role => role.CanFulfillWorkOrder);`). The same fact is stated in `docs/notes/note-7.md` and `docs/glossary/employee.md`.
