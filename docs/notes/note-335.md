# Note 335

`IdempotencyOptions.CacheEntrySeconds` in `src/UI/Server/Middleware/IdempotencyOptions.cs` is initialized to 86400 (`public int CacheEntrySeconds { get; set; } = 86400;`). That value is the number of seconds a successful idempotency response snapshot is retained. The property's XML comment states the default is 24 hours.
