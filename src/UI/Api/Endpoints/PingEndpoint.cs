namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// <c>GET api/ping</c>: plain-text <c>pong</c> for connectivity checks.
/// </summary>
public static class PingEndpoint
{
    public static void Map(IEndpointRouteBuilder api) => api.MapGet("ping", Get).AllowAnonymous();

    public static IResult Get(HttpResponse response)
    {
        response.Headers["X-Content-Type-Options"] = "nosniff";
        return PlainText.Ok("pong");
    }
}
