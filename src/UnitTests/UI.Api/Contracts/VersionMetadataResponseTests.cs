using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the version body clients read back through the source-generated <see cref="ApiJsonContext"/>.</summary>
[TestFixture]
public class VersionMetadataResponseTests
{
    [Test]
    public void Deserialize_Should_LeaveOptionalVersionsNull_When_BodyOmitsThem()
    {
        const string json = """
            {"buildConfiguration":"Release","machineName":"box","frameworkDescription":".NET 11.0.0","osDescription":"Linux"}
            """;

        var version = JsonSerializer.Deserialize(json, ApiJsonContext.Default.VersionMetadataResponse)!;

        version.BuildConfiguration.ShouldBe("Release");
        version.AssemblyVersion.ShouldBeNull();
        version.Environment.ShouldBeNull();
    }
}
