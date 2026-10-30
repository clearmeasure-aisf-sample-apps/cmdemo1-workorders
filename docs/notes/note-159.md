# Note 159

When text is not null or empty, ToolsWordCountController.CountLines replaces CR LF with LF using ordinal comparison, then replaces each remaining CR with LF, and returns the number of pieces from splitting on LF (`text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n').Length` after the empty check). The method is `CountLines` in `src/UI/Api/Controllers/ToolsWordCountController.cs`.
