# CQRS Request Flow

A typical read or write request follows this path:

```text
UI → API controller → IBus.Send(query or command) → MediatR
   → DataAccess handler → DataContext (EF Core) → database
```

1. The UI sends an HTTP request to the API.
2. The API controller maps the request to a query or command and sends it through `IBus`.
3. `IBus` forwards the request to MediatR.
4. MediatR dispatches it to the matching handler in `DataAccess`.
5. The handler reads or updates data through `DataContext`, which uses EF Core to access the database.
6. The handler's result returns through MediatR and `IBus` to the API, which responds to the UI.
