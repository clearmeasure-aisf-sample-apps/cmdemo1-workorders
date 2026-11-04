# Note 306

`Employee.CompareTo` in `src/Core/Model/Employee.cs` orders employees by last name and, when the last names compare equal, by first name, both with ordinal comparison (`string.Compare(LastName, other!.LastName, StringComparison.Ordinal)`, then `string.Compare(FirstName, other.FirstName, StringComparison.Ordinal)` when that result is 0). The same ordering is stated in `docs/notes/note-95.md`.
