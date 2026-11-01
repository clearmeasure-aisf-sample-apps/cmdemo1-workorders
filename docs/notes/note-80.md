# Note 80

POST `/api/tools/case` returns HTTP 400 with detail "JSON body field 'to' must be 'upper', 'lower', 'title', 'kebab', or 'snake'." when `to` is null, whitespace, or not one of those five names after trim. `TryConvert` returns false in those cases, and `UnknownTo` returns that problem. The name lookup is case-insensitive. The same POST is also routed at `api/v{version:apiVersion}/tools/case` with API version 1.0. `docs/api/tools.md` states that an unknown `to` returns 400.
