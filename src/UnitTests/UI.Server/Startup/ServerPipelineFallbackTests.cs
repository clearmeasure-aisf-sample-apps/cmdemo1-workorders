using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Startup;

/// <summary>
/// <see cref="ServerPipeline.FallbackToIndexHtml"/> against a <see cref="DefaultHttpContext"/>, with no app host.
/// </summary>
[TestFixture]
public class ServerPipelineFallbackTests
{
    [Test]
    public async Task ShouldReturn404_WhenTheUnknownPathIsAnApiPath()
    {
        var context = Get("/api/does-not-exist", new NullFileProvider());

        await ServerPipeline.FallbackToIndexHtml(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        ((MemoryStream)context.Response.Body).Length.ShouldBe(0);
    }

    [Test]
    public async Task ShouldReturn404_WhenTheWebRootHasNoIndexHtml()
    {
        var context = Get("/some/page", new NullFileProvider());

        await ServerPipeline.FallbackToIndexHtml(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task ShouldServeIndexHtml_WhenTheUnknownPathIsAPage()
    {
        var webRoot = Directory.CreateTempSubdirectory("fallback-webroot-");
        await File.WriteAllTextAsync(Path.Combine(webRoot.FullName, "index.html"), "<html>app</html>");
        using var files = new PhysicalFileProvider(webRoot.FullName);
        var context = Get("/workorder/manage", files);

        await ServerPipeline.FallbackToIndexHtml(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        context.Response.ContentType.ShouldBe("text/html; charset=utf-8");
        System.Text.Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()).ShouldBe("<html>app</html>");
        webRoot.Delete(recursive: true);
    }

    private static DefaultHttpContext Get(string path, IFileProvider webRoot)
    {
        var services = new ServiceCollection()
            .AddSingleton<IWebHostEnvironment>(new WebRootOnlyEnvironment { WebRootFileProvider = webRoot })
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services, Response = { Body = new MemoryStream() } };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        return context;
    }

    private sealed class WebRootOnlyEnvironment : IWebHostEnvironment
    {
        public required IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "fallback-tests";
    }
}
