namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Route and header names for the pool-slot test-support API.</summary>
public static class TestSupportConstants
{
    /// <summary>Path prefix for all test-support routes.</summary>
    public const string PathPrefix = "/api/test-support";

    /// <summary>Header that must match <see cref="TestSupportOptions.Secret"/> when the feature is on.</summary>
    public const string SecretHeaderName = "X-Test-Support-Secret";

    /// <summary>
    /// On pool slots only: when <c>true</c> with a valid secret, <c>/_healthcheck</c> reports NeedsReboot
    /// for this request without flipping the process-wide static flag.
    /// </summary>
    public const string NeedsRebootHeaderName = "X-Test-Support-NeedsReboot";

    /// <summary>localStorage key written by the Blazor session store and Playwright storage state.</summary>
    public const string UserSessionStorageKey = "bootcamp.userSession.username";
}
