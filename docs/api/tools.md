# API Tools

The sample text/utility tools, the echo endpoint and the WeatherForecast sample were retired in #1819. The remaining tool endpoints are described below. Each route is also available under `/api/v1.0/tools/`.

## Due-date check

**GET `/api/tools/due-date-check`** returns the due-date urgency for the required `date` (`YYYY-MM-DD`), relative to today in the America/Chicago timezone, assuming the work order is open.

Example:

```sh
curl -X GET "http://localhost:5175/api/tools/due-date-check?date=2025-01-15"
```

Response (200):

```json
{ "urgency": "Overdue" }
```

When `date` is missing or not in `YYYY-MM-DD` format, the endpoint returns a 400 problem.

## Work order statuses

**GET `/api/tools/work-order-statuses`** lists the work order statuses (`code`, `key`, `friendlyName`, `sortBy`) in sort order.

Example:

```sh
curl -X GET "http://localhost:5175/api/tools/work-order-statuses"
```

Response (200):

```json
[
  { "code": "DRT", "key": "Draft", "friendlyName": "Draft", "sortBy": 1 },
  { "code": "ASD", "key": "Assigned", "friendlyName": "Assigned", "sortBy": 2 },
  { "code": "IPG", "key": "InProgress", "friendlyName": "In Progress", "sortBy": 3 },
  { "code": "CMP", "key": "Complete", "friendlyName": "Complete", "sortBy": 4 },
  { "code": "CNL", "key": "Cancelled", "friendlyName": "Cancelled", "sortBy": 5 }
]
```
