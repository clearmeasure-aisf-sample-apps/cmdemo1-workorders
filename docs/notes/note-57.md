# Note 57

POST /api/tools/hash returns the SHA-256 of the JSON `text` field as lowercase hexadecimal of the UTF-8 bytes (`SHA256.HashData` and `Convert.ToHexString(...).ToLowerInvariant`). A missing body, or a null `text`, returns HTTP 400 with detail "JSON body field 'text' is required." The same action is also routed at `api/v{version:apiVersion}/tools/hash` (`ApiRoutes.VersionedApiPrefix`) with API version 1.0.
