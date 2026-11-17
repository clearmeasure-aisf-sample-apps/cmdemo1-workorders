using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

/// <summary><see cref="ApiEndpoints"/> maps each contract endpoint at the plain and the versioned prefix.</summary>
[TestFixture]
public class ApiEndpointsTests
{
    [TestCase("ping")]
    [TestCase("version")]
    [TestCase("tools/base64")]
    [TestCase("work-orders/status-counts")]
    public void MapApiEndpoints_Should_MapTheRouteUnderBothPrefixes(string route)
    {
        MappedApiEndpoints.At($"api/{route}").ShouldNotBeEmpty();
        MappedApiEndpoints.At($"{ApiRoutes.VersionedApiPrefix}/{route}").ShouldNotBeEmpty();
    }
}
