# API Tools

- **POST `/api/tools/hash`** computes a SHA-256 hash of UTF-8 text and can optionally include MD5 and SHA-1 hashes.
- **POST `/api/tools/base64`** encodes or decodes UTF-8 text using Base64.
- **POST `/api/tools/guid-generator`** generates between 1 and 100 UUIDs, with an optional count parameter.
- **GET `/api/tools/random`** returns a random number, 12-character alphanumeric string, UUID, or hexadecimal color based on the required `type` parameter.
- **POST `/api/tools/slugify`** converts text to a lowercase URL slug, replacing each run of non-alphanumeric characters with a hyphen.
- **POST `/api/tools/uppercase`** upper-cases JSON `text` with the invariant culture; empty or missing `text` returns `{ "text": "" }`.
- **POST `/api/tools/word-count`** counts whitespace-delimited words, characters, and lines in JSON `text`.
- **POST `/api/tools/case`** converts JSON `text` to the case named by `to` (`upper`, `lower`, `title`, `kebab`, or `snake`); an unknown `to` or missing `text` returns 400.
- **GET `/api/tools/timestamp-converter`** converts between Unix seconds and ISO-8601 timestamps using exactly one of the `unix` or `iso` parameters.
- **GET `/api/tools/due-date-check`** returns due-date urgency for required `date` (`YYYY-MM-DD`), relative to today in America/Chicago, assuming an open work order.
- **GET `/api/tools/work-order-statuses`** lists work order statuses (`code`, `key`, `friendlyName`, `sortBy`) in sort order.
