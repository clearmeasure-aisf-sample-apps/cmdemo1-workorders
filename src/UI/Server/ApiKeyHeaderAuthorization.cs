using System.Security.Cryptography;
using System.Text;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Reads the <c>X-Api-Key</c> request header and compares it to the expected key in constant
/// time. Shared by every pipeline that authenticates via the header.
/// </summary>
internal sealed class ApiKeyHeaderAuthorization
{
    public static bool IsAuthorized(HttpRequest request, string expectedKey)
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
