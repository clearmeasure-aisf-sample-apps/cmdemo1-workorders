namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Configuration for optional <c>X-Api-Key</c> validation on HTTP API routes under <c>/api/</c>.
/// </summary>
public sealed class ApiKeyAuthenticationOptions
{
    public const string SectionName = "ApiKeyAuthentication";

    /// <summary>
    /// Turns the optional <c>X-Api-Key</c> scheme on or off; while <see cref="IsConfigured"/> holds, requests to protected API paths must send a matching <c>X-Api-Key</c> header.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Secret value callers must send in the <c>X-Api-Key</c> header. Leading and trailing whitespace is trimmed on
    /// assignment (configuration binding included); a null, empty or whitespace-only value is stored as <see langword="null"/>.
    /// </summary>
    public string? ValidationKey
    {
        get;
        init => field = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// The single rule for whether the scheme is active: <see cref="Enabled"/> with a non-blank <see cref="ValidationKey"/>. Shared by the
    /// <c>/api/*</c> API-key gate and the <c>/_lamar</c> + <c>/_diagnostics</c> gate.
    /// </summary>
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ValidationKey);

    /// <summary>
    /// Non-null validation key for gate comparison. Valid only when <see cref="IsConfigured"/> is true;
    /// otherwise returns an empty string that will never match any real header value.
    /// </summary>
    public string RequiredValidationKey => ValidationKey ?? string.Empty;
}
