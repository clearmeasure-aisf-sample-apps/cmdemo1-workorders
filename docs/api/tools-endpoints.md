# Tool Endpoints

The sample text/utility tools, the echo endpoint and the WeatherForecast sample were retired in #1819. The remaining tool endpoints:

| Endpoint | Route | Method |
|----------|-------|--------|
| Work Order Statuses | `/api/tools/work-order-statuses` | GET |
| Due-Date Check | `/api/tools/due-date-check` | GET |

Each route is also available under the versioned prefix; for example, the versioned work-order-statuses route is `/api/v1.0/tools/work-order-statuses`. The versioned API route template uses `api/v{version:apiVersion}`.
