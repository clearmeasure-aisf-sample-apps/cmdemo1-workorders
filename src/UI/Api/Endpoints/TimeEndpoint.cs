using System.Globalization;
using Microsoft.Net.Http.Headers;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// <c>GET api/time</c>: the server's current UTC instant as ISO-8601 plain text, never cached.
/// </summary>
public static class TimeEndpoint
{
    public static void Map(IEndpointRouteBuilder api) => api.MapGet("time", Get).AllowAnonymous();

    public static IResult Get(HttpContext context, TimeProvider timeProvider)
    {
        var text = timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
        context.Response.Headers[HeaderNames.CacheControl] = "no-store";
        return ConditionalGetEtag.Conditional(
            context,
            ConditionalGetEtag.CreateWeakEtagForPlainText(text),
            () => PlainText.Ok(text));
    }
}
