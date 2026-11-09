using System.Net;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class DeploymentIdentityTests
{
    [Test]
    public async Task ShouldReadTheCommitAndEnvironment_WhenTheServerReportsThem()
    {
        using var http = Http(HttpStatusCode.OK, """{ "gitSha": "0123456789abcdef", "environmentName": "tdd" }""");

        var identity = await DeploymentIdentity.LoadAsync(http);

        identity.ShouldBe(new DeploymentIdentity("0123456789abcdef", "tdd"));
        identity.ShortSha.ShouldBe("0123456");
    }

    [TestCase(HttpStatusCode.InternalServerError, "{}")]
    [TestCase(HttpStatusCode.OK, "not json")]
    [TestCase(HttpStatusCode.OK, "{}")]
    public async Task ShouldBeUnreported_WhenTheServerCannotSay(HttpStatusCode status, string body)
    {
        using var http = Http(status, body);

        var identity = await DeploymentIdentity.LoadAsync(http);

        identity.ShouldBe(DeploymentIdentity.Unreported);
        identity.ShortSha.ShouldBeNull();
    }

    private static HttpClient Http(HttpStatusCode status, string body) =>
        new(new StubHandler(status, body)) { BaseAddress = new Uri("https://localhost/") };

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
