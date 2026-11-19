using Azure.Monitor.OpenTelemetry.AspNetCore;
using ClearMeasure.Bootcamp.UnitTests.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests;

[TestFixture]
public class TestHostTelemetryIsolationTests
{
    private const string ConnectionStringVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    private static readonly HttpClient NeverCompletingClient = new(new NeverCompletingHandler())
    {
        Timeout = TimeSpan.FromMilliseconds(200)
    };

    [Test]
    public void Should_ClearApplicationInsightsConnectionString_When_TestAssemblyStarts()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        connectionString.ShouldBeNull();
    }

    [Test]
    public async Task Should_NotRegisterAzureMonitor_When_TestHostStarts_AndVariableWasSetBeforeIsolation()
    {
        var originalValue = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        try
        {
            Environment.SetEnvironmentVariable(ConnectionStringVariable, $"InstrumentationKey={Guid.Empty}");
            new TestHostTelemetryIsolation().ClearApplicationInsightsConnectionString();

            await using var factory = new CorsEnabledApiWebApplicationFactory();
            using var client = factory.CreateClient();

            var azureMonitorRegistrations =
                factory.Services.GetService<IEnumerable<IConfigureOptions<AzureMonitorOptions>>>();
            azureMonitorRegistrations = azureMonitorRegistrations.ShouldNotBeNull();
            azureMonitorRegistrations.ToList().ShouldBeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionStringVariable, originalValue);
        }
    }

    [Test]
    public async Task Should_FailWithTimeout_When_RequestNeverCompletes()
    {
        await Should.ThrowAsync<TaskCanceledException>(() => NeverCompletingClient.GetAsync("http://localhost/"));
    }

    [Test]
    public async Task Should_ConfigureTestServerClientTimeout_When_ClientCreated()
    {
        await using var factory = new CorsEnabledApiWebApplicationFactory();
        using var client = factory.CreateClient();

        client.Timeout.ShouldBe(TestServerClientTimeout.Value);
    }

    private sealed class NeverCompletingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage();
        }
    }
}
