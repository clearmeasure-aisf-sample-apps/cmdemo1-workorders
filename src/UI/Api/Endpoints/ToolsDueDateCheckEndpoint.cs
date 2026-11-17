using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Stateless date-check utility endpoint — returns the due-date urgency for a given date.
/// </summary>
public static class ToolsDueDateCheckEndpoint
{
    public static void Map(IEndpointRouteBuilder tools) =>
        tools.MapGet("due-date-check", Get).AllowAnonymous();

    /// <summary>
    /// Returns the <see cref="DueDateUrgency"/> for the supplied <paramref name="date"/> relative
    /// to today in America/Chicago. Assumes an open work order (Draft status) to produce
    /// meaningful urgency results.
    /// </summary>
    public static IResult Get(string? date, TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return ApiProblems.BadRequest("Query parameter 'date' is required (format: YYYY-MM-DD).");
        }

        if (!DateOnly.TryParseExact(date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return ApiProblems.BadRequest($"Invalid date '{date}'. Expected format: YYYY-MM-DD (e.g. 2025-01-15).");
        }

        var urgency = DueDateUrgencyCalculator.Calculate(
            parsedDate,
            WorkOrderStatus.Draft,
            timeProvider);

        return TypedResults.Ok(new DueDateCheckResponse(urgency.ToString()));
    }
}

/// <summary>
/// JSON payload for <c>GET /api/tools/due-date-check</c>.
/// </summary>
public record DueDateCheckResponse(string Urgency);
