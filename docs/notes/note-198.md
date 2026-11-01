# Note 198

`WorkOrderStatus.FromCode` in `src/Core/Model/WorkOrderStatus.cs` trims its argument (`code.Trim()`), finds the status in `GetAllItems()` whose `Code` equals that trimmed value (`instance.Code == normalized`), and throws `ArgumentOutOfRangeException` when none matches. The exception message includes the original, untrimmed code (`$"Code '{code}' is not a valid code for {nameof(WorkOrderStatus)}"`).
