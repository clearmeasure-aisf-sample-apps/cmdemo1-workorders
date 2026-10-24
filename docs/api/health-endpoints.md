# Health and diagnostics endpoints

The UI.Server exposes these GET endpoints at its base URL for deployment checks and health dashboards. All three are outside
`/api` and do not require an API key.

## `/_healthcheck`

Returns the overall status of the registered application health checks as plain text. The first line is `Healthy`,
`Degraded`, or `Unhealthy`. The second line is `databaseVersion:` followed by the highest applied DbUp script number, or
`null` when the journal cannot be read. Checks cover dependencies and process health, including the LLM gateway, database,
API, and server. Healthy and degraded responses use HTTP 200; unhealthy responses use HTTP 503. This endpoint is available
to unauthenticated health probes.

## `/_build`

Returns JSON describing the running build, including version, commit and build links and timestamp, code size, test, coverage,
complexity, CRAP, and static-analysis facts. Values the build could not determine are `null`; without stamped build facts,
the assembly version is still returned.

This endpoint is anonymous, readable from any origin, and publicly cacheable for five minutes.

## `/_telemetry`

Returns a JSON snapshot of aggregate counters over a rolling 60-second window and vitals for the process answering the request.
It includes traffic and probe counts, request errors and latency, SQL command counts and latency, completed outgoing HTTP calls,
and process CPU, memory, thread, in-flight request, exception, and uptime figures. Latencies are milliseconds and may be `null`
until there is a sample. Counts are for one process instance, not a fleet-wide total.

The response contains aggregate numbers only, with no request paths, query strings, headers, usernames, SQL text, or connection
details. This endpoint is anonymous, readable from any origin, and not cached.
