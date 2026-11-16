// ReSharper disable MethodHasAsyncOverload -- Qodana C6 (#9039): Playwright/NUnit sync APIs are the
// idiomatic call shape here; qodana.yaml path-scoped exclude is documentation-only (qodana-cdnet
// ignores it, see #8986/C2), so enforcement is this file-level directive.
using System.Collections.Concurrent;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Warms up the Blazor WebAssembly application by loading the home page in a headless browser,
/// detecting JavaScript errors or stuck loading screens, and reloading until the LoginLink is visible.
/// This ensures the WASM payload is fully downloaded and initialized before parallel tests begin.
/// </summary>
public class BlazorWasmWarmUp
{
    private const int MaxAttempts = 5;
    private const int TimeoutSeconds = 30;

    private readonly IPlaywright _playwright;
    private readonly string _baseUrl;

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public BlazorWasmWarmUp(IPlaywright playwright, string baseUrl)
    {
        _playwright = playwright;
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Loads the home page in a disposable headless browser, retrying until the LoginLink element is visible.
    /// When it never shows, fails with what the browser saw (page and console errors, failed and 4xx/5xx
    /// requests) and the server's recent output, so a CI run says why the client did not start.
    /// </summary>
    public async Task ExecuteAsync()
    {
        TestContext.Out.WriteLine("Blazor WASM warm-up: starting...");
        await using var browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync(ContextOptions());
        context.SetDefaultTimeout(TimeoutSeconds * 1000);
        var page = await context.NewPageAsync();
        var problems = WatchForProblems(page);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            problems.Clear();
            TestContext.Out.WriteLine($"Blazor WASM warm-up: attempt {attempt}/{MaxAttempts}");
            if (await LoginLinkAppears(page))
            {
                TestContext.Out.WriteLine("Blazor WASM warm-up: LoginLink visible — app is ready.");
                await context.CloseAsync();
                return;
            }

            TestContext.Out.WriteLine($"Blazor WASM warm-up: LoginLink not visible after {TimeoutSeconds}s: {string.Join(" | ", problems)}");
        }

        await context.CloseAsync();
        throw new TimeoutException(
            $"Blazor WASM warm-up: LoginLink not visible after {MaxAttempts} attempts of {TimeoutSeconds}s. " +
            $"Last attempt saw: {string.Join(" | ", problems)}. Server output: {ServerFixture.RecentServerOutput()}");
    }

    // Locale as BrowserContextFactory sets it. Without one, Chromium on the ARM runner (LANG=C) reports
    // "en-US@posix", which the .NET 11 WebAssembly runtime rejects at start-up (CultureNotSupportedException).
    private BrowserNewContextOptions ContextOptions() =>
        new() { BaseURL = _baseUrl, Locale = "en-US", IgnoreHTTPSErrors = true };

    private static ConcurrentQueue<string> WatchForProblems(IPage page)
    {
        var problems = new ConcurrentQueue<string>();
        page.PageError += (_, error) => problems.Enqueue($"page error: {error}");
        page.Console += (_, message) =>
        {
            if (message.Type == "error") problems.Enqueue($"console: {message.Text}");
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400) problems.Enqueue($"HTTP {response.Status} {response.Url}");
        };
        page.RequestFailed += (_, request) => problems.Enqueue($"request failed: {request.Failure} {request.Url}");
        return problems;
    }

    private static async Task<bool> LoginLinkAppears(IPage page)
    {
        await page.GotoAsync("/");
        try
        {
            await page.GetByTestId("LoginLink").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = TimeoutSeconds * 1000
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
