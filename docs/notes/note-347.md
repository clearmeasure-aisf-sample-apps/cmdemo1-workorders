# Note 347

`WorkOrder.Title` in `src/Core/Model/WorkOrder.cs` is initialized to `""` and its setter assigns `value?.Trim()`, so a null title stays null and a non-null title loses leading and trailing whitespace while keeping whitespace between words. The property's XML summary states the same rule.
