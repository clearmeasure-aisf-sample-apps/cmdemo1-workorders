# Note 344

`ToolsWordCountController.CountCharacters` in `src/UI/Api/Controllers/ToolsWordCountController.cs` is `text?.Length ?? 0`, so a null text yields zero characters. The same behavior is stated in `docs/notes/note-54.md` ("CountCharacters returns text.Length or 0").
