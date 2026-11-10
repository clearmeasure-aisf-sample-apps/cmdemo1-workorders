// ReSharper disable NotAccessedPositionalProperty.Local -- problem-details records are serialized onto the HTTP response
using System.Text.Json;
using FluentValidation.Results;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Writes an <c>application/problem+json</c> response from middleware that answers before MVC runs: a plain problem
/// (status, title, detail), or a 400 listing validation errors by property.
/// </summary>
internal static class ProblemDetailsWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, int status, string title, string? detail) =>
        WriteAsync(context, new ProblemDetailsDto(status, title, detail, null));

    public static Task WriteValidationProblemAsync(HttpContext context, IEnumerable<ValidationFailure> failures) =>
        WriteAsync(context, new ProblemDetailsDto(
            StatusCodes.Status400BadRequest,
            "One or more validation errors occurred.",
            null,
            failures
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray())));

    private static async Task WriteAsync(HttpContext context, ProblemDetailsDto problem)
    {
        context.Response.StatusCode = problem.Status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(context.Response.Body, problem, JsonOptions, context.RequestAborted);
    }

    private sealed record ProblemDetailsDto(int Status, string Title, string? Detail, Dictionary<string, string[]>? Errors);
}
