using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class VersionControllerTests
{
    [Test]
    public void Get_Should_ReturnOk_WithExpectedShape()
    {
        var stubHostEnvironment = new StubHostEnvironment("TestEnvironment");
        var controller = CreateController(stubHostEnvironment);

        var result = controller.Get();

        var content = result.ShouldBeContentResult();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload.AssemblyVersion.ShouldNotBeNullOrEmpty();
        payload.InformationalVersion.ShouldNotBeNullOrEmpty();
        payload.BuildConfiguration.ShouldNotBeNullOrEmpty();
        payload.Environment.ShouldBe("TestEnvironment");
        payload.MachineName.ShouldBe(Environment.MachineName);
        payload.FrameworkDescription.ShouldBe(RuntimeInformation.FrameworkDescription);
        payload.OsDescription.ShouldNotBeNullOrEmpty();
        payload.OsDescription.ShouldBe(RuntimeInformation.OSDescription);
    }

    [Test]
    public void Get_Should_IncludeBuildConfiguration_MatchingAssemblyAttribute()
    {
        var apiAssembly = typeof(VersionEndpoint).Assembly;
        var expected = apiAssembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        if (string.IsNullOrWhiteSpace(expected))
        {
#if DEBUG
            expected = "Debug";
#else
            expected = "Release";
#endif
        }

        var controller = CreateController(new StubHostEnvironment("Test"));
        var result = controller.Get();
        var content = result.ShouldBeContentResult();
        var payload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);

        payload.ShouldNotBeNull();
        payload.BuildConfiguration.ShouldBe(expected);
        payload.BuildConfiguration.ShouldBeOneOf("Debug", "Release");
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesPayloadEtag()
    {
        var controller = CreateController(new StubHostEnvironment("Test"));
        var first = controller.Get();
        first.ShouldBeContentResult();
        var etag = controller.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();

        controller.Request.Headers.IfNoneMatch = etag;
        var second = controller.Get();

        var status = second.ShouldBeStatusCodeResult();
        status.StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    private static EndpointUnderTest CreateController(IHostEnvironment hostEnvironment) =>
        new(context => VersionEndpoint.Get(context, hostEnvironment));

    private sealed class EndpointUnderTest(Func<HttpContext, IResult> get)
    {
        public HttpContext HttpContext { get; } = new DefaultHttpContext();
        public HttpRequest Request => HttpContext.Request;
        public HttpResponse Response => HttpContext.Response;
        public IResult Get() => get(HttpContext);
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
