# Note 264

Saving a search view sets `SavedSearchView.CreatedDate` from the current UTC time. `SavedSearchViewHandler.Handle` for `SaveSearchViewCommand` assigns `CreatedDate = time.GetUtcNow().DateTime` when it adds the new `SavedSearchView` in `src/DataAccess/Handlers/SavedSearchViewHandler.cs`.
