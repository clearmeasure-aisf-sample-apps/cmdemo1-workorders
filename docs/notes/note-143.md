# Note 143

Employee.HasRole returns false when roleName is null or empty (`string.IsNullOrEmpty(roleName)`), and otherwise returns true when any role's Name equals roleName with StringComparison.OrdinalIgnoreCase (`return Roles.Any(role => string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase));`). The method is `Employee.HasRole` in `src/Core/Model/Employee.cs`.
