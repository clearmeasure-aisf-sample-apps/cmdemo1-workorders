using ClearMeasure.Bootcamp.UI.Server;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// Named <see cref="ApiKeyAuthenticationOptions"/> states for the API-key fixtures, so each test reads with the option state it needs.
/// </summary>
internal static class ApiKeyAuthenticationOptionsMother
{
    /// <summary>
    /// The validation key configured by <see cref="Enabled"/> and <see cref="Disabled"/>; send it as <c>X-Api-Key</c> to match.
    /// </summary>
    public const string ValidationKey = "secret";

    /// <summary>
    /// The scheme is on with a non-blank key: protected API paths require a matching <c>X-Api-Key</c>.
    /// </summary>
    public static ApiKeyAuthenticationOptions Enabled() => new() { Enabled = true, ValidationKey = ValidationKey };

    /// <summary>
    /// The scheme is switched off even though a key is present: no path requires validation.
    /// </summary>
    public static ApiKeyAuthenticationOptions Disabled() => new() { Enabled = false, ValidationKey = ValidationKey };

    /// <summary>
    /// The scheme is switched on but the key is blank, so it is not configured and no path requires validation.
    /// </summary>
    public static ApiKeyAuthenticationOptions EnabledWithBlankKey() => new() { Enabled = true, ValidationKey = "" };
}
