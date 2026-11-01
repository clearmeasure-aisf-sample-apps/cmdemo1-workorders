# Note 150

SlaHealth.TrendText returns null when `lastPercent` is null or `currentPercent` is null (`if (lastPercent is null || currentPercent is null) return null`). Otherwise it subtracts `lastPercent` from `currentPercent` and returns `↑ up from {lastPercent:0}%` when the difference is greater than zero, `↓ down from {lastPercent:0}%` when the difference is less than zero, and `— flat at {lastPercent:0}%` when the difference is zero. The method is `SlaHealth.TrendText` in `src/Core/Queries/SlaHealth.cs`.
