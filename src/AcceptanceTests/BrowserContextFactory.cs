using ClearMeasure.Bootcamp.AcceptanceTests.Builders;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>Opens a Playwright browser context for one acceptance test.</summary>
public static class BrowserContextFactory
{
    /// <summary>
    /// When <c>ACCEPTANCE_RECORD_VIDEO</c> is <c>1</c>/<c>true</c>, records a WebM under
    /// <c>playwright-videos/</c> for sticky PR artifact upload (#1671). Off by default.
    /// </summary>
    public static bool RecordVideoEnabled { get; } =
        IsTruthyEnvironmentVariable("ACCEPTANCE_RECORD_VIDEO");

    /// <summary>Creates a context optionally pre-authenticated via storage state.</summary>
    public static async Task<IBrowserContext> OpenAsync(
        IBrowser browser,
        string applicationBaseUrl,
        string? storageStatePath)
    {
        var options = new BrowserNewContextOptions
        {
            BaseURL = applicationBaseUrl,
            Locale = "en-US",
            IgnoreHTTPSErrors = true,
            Permissions = ["clipboard-read", "clipboard-write"],
            ViewportSize = new ViewportSize { Width = 800, Height = 600 },
            StorageStatePath = storageStatePath
        };
        if (RecordVideoEnabled)
        {
            var testLeaf =
                $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}";
            var videoDir = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "playwright-videos",
                testLeaf);
            Directory.CreateDirectory(videoDir);
            options.RecordVideoDir = videoDir;
            options.RecordVideoSize = new RecordVideoSize { Width = 800, Height = 600 };
        }

        var browserContext = await browser.NewContextAsync(options);
        browserContext.SetDefaultTimeout(60_000);
        await StartTracingAsync(browserContext);
        return browserContext;
    }

    private static bool IsTruthyEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return value is not null &&
               (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("yes", StringComparison.OrdinalIgnoreCase));
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
