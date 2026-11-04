# Note 308

`Employee.GetFullName` in `src/Core/Model/Employee.cs` returns `$"{FirstName} {LastName}"` when `MiddleName` is null or whitespace, and `$"{FirstName} {MiddleName} {LastName}"` otherwise (`return string.IsNullOrWhiteSpace(MiddleName) ? $"{FirstName} {LastName}" : $"{FirstName} {MiddleName} {LastName}";`).
