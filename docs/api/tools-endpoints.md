# Anonymous API tools

The following utility endpoints allow anonymous access. Each endpoint is available under both `/api/tools/...` and the versioned prefix `/api/v1.0/tools/...`.

| Method and route | Details |
| --- | --- |
| `POST /api/tools/hash` | Accepts JSON with a `text` field and returns its SHA-256 hash. Set `includeMd5` or `includeSha1` to `true` in the JSON body or query string to include those optional hashes. |
| `POST /api/tools/base64` | Accepts JSON with `text` and an optional `mode` (`encode` by default, or `decode`); encodes or decodes UTF-8 text. |
| `POST /api/tools/guid-generator` | Returns a JSON array of UUIDs. The optional `count` query parameter defaults to `1` and must be between `1` and `100`. |
| `GET /api/tools/random` | Requires the `type` query parameter: `number`, `string` (12 alphanumeric characters), `uuid`, or `color` (`#RRGGBB`). Returns plain text. |
| `POST /api/tools/slugify` | Accepts JSON with a `text` field and returns a lowercase slug, replacing runs of non-alphanumeric characters with a hyphen. |
| `GET /api/tools/timestamp-converter` | Supply exactly one of the `unix` (Unix seconds) or `iso` (ISO-8601 date/time) query parameters. Returns Unix seconds, an ISO-8601 UTC timestamp, and a human-readable UTC value. |

For the versioned route, replace the `/api` prefix in each table route with `/api/v1.0`. For example, the hash endpoint is `POST /api/v1.0/tools/hash`.
