using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Launches a worker-scoped browser, creates per-test data via test-support, and opens a fresh context.
/// </summary>
public static class BrowserFixtureSetup
{
    private static readonly Random RandomPosition = new();

    /// <summary>Creates isolated browser state for one acceptance test.</summary>
    public static async Task<TestState> CreateAsync(BrowserSetupRequest request)
    {
        var testTag = Guid.NewGuid().ToString("N")[..8];
        var currentUser = await CreateEmployeeAsync(testTag);
        var browser = await LaunchWorkerBrowserAsync(request.Playwright, request.Headless, request.SlowMo);
        var storageStatePath = await BrowserContextFactory.WriteSignInStateAsync(
            request.UseStorageStateSignIn,
            currentUser.UserName,
            request.ApplicationBaseUrl);
        var browserContext = await BrowserContextFactory.OpenAsync(
            browser,
            request.ApplicationBaseUrl,
            storageStatePath);
        TryDelete(storageStatePath);
        var page = await browserContext.NewPageAsync();
        await NavigateReadyAsync(page, request.UseStorageStateSignIn);

        return new TestState
        {
            Page = page,
            BrowserContext = browserContext,
            Browser = browser,
            CurrentUser = currentUser,
            TestTag = testTag
        };
    }

    private static async Task<Employee> CreateEmployeeAsync(string testTag)
    {
        var created = await AcceptanceTestSupportSettings.CreateClient().CreateEmployeeAsync(testTag);
        var employee = new Employee(created.UserName, created.FirstName, created.LastName, created.EmailAddress)
        {
            Id = created.Id
        };
        employee.AddRole(new Role("admin", true, true));
        return employee;
    }

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
            ? nameof(Logout.Elements.WelcomeText)
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
