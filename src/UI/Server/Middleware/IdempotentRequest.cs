using System.Security.Cryptography;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// A POST or PUT to the API that carries an <see cref="IdempotencyConstants.HeaderName"/>: its key, and the composite
/// key (method, path, key and a SHA-256 of the body) that identifies the exact request the key was first used for.
/// </summary>
internal sealed record IdempotentRequest(string Key, string CompositeKey)
{
    public static bool ShouldInspect(HttpRequest request) =>
        (HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method))
        && (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || ApiRateLimitingExtensions.ShouldApplyToPath(request.Path));

    public static string? ReadKey(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(IdempotencyConstants.HeaderName, out var keyValues))
        {
            return null;
        }

        var trimmed = keyValues.ToString().Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>Hashes the body (buffering it, so the endpoint can read it again) into the composite key.</summary>
    public static async Task<IdempotentRequest> ForAsync(HttpRequest request, string key, CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        var bodyHash = Convert.ToHexString(await SHA256.HashDataAsync(request.Body, cancellationToken));
        request.Body.Position = 0;
        return new IdempotentRequest(key, $"{request.Method}\u001f{request.Path.Value}\u001f{key}\u001f{bodyHash}");
    }
}
