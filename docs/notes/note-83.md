# Note 83

POST `/api/tools/base64` treats a missing, null, or whitespace `mode` as `encode`, encodes `text` as UTF-8, and returns that Base64 string in JSON `result`. `ToolsBase64Controller.Post` in `src/UI/Api/Controllers/ToolsBase64Controller.cs` sets `mode` to `"encode"` when `string.IsNullOrWhiteSpace(request.Mode)` is true, then the `"encode"` arm returns `Ok(new Base64Response(Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Text))))`. `docs/api/tools.md` states that `mode` is `encode` (the default) or `decode`, and that encode returns `{ "result": "<base64>" }`.
