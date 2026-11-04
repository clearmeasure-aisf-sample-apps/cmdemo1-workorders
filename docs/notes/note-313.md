# Note 313

`WorkOrderStatus.None` in `src/Core/Model/WorkOrderStatus.cs` is `public static readonly WorkOrderStatus None = new("", "", " ", 0);`. The protected constructor stores those arguments as Code, Key, FriendlyName, and SortBy, so the sentinel has an empty code, an empty key, a friendly name of one space, and sort order 0. `docs/notes/note-23.md` states the same fact.
