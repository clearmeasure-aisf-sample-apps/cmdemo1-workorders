# Note 54

POST /api/tools/word-count returns zero words, zero characters, and zero lines when the JSON text is null or missing. ToolsWordCountController.CountWords splits on whitespace and drops empty entries, and null text is zero words. CountCharacters returns text.Length or 0. CountLines returns 0 when text is null or empty.
