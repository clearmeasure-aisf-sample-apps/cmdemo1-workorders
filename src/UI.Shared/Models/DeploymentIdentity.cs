using System.Text.Json;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// Which build and environment the server runs, as <c>/api/status/environment</c> reports them; "unknown" when the
/// server cannot be reached or answers without them.
/// </summary>
public sealed record DeploymentIdentity(string GitSha, string EnvironmentName)
{
    private const string Unknown = "unknown";

    public static DeploymentIdentity Unreported { get; } = new(Unknown, Unknown);

    /// <summary>The short commit shown in the footer, or <c>null</c> when the commit is not known.</summary>
    public string? ShortSha => GitSha != Unknown && GitSha.Length >= 7 ? GitSha[..7] : null;

    public static async Task<DeploymentIdentity> LoadAsync(HttpClient http)
    {
        try
        {
            var response = await http.GetAsync("/api/status/environment");
            return response.IsSuccessStatusCode ? await ParseAsync(response.Content) : Unreported;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            return Unreported;
        }
    }

    private static async Task<DeploymentIdentity> ParseAsync(HttpContent content)
    {
        await using var stream = await content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return new DeploymentIdentity(Text(doc.RootElement, "gitSha"), Text(doc.RootElement, "environmentName"));
    }

    private static string Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) ? value.GetString() ?? Unknown : Unknown;
}
