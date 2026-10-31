# Note 110

WorkOrderStatus.FromCode trims the argument (`code.Trim()`), searches `GetAllItems()` for the status whose `Code` equals that trimmed value (`instance.Code == normalized`), and throws `ArgumentOutOfRangeException` when none matches. The exception message includes the original, untrimmed code (`$"Code '{code}' is not a valid code for {nameof(WorkOrderStatus)}"`). The method is `WorkOrderStatus.FromCode` in `src/Core/Model/WorkOrderStatus.cs`.
