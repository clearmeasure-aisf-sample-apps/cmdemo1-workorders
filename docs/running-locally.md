# Running locally

To run the application locally with SQLite:

```bash
export DATABASE_ENGINE=SQLite
export ConnectionStrings__SqlConnectionString=""
dotnet test src/IntegrationTests --filter "ZDataLoader"
cd src/UI/Server
dotnet run --no-launch-profile --urls "https://localhost:7174;http://localhost:5174"
```

The application is available at https://localhost:7174. Check its health at
https://localhost:7174/_healthcheck.
