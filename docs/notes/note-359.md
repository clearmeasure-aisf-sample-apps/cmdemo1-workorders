# Note 359

`Employee.HasRole` in `src/Core/Model/Employee.cs` returns false when `roleName` is null or empty (`if (string.IsNullOrEmpty(roleName)) return false;`). Otherwise it returns true when any role's name equals `roleName`, ignoring case (`return Roles.Any(role => string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase));`). The same fact is stated in `docs/notes/note-285.md`.
