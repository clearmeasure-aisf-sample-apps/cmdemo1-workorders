# Anonymous API tools

These utility endpoints are anonymous and rate-limited. Each is available at the listed `/api/tools/...` route and under the versioned `/api/v1.0/tools/...` prefix.

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/tools/hash` | Hashes the JSON body's `text` as UTF-8 with SHA-256. Set `includeMd5` and/or `includeSha1` in the body or query string to include optional hashes. |
| POST | `/api/tools/base64` | Encodes or decodes the JSON body's `text` as UTF-8 Base64. The optional `mode` is `encode` (default) or `decode`. |
| POST | `/api/tools/guid-generator` | Returns a JSON array of GUIDs. The optional `count` query parameter defaults to 1 and accepts values from 1 to 100. |
| GET | `/api/tools/random` | Returns a plain-text random value. The required `type` query parameter is `number`, `string` (12 alphanumeric characters), `uuid`, or `color` (`#RRGGBB`). |
| POST | `/api/tools/slugify` | Converts the JSON body's `text` to a lowercase URL slug, replacing runs of non-alphanumeric characters with a hyphen. |
| GET | `/api/tools/timestamp-converter` | Converts between Unix seconds and ISO-8601. Supply exactly one `unix` or `iso` query parameter; the response includes both representations and a human-readable UTC value. |

For example, the versioned hash route is `/api/v1.0/tools/hash`. The versioned API route template uses `api/v{version:apiVersion}`.
