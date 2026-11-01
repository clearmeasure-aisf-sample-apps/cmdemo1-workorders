# API rate limiting

The tools endpoints are rate-limited by `ApiRateLimiting.PolicyName`. The constant is `ApiSlidingWindow`, defined in `src/UI/Api/ApiRateLimiting.cs`.

Each tools controller applies the policy with `[EnableRateLimiting(ApiRateLimiting.PolicyName)]`:

- `ToolsHashController` (`/api/tools/hash`)
- `ToolsBase64Controller` (`/api/tools/base64`)
- `ToolsGuidGeneratorController` (`/api/tools/guid-generator`)
- `ToolsRandomController` (`/api/tools/random`)
- `ToolsSlugifyController` (`/api/tools/slugify`)
- `ToolsCaseController` (`/api/tools/case`)
- `ToolsWordCountController` (`/api/tools/word-count`)
- `ToolsDueDateCheckController` (`/api/tools/due-date-check`)
- `ToolsWorkOrderStatusesController` (`/api/tools/work-order-statuses`)
- `TimestampConverterController` (`/api/tools/timestamp-converter`)

Each of those routes is also available under the versioned prefix `/api/v1.0/tools/`.
