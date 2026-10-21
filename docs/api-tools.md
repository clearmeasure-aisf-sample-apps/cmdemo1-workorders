# API Tools

- **POST `/api/tools/hash`** computes a SHA-256 hash of UTF-8 text and can optionally include MD5 and SHA-1 hashes.
- **POST `/api/tools/base64`** encodes or decodes UTF-8 text using Base64.
- **POST `/api/tools/guid-generator`** generates between 1 and 100 UUIDs, with an optional count parameter.
- **GET `/api/tools/random`** returns a random number, 12-character alphanumeric string, UUID, or hexadecimal color based on the required `type` parameter.
- **POST `/api/tools/slugify`** converts text to a lowercase URL slug, replacing each run of non-alphanumeric characters with a hyphen.
- **GET `/api/tools/timestamp-converter`** converts between Unix seconds and ISO-8601 timestamps using exactly one of the `unix` or `iso` parameters.
