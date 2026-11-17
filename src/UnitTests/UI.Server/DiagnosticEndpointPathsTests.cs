using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class DiagnosticEndpointPathsTests
{
    [TestCase("/_lamar/services", false)]
    [TestCase("/_diagnostics/services", true)]
    [TestCase("/_diagnostics", true)]
    [TestCase("/_diagnostics/reset-db-connections", true)]
    [TestCase("/_DIAGNOSTICS/reset-db-connections", true)]
    [TestCase("/api/diagnostics", false)]
    [TestCase("/api/health", false)]
    [TestCase("/_healthcheck", false)]
    [TestCase("/_telemetry", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void Matches_ReturnsExpected_ForPath(string? path, bool expected) =>
        DiagnosticEndpointPaths.Matches(path).ShouldBe(expected);

    [TestCase("Development", true)]
    [TestCase("tdd", true)]
    [TestCase("TDD", true)]
    [TestCase("Testing", false)]
    [TestCase("Production", false)]
    [TestCase("uat", false)]
    public void AllowsAnonymousWhenApiKeyOff_ReturnsExpected_ForEnvironment(
        string environmentName,
        bool expected) =>
        DiagnosticEndpointPaths.AllowsAnonymousWhenApiKeyOff(new StubHostEnvironment(environmentName))
            .ShouldBe(expected);

    private sealed class StubHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "/";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
