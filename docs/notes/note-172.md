# Note 172

A request to `/ws/notifications` that is not a WebSocket upgrade returns HTTP 400. `RealtimeNotificationWebSocketMiddleware.Path` is the constant `/ws/notifications` (`public const string Path = "/ws/notifications";`) in `src/UI/Server/Notifications/RealtimeNotificationWebSocketMiddleware.cs`. `InvokeAsync` compares the request path to that constant with `StringComparison.OrdinalIgnoreCase` and, when `context.WebSockets.IsWebSocketRequest` is false, sets the response status to `StatusCodes.Status400BadRequest`.
