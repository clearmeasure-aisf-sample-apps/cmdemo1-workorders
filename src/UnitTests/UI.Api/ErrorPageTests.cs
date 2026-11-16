using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ErrorPageTests
{
    [Test]
    public async Task WriteAsync_Should_ShowRequestId_FromTraceIdentifier()
    {
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-8970" };
        httpContext.Response.Body = new MemoryStream();

        await ErrorPage.WriteAsync(httpContext);

        httpContext.Response.ContentType.ShouldBe("text/html; charset=utf-8");
        httpContext.Response.Body.Position = 0;
        var html = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        html.ShouldContain("<code>trace-8970</code>");
    }

    [Test]
    public void Render_Should_OmitRequestId_When_RequestIdEmpty()
    {
        ErrorPage.Render("").ShouldNotContain("Request ID");
    }

    [Test]
    public void Render_Should_EncodeRequestId()
    {
        ErrorPage.Render("<script>").ShouldContain("&lt;script&gt;");
    }

    [Test]
    public void Render_Should_UseInlineStylesAndNameTheAppInTheTitle()
    {
        var markup = ErrorPage.Render("trace");

        markup.ShouldContain("<title>Error - .NET Conf Work Orders</title>");
        markup.ShouldContain("<style>");
        markup.ShouldContain("text-danger");
        markup.ShouldNotContain("href=\"~/css/");
    }
}
