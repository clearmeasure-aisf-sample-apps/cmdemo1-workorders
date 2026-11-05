# Note 343

`ToolsUppercaseController.Post` in `src/UI/Api/Controllers/ToolsUppercaseController.cs` treats a missing body or a null `Text` as an empty string (`var text = request?.Text ?? string.Empty`) and returns that text converted with `text.ToUpperInvariant()`. The method summary says a missing body or text yields an empty string. The same behavior is stated in `docs/notes/note-56.md`.
