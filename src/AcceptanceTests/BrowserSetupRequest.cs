namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>Inputs for opening one acceptance-test browser context.</summary>
public sealed class BrowserSetupRequest
{
    /// <summary>Shared Playwright instance from <see cref="ServerFixture"/>.</summary>
    public required IPlaywright Playwright { get; init; }

    /// <summary>Whether Chromium runs headless.</summary>
    public required bool Headless { get; init; }

    /// <summary>Playwright slow-mo milliseconds.</summary>
    public required int SlowMo { get; init; }

    /// <summary>Application base URL under test.</summary>
    public required string ApplicationBaseUrl { get; init; }

    /// <summary>When true, authenticate via Playwright storage state.</summary>
    public required bool UseStorageStateSignIn { get; init; }
}
