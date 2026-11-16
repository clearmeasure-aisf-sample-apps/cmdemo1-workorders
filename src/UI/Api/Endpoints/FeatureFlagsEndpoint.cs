using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// <c>GET api/features/flags</c>: the runtime feature flags and whether each is on.
/// </summary>
public static class FeatureFlagsEndpoint
{
    public static void Map(IEndpointRouteBuilder api) => api.MapGet("features/flags", Get);

    public static async Task<IResult> Get(IBus bus)
    {
        var result = await bus.Send(new FeatureFlagsQuery());
        return ConditionalGetEtag.JsonContent(result.Flags);
    }
}
