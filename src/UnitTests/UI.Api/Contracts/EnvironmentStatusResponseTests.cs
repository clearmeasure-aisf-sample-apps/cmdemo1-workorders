using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the environment status body written through the source-generated <see cref="ApiJsonContext"/>.</summary>
[TestFixture]
public class EnvironmentStatusResponseTests
{
    [Test]
    public void Serialize_Should_WriteRedactedVariablesUnderCamelCaseNames()
    {
        var status = new EnvironmentStatusResponse
        {
            OsDescription = "Linux",
            ProcessorCount = 4,
            ClrVersion = "11.0.0",
            EnvironmentVariableNames = ["PATH"],
            EnvironmentVariables = new Dictionary<string, string> { ["PATH"] = "***" },
            Version = "2.4.1",
            GitSha = "abc1234",
            EnvironmentName = "tdd"
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(status, ApiJsonContext.Default.EnvironmentStatusResponse));

        json.RootElement.GetProperty("processorCount").GetInt32().ShouldBe(4);
        json.RootElement.GetProperty("environmentVariables").GetProperty("PATH").GetString().ShouldBe("***");
        json.RootElement.GetProperty("gitSha").GetString().ShouldBe("abc1234");
    }
}
