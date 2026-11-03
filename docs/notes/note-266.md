# Note 266

`SavedSearchView.ValidateNew` in `src/Core/Model/SavedSearchView.cs` returns `A view named '{trimmed}' already exists.` when an existing name equals the trimmed new name under an ordinal ignore-case comparison (`existingNames.Any(n => string.Equals(n.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))`). The name inside the message is the trimmed new name.
