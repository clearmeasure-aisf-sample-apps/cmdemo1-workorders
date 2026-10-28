# Note 108

Employee.AddRole adds the given role to the employee's Roles set (`Roles.Add(role)`). Roles is `public ISet<Role> Roles { get; init; } = new HashSet<Role>();`. The method is `public void AddRole(Role role)` on `Employee` in `src/Core/Model/Employee.cs`.
