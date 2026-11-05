using ClearMeasure.Bootcamp.Core.FeatureFlags;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClearMeasure.Bootcamp.UI.Server.Controllers;

/// <summary>
/// Maps <see cref="FeatureDisabledException"/> from the bus to a 403 RFC 7807 problem response naming the
/// disabled feature. Other exceptions are left to the regular error pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class FeatureDisabledProblemAttribute : ExceptionFilterAttribute
{
    /// <summary>Problem type URI for a forbidden response.</summary>
    public const string ProblemType = "https://tools.ietf.org/html/rfc9110#section-15.5.4";

    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not FeatureDisabledException disabled)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Type = ProblemType,
            Title = "Feature disabled",
            Status = StatusCodes.Status403Forbidden,
            Detail = disabled.Message,
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["feature"] = disabled.FeatureName;

        context.Result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status403Forbidden,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }
}
