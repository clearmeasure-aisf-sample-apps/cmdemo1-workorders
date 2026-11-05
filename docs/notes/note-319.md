# Note 319

`Team.AddMember` in `src/Core/Model/Team.cs` adds the employee to `Members` (`public void AddMember(Employee employee) => Members.Add(employee);`). `Members` is `ISet<Employee>` initialized to `new HashSet<Employee>()`, so an employee who is already a member is not added again. The same fact is stated in `docs/notes/note-25.md`.
