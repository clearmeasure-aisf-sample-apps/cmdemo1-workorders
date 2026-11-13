using ClearMeasure.Bootcamp.AcceptanceTests.Builders;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>Opens a Playwright browser context for one acceptance test.</summary>
public static class BrowserContextFactory
{
    /// <summary>Creates a context optionally pre-authenticated via storage state.</summary>
    public static async Task<IBrowserContext> OpenAsync(
        IBrowser browser,
        string applicationBaseUrl,
        string? storageStatePath)
    {
        var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = applicationBaseUrl,
            Locale = "en-US",
            IgnoreHTTPSErrors = true,
            Permissions = ["clipboard-read", "clipboard-write"],
            ViewportSize = new ViewportSize { Width = 800, Height = 600 },
            StorageStatePath = storageStatePath
        });
        browserContext.SetDefaultTimeout(60_000);
        await StartTracingAsync(browserContext);
        return browserContext;
    }

    /// <summary>Writes a temp storage-state file for <paramref name="userName"/> when sign-in is requested.</summary>
    public static async Task<string?> WriteSignInStateAsync(
        bool useStorageStateSignIn,
        string userName,
        string applicationBaseUrl)
    {
        if (!useStorageStateSignIn)
        {
            return null;
        }

        return await PlaywrightSignInState.WriteTempFileAsync(userName, applicationBaseUrl);
    }

    private static Task StartTracingAsync(IBrowserContext browserContext) =>
        browserContext.Tracing.StartAsync(new TracingStartOptions
        {
            Title = $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}",
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
}
