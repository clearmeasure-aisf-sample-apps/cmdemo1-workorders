using System.Security.Cryptography;
using System.Text;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, IOptions<ApiKeyAuthenticationOptions> optionsAccessor) =>
        ApiKeyAuthenticationPipeline.InvokeAsync(context, next, optionsAccessor.Value);

    internal static bool ShouldValidate(PathString path, ApiKeyAuthenticationOptions options) =>
        ApiKeyValidationRules.RequiresValidation(path.Value, options);

    internal static bool IsApiPath(string pathValue) =>
        pathValue.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
        || pathValue.Equals("/api", StringComparison.OrdinalIgnoreCase);

    internal static bool IsPublicVersionOrTimePath(string pathValue) =>
        ApiPublicPathRules.TryGetLeafSegment(pathValue, out var leaf) && ApiPublicPathRules.IsPublicLeaf(leaf);

    internal static bool IsAuthorized(HttpRequest request, string expectedKey)
    {
        if (!request.Headers.TryGetValue(ApiKeyConstants.HeaderName, out var providedValues))
        {
            return false;
        }

        var provided = providedValues.FirstOrDefault();
        return !string.IsNullOrEmpty(provided) && FixedTimeEqualsUtf8(expectedKey, provided);
    }

    private static bool FixedTimeEqualsUtf8(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        if (expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
