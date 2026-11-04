# Note 310

`Employee.GetDisplayName` in `src/Core/Model/Employee.cs` returns `$"{LastName}, {FirstName}"` when both names are non-whitespace (`return string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{LastName}, {FirstName}";`). When `FirstName` is null or whitespace it returns `LastName` if that is set, otherwise `UserName` (`return string.IsNullOrWhiteSpace(LastName) ? UserName : LastName;`). When `FirstName` is set and `LastName` is null or whitespace it returns `FirstName`. Names are not trimmed. The same fact is stated in `docs/notes/note-107.md`.
