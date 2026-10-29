# Note 56

POST /api/tools/uppercase returns JSON whose text is the request text converted with string.ToUpperInvariant. A missing body, or a null or omitted text, yields an empty string. The same action is also routed at api/v{version:apiVersion}/tools/uppercase with API version 1.0.
