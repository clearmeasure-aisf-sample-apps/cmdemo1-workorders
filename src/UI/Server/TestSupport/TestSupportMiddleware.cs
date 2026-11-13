namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>
/// Returns 404 for <c>/api/test-support</c> when disabled; otherwise requires the secret header.
/// </summary>
public sealed class TestSupportMiddleware(RequestDelegate next)
{
    /// <summary>Runs the gate for test-support paths, then continues the pipeline.</summary>
    public async Task InvokeAsync(HttpContext context, TestSupportOptions options)
    {
        if (!IsTestSupportPath(context.Request.Path.Value))
        {
            await next(context);
            return;
        }

        if (!options.Enabled)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!TestSupportSecret.Matches(context.Request, options.Secret))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    internal static bool IsTestSupportPath(string? pathValue)
    {
        if (string.IsNullOrEmpty(pathValue))
        {
            return false;
        }

        return pathValue.Equals(TestSupportConstants.PathPrefix, StringComparison.OrdinalIgnoreCase)
               || pathValue.StartsWith(TestSupportConstants.PathPrefix + "/", StringComparison.OrdinalIgnoreCase);
    }
}
