namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Problem responses the API endpoints return (RFC 7807, <c>application/problem+json</c>).
/// </summary>
public static class ApiProblems
{
    /// <summary>400 Bad Request naming what was wrong with the request.</summary>
    public static IResult BadRequest(string? detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);
}
