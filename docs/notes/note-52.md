# Note 52

A slug from POST /api/tools/slugify is at most 80 characters and does not end with a hyphen. ToolsSlugifyController.MaxSlugLength is 80. GenerateSlug turns each run of non-alphanumeric characters into one hyphen, trims leading and trailing hyphens, lowercases with the invariant culture, and when the result is longer than 80 characters keeps the first 80 and trims a trailing hyphen. Whitespace-only text yields an empty slug.
