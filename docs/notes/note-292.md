# Note 292

`SavedSearchView.MaxViewsPerEmployee` in `src/Core/Model/SavedSearchView.cs` is the constant 20 (`public const int MaxViewsPerEmployee = 20`). When an employee already has 20 views, `SavedSearchView.ValidateNew` returns "You can save up to 20 views. Delete one first." The same limit is stated in `docs/notes/note-32.md`.
