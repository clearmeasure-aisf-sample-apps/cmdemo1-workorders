using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Read-only count of work orders per status, read from the database through the normal query path
/// (<see cref="IBus"/> → <see cref="WorkOrderCountByStatusQuery"/> → EF Core). Anonymous and a plain GET, so a
/// browser can call it with <c>fetch(url, { mode: 'no-cors' })</c> to make representative traffic. Not under the
/// API sliding-window rate limit: a dashboard sends about two requests a second from one client for a minute.
/// </summary>
public static class WorkOrderStatusCountsEndpoint
{
    public static void Map(IEndpointRouteBuilder api) =>
        api.MapGet("work-orders/status-counts", Get).AllowAnonymous();

    /// <summary>
    /// Returns <c>{ "Draft": n, "Assigned": n, ... }</c> keyed by status key, every status present. Never cached.
    /// </summary>
    public static async Task<IResult> Get(HttpResponse response, IBus bus)
    {
        response.Headers.CacheControl = "no-store,no-cache";
        response.Headers.Pragma = "no-cache";
        var counts = await bus.Send(new WorkOrderCountByStatusQuery());
        return TypedResults.Ok(counts);
    }
}
