# Note 183

When POST `/api/tools/base64` receives a `mode` that, after trim and invariant-culture lowercase, is neither `encode` nor `decode`, `ToolsBase64Controller.Post` returns HTTP 400 with detail `JSON body field 'mode' must be 'encode' or 'decode'.` The method is `Post` in `src/UI/Api/Controllers/ToolsBase64Controller.cs`.
