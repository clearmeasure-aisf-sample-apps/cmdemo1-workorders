# Note 223

`SavedSearchView.ValidateNew` in `src/Core/Model/SavedSearchView.cs` returns `Enter a name for the view.` when the trimmed name is empty (`var trimmed = name?.Trim() ?? string.Empty;` then `if (trimmed.Length == 0) return "Enter a name for the view.";`).
