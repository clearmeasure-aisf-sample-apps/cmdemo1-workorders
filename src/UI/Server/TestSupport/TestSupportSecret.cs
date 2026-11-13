using System.Security.Cryptography;
using System.Text;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Fixed-time comparison of the test-support secret header to the configured value.</summary>
public static class TestSupportSecret
{
    /// <summary>
    /// Returns true when the request carries <see cref="TestSupportConstants.SecretHeaderName"/>
    /// equal to <paramref name="expectedSecret"/> (UTF-8, fixed-time).
    /// </summary>
    public static bool Matches(HttpRequest request, string expectedSecret)
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
