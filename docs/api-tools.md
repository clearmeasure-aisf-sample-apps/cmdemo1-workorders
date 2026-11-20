# API Tools

The sample text/utility tools, the echo endpoint and the WeatherForecast sample were retired in #1819. The remaining tool endpoints:

- **GET `/api/tools/due-date-check`** returns the due-date urgency for the required `date` (`YYYY-MM-DD`), relative to today in the America/Chicago timezone, assuming the work order is open.
- **GET `/api/tools/work-order-statuses`** lists the work order statuses (`code`, `key`, `friendlyName`, `sortBy`) in sort order.
