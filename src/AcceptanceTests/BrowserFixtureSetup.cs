using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.Acceptance;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Launches a worker-scoped browser, creates per-test data via test-support (or SQL fallback),
/// and opens a fresh context.
/// </summary>
public static class BrowserFixtureSetup
{
    private static readonly Random RandomPosition = new();

    /// <summary>Creates isolated browser state for one acceptance test.</summary>
    public static async Task<TestState> CreateAsync(BrowserSetupRequest request)
    {
        var testTag = Guid.NewGuid().ToString("N")[..8];
        var provision = await CreateEmployeeAsync(testTag);
        // PlaywrightSignInState writes localStorage locally; it does not need the test-support HTTP API.
        // Keep storage-state sign-in on the SQL fallback path (tdd/uat/prod) so LoginAsCurrentUser
        // does not depend on the 1,000-row employee dropdown.
        var useStorageStateSignIn = request.UseStorageStateSignIn;
        var browser = await LaunchWorkerBrowserAsync(request.Playwright, request.Headless, request.SlowMo);
        var storageStatePath = await BrowserContextFactory.WriteSignInStateAsync(
            useStorageStateSignIn,
            provision.Employee.UserName,
            request.ApplicationBaseUrl);
        var browserContext = await BrowserContextFactory.OpenAsync(
            browser,
            request.ApplicationBaseUrl,
            storageStatePath);
        TryDelete(storageStatePath);
        var page = await browserContext.NewPageAsync();
        await NavigateReadyAsync(page, useStorageStateSignIn);

        return new TestState
        {
            Page = page,
            BrowserContext = browserContext,
            CurrentUser = provision.Employee,
            TestTag = testTag
        };
    }

    /// <summary>
    /// Probes test-support, then creates via API or <see cref="TestUsers.AcceptanceTestEmployees.Add"/>.
    /// </summary>
    private static Task<AcceptanceEmployeeProvision> CreateEmployeeAsync(string testTag) =>
        AcceptanceEmployeeProvisionerFactory.CreateDefault().CreateAsync(testTag);

    private static Task<IBrowser> LaunchWorkerBrowserAsync(IPlaywright playwright, bool headless, int slowMo) =>
        WorkerBrowserPool.GetBrowserAsync(playwright, new BrowserTypeLaunchOptions
        {
            Headless = headless,
            SlowMo = slowMo,
            Args =
            [
                $"--window-position={RandomPosition.Next(0, 1200)},{RandomPosition.Next(0, 700)}",
                "--window-size=800,600"
            ]
        });

    private static async Task NavigateReadyAsync(IPage page, bool useStorageStateSignIn)
    {
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await page.GotoAsync("/");
                await page.WaitForURLAsync("/");
                await WaitForShellAsync(page, useStorageStateSignIn);
                return;
            }
            catch (PlaywrightException) when (attempt < maxRetries)
            {
                await Task.Delay(2000);
            }
        }
    }

    private static Task WaitForShellAsync(IPage page, bool useStorageStateSignIn)
    {
        var testId = useStorageStateSignIn
            ? nameof(Logout.Elements.UserMenuToggle)
            : nameof(LoginLink.Elements.LoginLink);
        return page.GetByTestId(testId).WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
    }

    private static void TryDelete(string? path)
    {
        if (path is null)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch
        {
            // Temp sign-in file; ignore cleanup races.
        }
    }
}
