# Note 179

SimpleHealthResponseBuilder.Build always sets SimpleHealthResponse.Status to SimpleHealthStatus.Healthy (`Status = SimpleHealthStatus.Healthy`). SimpleHealthStatus.Healthy is the constant `nameof(Healthy)`, so the status string is Healthy (`public const string Healthy = nameof(Healthy);`). The method is `Build` in `src/UI/Api/SimpleHealthResponseBuilder.cs`. The constant is `SimpleHealthStatus.Healthy` in `src/UI/Api/DetailedHealthModels.cs`.
