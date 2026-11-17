using System.Net;
using System.Text;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags;

/// <summary>Answers every request with one recorded Octopus response and keeps the requests it received.</summary>
public sealed class StubOctopusTogglesHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    /// <summary>Answers 200 with <paramref name="body"/>.</summary>
    public StubOctopusTogglesHandler(string body)
        : this(HttpStatusCode.OK, body)
    {
    }

    /// <summary>The requests received, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        response.Headers.Add("ContentHash", "AQ==");
        return Task.FromResult(response);
    }
}
