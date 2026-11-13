using System.Text.Json;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Builders;

/// <summary>Builds Playwright storage-state JSON that signs in without the login employee list.</summary>
public static class PlaywrightSignInState
{
    /// <summary>
    /// Writes a temp storage-state file for <paramref name="userName"/> at <paramref name="origin"/> and returns its path.
    /// </summary>
    public static async Task<string> WriteTempFileAsync(string userName, string origin)
    {
        var payload = new
        {
            cookies = Array.Empty<object>(),
            origins = new[]
            {
                new
                {
                    origin = origin.TrimEnd('/'),
                    localStorage = new[]
                    {
                        new
                        {
                            name = TestSupportClient.UserSessionStorageKey,
                            value = userName
                        }
                    }
                }
            }
        };

        var path = Path.Combine(Path.GetTempPath(), $"pw-signin-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload));
        return path;
    }
}
