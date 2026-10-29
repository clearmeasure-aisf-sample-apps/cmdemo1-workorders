# Note 51

Employee.GetInitials returns the uppercase initials of the first and last names. The private helper GetInitial returns an empty string when the name is null or whitespace, and otherwise the first character of the trimmed name uppercased with char.ToUpperInvariant. The middle name is omitted.
