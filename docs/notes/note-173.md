# Note 173

DependencyCallListener.SqlClientListenerName is the public constant "SqlClientDiagnosticListener" (`public const string SqlClientListenerName = "SqlClientDiagnosticListener";`) in `src/UI/Server/LiveTelemetry/DependencyCallListener.cs`. `OnNext` ignores a diagnostic listener whose name is not that constant (`if (value.Name != SqlClientListenerName) { return; }`). That constant names the Microsoft.Data.SqlClient diagnostic listener the process uses for SQL command counts.
