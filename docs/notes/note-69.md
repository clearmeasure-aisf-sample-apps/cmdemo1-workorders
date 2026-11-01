# Note 69

GET `/api/ping` and GET `/api/v1.0/ping` return the plain-text body `pong` with content type `text/plain; charset=utf-8` and status 200. `PingController.Get` in `src/UI/Api/Controllers/PingController.cs` returns that `ContentResult`. The versioned route is `api/v{version:apiVersion}/ping` with API version 1.0.
