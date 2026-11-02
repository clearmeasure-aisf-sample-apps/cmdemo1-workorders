# Note 232

`WorkOrder.Title` in `src/Core/Model/WorkOrder.cs` is initialized to an empty string and its setter stores `value?.Trim()` (`public string? Title { get; set => field = value?.Trim(); } = "";`).
