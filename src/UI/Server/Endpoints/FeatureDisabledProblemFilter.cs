using ClearMeasure.Bootcamp.Core.FeatureFlags;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Server.Endpoints;

/// <summary>
/// Maps <see cref="FeatureDisabledException"/> from the bus to a 403 RFC 7807 problem response naming the
/// disabled feature. Other exceptions are left to the regular error pipeline.
/// </summary>
public sealed class FeatureDisabledProblemFilter : IEndpointFilter
{
    /// <summary>Problem type URI for a forbidden response.</summary>
    public const string ProblemType = "https://tools.ietf.org/html/rfc9110#section-15.5.4";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (FeatureDisabledException disabled)
        {
            return Problem(disabled, context.HttpContext);
        }
    }

    /// <summary>The 403 problem naming the feature <paramref name="disabled"/> reports.</summary>
    public static IResult Problem(FeatureDisabledException disabled, HttpContext httpContext) =>
        TypedResults.Problem(new ProblemDetails
        {
            Type = ProblemType,
            Title = "Feature disabled",
            Status = StatusCodes.Status403Forbidden,
            Detail = disabled.Message,
            Instance = httpContext.Request.Path,
            Extensions = { ["feature"] = disabled.FeatureName }
        });
}
