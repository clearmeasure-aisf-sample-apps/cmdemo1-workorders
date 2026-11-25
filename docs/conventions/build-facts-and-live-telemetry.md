# Build facts (`/_build`) and live telemetry (`/_telemetry`)

Read when touching `src/UI/Server/BuildFacts/`, `src/UI/Server/LiveTelemetry/`, `scripts/Write-BuildFacts.ps1`,
`WorkOrderStatusCountsController` or the release step that stamps build facts. Moved from `CLAUDE.md`. See also
[health-endpoints.md](../health-endpoints.md).

## Build facts

What the deployed process was built from and how good that build is (the demo-environment kit's CAP-079); anonymous,
no DB access, no MediatR, outside `/api`:

| Route | Method | Description |
|---|---|---|
| `/_build` | GET | `build-facts.json` of the content root. `Access-Control-Allow-Origin: *`, `Cache-Control: public, max-age=300` |

**Endpoint:** `src/UI/Server/BuildFacts/` (`AddBuildFacts`, `MapBuildFacts` in `ServerApplication`). The answer always
has `version`, `commit`, `commitUrl`, `builtAt`, `buildUrl`, `code` (lines of code per language), `tests`, `coverage`,
`complexity`, `crap`, `analysis`, each null when the build could not tell. Without the file (a local run) it has the
version of the assembly and nulls.

**Script:** `scripts/Write-BuildFacts.ps1` writes the file: the code from the checkout (`git ls-files`, non-blank
lines), the rest from the Build run's artifacts (`test-results-linux`, `test-results-acceptance`,
`code-coverage-linux`, `crap-metrics-linux`, `qodana-report`); a missing artifact is a null section, never a failed
release. The Release workflow (the kit's `release.yml`, step "Stamp the build facts into the image's files") runs it
with `-OutputPath built/build-facts.json` before `docker build`, so the image carries the file at its content root
(`/app/build-facts.json`; the Dockerfile copies `built/` to `/app`). Locally:
`pwsh -NoProfile -File scripts/Write-BuildFacts.ps1`.

## Live telemetry

The process's own counts of the last minute, for a health dashboard in another origin (the demo-environment kit's
CAP-077). Counted in-process, whether or not a telemetry exporter is configured:

| Route | Method | Description |
|---|---|---|
| `/_telemetry` | GET | Rolling 60 s counts: requests (Front Door / direct / errors / p95), probes, SQL commands (during requests / background / p95), outgoing HTTP calls; and `process`: the vitals of the process. Anonymous, outside `/api`, `Access-Control-Allow-Origin: *`, `Cache-Control: no-store` |
| `/api/work-orders/status-counts` | GET | Work-order count per status through `IBus` → `WorkOrderCountByStatusQuery` → EF Core: representative read traffic for the dashboard's traffic button. Anonymous (a public leaf in `ApiPublicPathRules`), not rate-limited, `no-store`. Also `/api/v1.0/work-orders/status-counts` |

**Code:** `src/UI/Server/LiveTelemetry/` (`AddLiveTelemetry`, `LiveTelemetryMiddleware`, `MapLiveTelemetry` in
`ServerApplication`); `src/UI/Api/Controllers/WorkOrderStatusCountsController.cs`.

**Numbers only.** `/_telemetry` answers counters, timings and process figures: no path, no query string, no header,
no user name, no SQL text, no host or connection detail. Keep it that way: any origin may read it without signing in.

Request classes (`RequestClassifier`): header `X-FD-HealthProbe: 1` → Front Door probe; `/_*` (except `/_framework`,
`/_content`), `/health`, `/alive` → probe; otherwise traffic, through Front Door when `X-Azure-FDID` is present.

`sql`: `perMinute` = `requests` (commands executed while an HTTP request was being handled, traffic or probe: the
flow `LiveTelemetryMiddleware` marks with `RequestFlow`) + `background` (the process's own: NServiceBus SQL-transport
polling, hosted services); `p95Ms` covers both. The source is Microsoft.Data.SqlClient's diagnostic events
(`DependencyCallListener`), so SQLite commands (tests, local fallback) are not counted.

`http`: completed outgoing HTTP calls (the runtime's `System.Net.Http` meter); telemetry export is left out.

`process`: `cpuPercent` (share of all processors over the last sampling interval: at least 2 s, sampled when a
request completes or the endpoint is read, never per request), `workingSetMb`, `gcHeapMb`, `threads` (thread pool),
`inFlight` (requests executing, the reading one included), `exceptionsPerMinute` (requests that ended in an unhandled
exception: escaped the pipeline, or answered by the exception handler; not first-chance exceptions) and
`uptimeSeconds` (since `startedAt`).

One process counts for itself: with more than one instance, an answer is one instance's numbers.
