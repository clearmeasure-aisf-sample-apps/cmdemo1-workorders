# Note 168

POST `/_diagnostics/reset-db-connections` calls `IDatabaseConfiguration.ResetConnectionPool()` and returns HTTP 200 with the body `Connection pools cleared`. `DiagnosticController` in `src/UI/Api/Controllers/DiagnosticController.cs` is mapped with `[Route("_diagnostics")]`, and `ResetDbConnections` is mapped with `[HttpPost("reset-db-connections")]` and returns `Ok("Connection pools cleared")` after `databaseConfiguration.ResetConnectionPool()`.
