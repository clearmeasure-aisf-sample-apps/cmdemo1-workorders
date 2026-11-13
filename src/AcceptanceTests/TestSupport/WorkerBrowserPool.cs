using System.Collections.Concurrent;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

/// <summary>
/// One Chromium browser per NUnit worker. Each test opens a fresh context on the worker's browser.
/// </summary>
public static class WorkerBrowserPool
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<IBrowser>>> Browsers = new();

    /// <summary>Returns the shared browser for the current NUnit worker, launching it on first use.</summary>
    public static Task<IBrowser> GetBrowserAsync(IPlaywright playwright, BrowserTypeLaunchOptions launchOptions)
    {
        var workerId = TestContext.CurrentContext.WorkerId ?? Environment.CurrentManagedThreadId.ToString();
        var lazy = Browsers.GetOrAdd(
            workerId,
            _ => new Lazy<Task<IBrowser>>(
                () => playwright.Chromium.LaunchAsync(launchOptions),
                LazyThreadSafetyMode.ExecutionAndPublication));
        return lazy.Value;
    }

    /// <summary>Closes every worker browser. Call from the acceptance <c>OneTimeTearDown</c>.</summary>
    public static async Task DisposeAllAsync()
    {
        foreach (var pair in Browsers)
        {
            if (!pair.Value.IsValueCreated)
            {
                continue;
            }

            try
            {
                var browser = await pair.Value.Value;
                await browser.CloseAsync();
            }
            catch (Exception)
            {
                // Browser already tearing down with the process; ignore close races.
            }
        }

        Browsers.Clear();
    }
}
