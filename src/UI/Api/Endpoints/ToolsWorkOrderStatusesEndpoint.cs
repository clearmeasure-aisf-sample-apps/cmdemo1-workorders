using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Returns all valid work order statuses for integrators and operators.
/// </summary>
public static class ToolsWorkOrderStatusesEndpoint
{
    public static void Map(IEndpointRouteBuilder tools) =>
        tools.MapGet("work-order-statuses", Get).AllowAnonymous();

    /// <summary>
    /// Returns all valid work order statuses (code, key, friendlyName, sortBy) in sort order.
    /// </summary>
    public static IResult Get()
    {
        var statuses = WorkOrderStatus.GetAllItems()
            .OrderBy(s => s.SortBy)
            .Select(s => new WorkOrderStatusDto(s.Code, s.Key, s.FriendlyName, s.SortBy))
            .ToArray();

        return TypedResults.Ok(statuses);
    }
}

/// <summary>
/// Represents a single work order status entry for the discovery endpoint.
/// </summary>
public record WorkOrderStatusDto(string Code, string Key, string FriendlyName, byte SortBy);
