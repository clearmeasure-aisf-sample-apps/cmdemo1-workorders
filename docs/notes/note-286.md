# Note 286

`Employee.GetInitials()` in `src/Core/Model/Employee.cs` returns the uppercase initials of the first and last names by concatenating `GetInitial(FirstName)` and `GetInitial(LastName)` (`return $"{GetInitial(FirstName)}{GetInitial(LastName)}";`). The private helper `GetInitial` returns an empty string when the name is null or whitespace, and otherwise `char.ToUpperInvariant(name.Trim()[0]).ToString()`. The middle name is omitted. The same fact is stated in `docs/notes/note-51.md`.
