using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// Pins the single segment-prefix dialect shared by the API, machine client, diagnostic and test-support
/// path matchers: equal or followed by a <c>/</c> segment, case-insensitive, never a bare string prefix.
/// </summary>
[TestFixture]
public class RequestPathPrefixTests
{
    [TestCase("/_lamar", true)]
    [TestCase("/_lamar/", true)]
    [TestCase("/_lamar/services", true)]
    [TestCase("/_LAMAR/Services", true)]
    [TestCase("/_lamarx", false)]
    [TestCase("/x/_lamar", false)]
    [TestCase("/", false)]
    [TestCase("", false)]
    public void IsUnder_ReturnsExpected_When_PathStringGiven(string path, bool expected)
    {
        RequestPathPrefix.IsUnder(new PathString(path), new PathString("/_lamar")).ShouldBe(expected);
    }

    [TestCase("/api/test-support", true)]
    [TestCase("/api/test-support/employees", true)]
    [TestCase("/api/test-supportive", false)]
    [TestCase("api/test-support", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void IsUnder_ReturnsExpected_When_RawPathValueGiven(string? pathValue, bool expected)
    {
        Should.NotThrow(() => RequestPathPrefix.IsUnder(pathValue, new PathString("/api/test-support")))
            .ShouldBe(expected);
    }
}
