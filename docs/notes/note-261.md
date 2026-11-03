# Note 261

`WorkOrder.Instructions` in `src/Core/Model/WorkOrder.cs` is initialized to `""` and its setter assigns `GetTruncatedString(value)`. That method returns `string.Empty` when the value is null, and otherwise `value.Substring(0, Math.Min(InstructionsMaxLength, value.Length))`, where `InstructionsMaxLength` is `4000`. The same limit is stated in `docs/notes/note-31.md`.
