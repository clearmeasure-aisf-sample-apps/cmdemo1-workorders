using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class DiagnosticEndpointAccessPipelineTests
{
    private const string Key = "diagnostic-key";

    [Test]
    public async Task Should_CallNext_When_PathIsNotDiagnostic()
    {
        var context = CreateContext("/api/health");
        var next = new NextRecorder();

        await DiagnosticEndpointAccessPipeline.InvokeAsync(context, next.Invoke, KeyOn(), Environment(Environments.Production));

        next.Calls.ShouldBe(1);
    }

    [Test]
    public async Task Should_Return401_When_KeyConfiguredAndHeaderMissing()
    {
        var context = CreateContext("/_diagnostics");
        var next = new NextRecorder();

        await DiagnosticEndpointAccessPipeline.InvokeAsync(context, next.Invoke, KeyOn(), Environment(Environments.Development));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        next.Calls.ShouldBe(0);
    }

    [Test]
    public async Task Should_CallNext_When_KeyConfiguredAndHeaderMatches()
    {
        var context = CreateContext("/_lamar");
        context.Request.Headers["X-Api-Key"] = Key;
        var next = new NextRecorder();

        await DiagnosticEndpointAccessPipeline.InvokeAsync(context, next.Invoke, KeyOn(), Environment(Environments.Production));

        next.Calls.ShouldBe(1);
    }

    [Test]
    public async Task Should_CallNext_When_KeyOffInDevelopment()
    {
        var context = CreateContext("/_diagnostics");
        var next = new NextRecorder();

        await DiagnosticEndpointAccessPipeline.InvokeAsync(context, next.Invoke, new ApiKeyAuthenticationOptions(), Environment(Environments.Development));

        next.Calls.ShouldBe(1);
    }

    [Test]
    public async Task Should_Return404_When_KeyOffInProduction()
    {
        var context = CreateContext("/_diagnostics");
        var next = new NextRecorder();

        await DiagnosticEndpointAccessPipeline.InvokeAsync(context, next.Invoke, new ApiKeyAuthenticationOptions(), Environment(Environments.Production));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        next.Calls.ShouldBe(0);
    }

    private static DefaultHttpContext CreateContext(string path) =>
        new() { Request = { Path = path }, Response = { Body = new MemoryStream() } };

    private static ApiKeyAuthenticationOptions KeyOn() => new() { Enabled = true, ValidationKey = Key };

    private static HostingEnvironment Environment(string name) => new() { EnvironmentName = name };
}
