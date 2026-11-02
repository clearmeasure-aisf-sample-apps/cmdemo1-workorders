# Note 247

`Employee.ToString` in `src/Core/Model/Employee.cs` returns `GetFullName()` (`return GetFullName();`). `Employee.GetFullName` returns `$"{FirstName} {LastName}"` when `MiddleName` is null or whitespace, and `$"{FirstName} {MiddleName} {LastName}"` otherwise.
