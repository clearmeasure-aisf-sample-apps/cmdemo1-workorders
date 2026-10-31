# Note 178

ToolsWordCountController.CountWords returns the length of a whitespace split that drops empty entries, or zero when text is null (`text?.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries).Length ?? 0`). The method is `CountWords` in `src/UI/Api/Controllers/ToolsWordCountController.cs`.
