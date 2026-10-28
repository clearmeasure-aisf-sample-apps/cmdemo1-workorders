# Note 109

Employee.DisplayName returns the trimmed first name, a space, and the trimmed last name when both names are set (`return $"{FirstName.Trim()} {LastName.Trim()}";`). When only the first name is set, it returns that name trimmed (`return FirstName.Trim();`). When only the last name is set, it returns that name trimmed (`return LastName.Trim();`). When both are null, empty, or whitespace, it returns UserName. A whitespace-only name counts as empty. The middle name is omitted. The property is `Employee.DisplayName` in `src/Core/Model/Employee.cs`.
