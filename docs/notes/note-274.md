# Note 274

`ToolsSlugifyController.GenerateSlug` in `src/UI/Api/Controllers/ToolsSlugifyController.cs` returns `string.Empty` when its argument is null or whitespace (`if (string.IsNullOrWhiteSpace(text)) { return string.Empty; }`). `docs/notes/note-52.md` states the same behavior: whitespace-only text yields an empty slug.
