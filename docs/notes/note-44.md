# Note 44

A saved search view name is at most 100 characters and its query is at most 1000 characters. SavedSearchView.MaxNameLength is 100. SavedSearchView.MaxQueryLength is 1000. SavedSearchView.ValidateNew returns "Name must be 100 characters or fewer." when the trimmed name is longer, and "Current filters are too long to save." when the query is longer. SaveSearchViewCommandValidator applies MaximumLength of those constants. SavedSearchViewMap maps the name and query columns with HasMaxLength of the same constants.
