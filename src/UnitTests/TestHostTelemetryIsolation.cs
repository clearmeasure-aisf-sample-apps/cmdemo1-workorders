namespace ClearMeasure.Bootcamp.UnitTests;

/// <summary>
/// Clears the APPLICATIONINSIGHTS_CONNECTION_STRING environment variable for the whole unit-test process
/// so WebApplicationFactory test hosts never register Azure Monitor (several OpenTelemetry TracerProviders
/// in one process threw 'already contains item with same key microsoft.sample_rate' inside TestServer and
/// hung the request); the original value is restored on teardown.
/// </summary>
[SetUpFixture]
public sealed class TestHostTelemetryIsolation
{
    private const string VariableName = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    private string? _originalValue;

    [OneTimeSetUp]
    public void ClearApplicationInsightsConnectionString()
    {
        _originalValue = Environment.GetEnvironmentVariable(VariableName);
        Environment.SetEnvironmentVariable(VariableName, null);
    }

    [OneTimeTearDown]
    public void RestoreApplicationInsightsConnectionString() =>
        Environment.SetEnvironmentVariable(VariableName, _originalValue);
}
