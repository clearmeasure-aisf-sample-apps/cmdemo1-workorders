# Running locally

## Build

From the repository root, build the solution:

```bash
dotnet build src/ChurchBulletin.sln --configuration Release
```

## Run UI.Server

The application needs a database. On Windows, configure SQL Server LocalDB and run the Server project:

```powershell
cd src/UI/Server
dotnet run
```

On Linux or macOS, use the repository's SQL Server Docker setup, or select SQLite when Docker is unavailable. For SQLite, set the
environment and seed reference data before starting the Server:

```bash
export DATABASE_ENGINE=SQLite
export ConnectionStrings__SqlConnectionString=""
dotnet test src/IntegrationTests --filter "ZDataLoader"
cd src/UI/Server
dotnet run --no-launch-profile --urls "https://localhost:7174;http://localhost:5174"
```

Use `--no-launch-profile` on Linux and macOS to avoid the Windows LocalDB connection string in the launch settings.

The health check is available at https://localhost:7174/_healthcheck.

## Run tests

From the repository root:

```bash
dotnet test src/UnitTests --configuration Release
dotnet test src/IntegrationTests --configuration Release
```
