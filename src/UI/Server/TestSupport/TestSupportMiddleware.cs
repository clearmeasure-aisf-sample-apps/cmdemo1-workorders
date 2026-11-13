using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>
/// Returns 404 for <c>/api/test-support</c> when disabled; otherwise requires the secret header.
/// </summary>
public sealed class TestSupportMiddleware(RequestDelegate next)
{
    /// <summary>Runs the gate for test-support paths, then continues the pipeline.</summary>
    public async Task InvokeAsync(HttpContext context, IOptions<TestSupportOptions> optionsAccessor)
    {
        if (!IsTestSupportPath(context.Request.Path.Value))
        {
            await next(context);
            return;
        }

        var options = optionsAccessor.Value;
        if (!options.Enabled)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!SecretMatches(context.Request, options.Secret))
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

    internal static bool SecretMatches(HttpRequest request, string expectedSecret)
    {
        if (string.IsNullOrEmpty(expectedSecret))
        {
            return false;
        }

        if (!request.Headers.TryGetValue(TestSupportConstants.SecretHeaderName, out var values))
        {
            return false;
        }

        var provided = values.FirstOrDefault();
        if (string.IsNullOrEmpty(provided))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return expectedBytes.Length == providedBytes.Length
               && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
