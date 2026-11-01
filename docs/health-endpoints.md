# Health and build endpoints

The UI.Server exposes these GET endpoints at its base URL. They provide health, build, and runtime information for deployment
checks and health dashboards.

## `/_healthcheck`

Returns the overall status of the registered application health checks as plain text. The first line is `Healthy`,
`Degraded`, or `Unhealthy`. The second line is `databaseVersion:` followed by the highest applied DbUp script number, or
`null` when the journal cannot be read. The checks cover dependencies and process health, including the LLM gateway,
database, API, and server. An unhealthy result is returned with an HTTP 503 status; healthy and degraded results use HTTP 200.

## `/_build`

Returns JSON describing the build running in the process. The response includes version, commit and build links and timestamp,
code size, test, coverage, complexity, CRAP, and static-analysis facts. Values the build could not determine are `null`.

Release builds include facts stamped into `build-facts.json`. When that file is unavailable, such as in a local run, the
endpoint still returns the same JSON shape with the assembly version and `null` for the other facts. Responses are publicly
cacheable for five minutes.

## `/_telemetry`

Returns a JSON snapshot of aggregate counters over a rolling 60-second window and vitals for the process answering the request.
It includes traffic and probe counts, request errors and latency, SQL command counts and latency, completed outgoing HTTP
calls, and process CPU, memory, thread, in-flight request, exception, and uptime figures. Latencies are milliseconds and may
be `null` until there is a sample.

Counts are for one process instance, not a fleet-wide total. The response contains aggregate numbers only: it does not include
request paths, query strings, headers, usernames, SQL text, or connection details. Responses are not cached.
