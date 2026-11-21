# API rate limiting

The tool endpoints are rate-limited by `ApiRateLimiting.PolicyName`. The constant is `ApiSlidingWindow`, defined in `src/UI/Api/ApiRateLimiting.cs`.

Each tools controller applies the policy with `[EnableRateLimiting(ApiRateLimiting.PolicyName)]`:

- `ToolsDueDateCheckController` (`/api/tools/due-date-check`)
- `ToolsWorkOrderStatusesController` (`/api/tools/work-order-statuses`)

Each of those routes is also available under the versioned prefix `/api/v1.0/tools/`.
