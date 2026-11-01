# API tools

Anonymous, rate-limited utility routes. Each route is also available under `/api/v1.0/tools/` (API version 1.0). The curl examples use the local UI.Server base URL `https://localhost:7174`.

## POST /api/tools/hash

Returns the SHA-256 of JSON `text` (UTF-8) as lowercase hex in `sha256`. Optional `includeMd5` and `includeSha1`, in the body or as query flags, add `md5` and `sha1`. Those properties are omitted when not requested. A missing `text` returns 400.

```bash
curl -s -X POST "https://localhost:7174/api/tools/hash" \
  -H "Content-Type: application/json" \
  -d '{"text":"hello"}'
```

## POST /api/tools/base64

Encodes or decodes JSON `text` (UTF-8). `mode` is `encode` (the default) or `decode`. Encode returns `{ "result": "<base64>" }`. Empty `text`, or any other `mode`, returns 400.

```bash
curl -s -X POST "https://localhost:7174/api/tools/base64" \
  -H "Content-Type: application/json" \
  -d '{"text":"hi","mode":"encode"}'
```

## POST /api/tools/guid-generator

Returns a JSON array of GUID strings in `D` format. Query `count` defaults to 1 and must be from 1 to 100.

```bash
curl -s -X POST "https://localhost:7174/api/tools/guid-generator?count=1"
```

## GET /api/tools/random

Returns plain text (`text/plain; charset=utf-8`). Query `type` is required: `number`, `string` (12 alphanumeric characters), `uuid`, or `color` (`#RRGGBB`). A missing or unknown `type` returns 400.

```bash
curl -s "https://localhost:7174/api/tools/random?type=uuid"
```

## POST /api/tools/slugify

Returns `{ "slug": "<slug>" }` from JSON `text`: runs of non-alphanumeric characters become a single hyphen, leading and trailing hyphens are removed, and the result is lowercase. A missing `text` returns 400.

```bash
curl -s -X POST "https://localhost:7174/api/tools/slugify" \
  -H "Content-Type: application/json" \
  -d '{"text":"Hello, World!"}'
```

## GET /api/tools/timestamp-converter

Supply exactly one of query `unix` (64-bit Unix seconds) or `iso` (ISO-8601). The JSON response has `unix`, `iso` (`yyyy-MM-ddTHH:mm:ssZ`), and `human` (`dddd, dd MMMM yyyy HH:mm:ss UTC`), using the invariant culture. Missing both, or sending both, returns 400.

```bash
curl -s "https://localhost:7174/api/tools/timestamp-converter?unix=0"
```

## POST /api/tools/word-count

Returns `{ "words": N, "characters": N, "lines": N }` for JSON `text`. Words are whitespace-delimited. A missing or empty `text` counts as zero.

```bash
curl -s -X POST "https://localhost:7174/api/tools/word-count" \
  -H "Content-Type: application/json" \
  -d '{"text":"hello world"}'
```

## POST /api/tools/case

Returns `{ "text": "<converted>" }`. JSON `to` is `upper`, `lower`, `title`, `kebab`, or `snake` (case-insensitive). A missing `text`, or an unknown `to`, returns 400.

```bash
curl -s -X POST "https://localhost:7174/api/tools/case" \
  -H "Content-Type: application/json" \
  -d '{"text":"Hello","to":"lower"}'
```

## GET /api/tools/due-date-check

Returns `{ "urgency": "<name>" }` for required query `date` (`YYYY-MM-DD`). Urgency is `None`, `DueToday`, or `Overdue`, relative to today in America/Chicago, treating the work order as open (Draft). A missing or unparseable `date` returns 400.

```bash
curl -s "https://localhost:7174/api/tools/due-date-check?date=2026-01-15"
```

## POST /api/tools/uppercase

Returns `{ "text": "<upper>" }` using the invariant culture. A missing body or `text` is treated as empty and returns `{ "text": "" }`.

```bash
curl -s -X POST "https://localhost:7174/api/tools/uppercase" \
  -H "Content-Type: application/json" \
  -d '{"text":"Hello"}'
```

## GET /api/tools/work-order-statuses

Returns a JSON array of `{ "code", "key", "friendlyName", "sortBy" }`, one entry per work order status, ordered by `sortBy`.

```bash
curl -s "https://localhost:7174/api/tools/work-order-statuses"
```
