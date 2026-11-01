# Note 86

Employee.HasRole returns false when the role name is null or empty. Otherwise it returns true when any of the employee's roles has a name equal to the given name, compared with ordinal ignore-case (`StringComparison.OrdinalIgnoreCase`). A whitespace-only name is not empty; it matches only a role whose name equals that string, ignoring case. The method is `Employee.HasRole` in `src/Core/Model/Employee.cs`.
