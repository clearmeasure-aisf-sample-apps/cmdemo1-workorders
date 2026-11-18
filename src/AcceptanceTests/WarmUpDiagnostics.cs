// ReSharper disable MethodHasAsyncOverload -- Qodana C6 (#9039): Playwright/NUnit sync APIs are the
// idiomatic call shape here; qodana.yaml path-scoped exclude is documentation-only (qodana-cdnet
// ignores it, see #8986/C2), so enforcement is this file-level directive.
namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Owns the evidence (console, errors, failed requests, HTTP errors) from one warm-up page.
/// Subscribes to page events in the constructor; captures diagnostics to disk on demand.
/// </summary>
public sealed class WarmUpDiagnostics
{
    private readonly IPage _page;
    private readonly List<string> _consoleEntries = new();
    private readonly List<string> _failedRequests = new();
    private readonly List<(int Status, string Url)> _httpErrors = new();
    private readonly List<string> _jsErrors = new();

    public WarmUpDiagnostics(IPage page)
    {
        _page = page;
        _page.Console += (_, entry) => _consoleEntries.Add($"[{entry.Type}] {entry.Text}");
        _page.RequestFailed += (_, request) => _failedRequests.Add(request.Url);
        _page.PageError += (_, error) => _jsErrors.Add(error);
        _page.Response += (_, response) =>
        {
            var status = (int)response.Status;
            if (status >= 400)
            {
                _httpErrors.Add((status, response.Url));
            }
        };
    }

    /// <summary>
    /// JavaScript errors captured during the warm-up page load.
    /// </summary>
    public IReadOnlyList<string> JsErrors => _jsErrors;

    /// <summary>
    /// Clears all captured evidence (used between reloads).
    /// </summary>
    public void Clear()
    {
        _consoleEntries.Clear();
        _failedRequests.Clear();
        _httpErrors.Clear();
        _jsErrors.Clear();
    }

    /// <summary>
    /// Writes screenshot, page HTML, and consolidated log to disk under warmup-diagnostics/{label}.
    /// </summary>
    public async Task SaveAsync(string label)
    {
        var diagsDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "warmup-diagnostics");
        Directory.CreateDirectory(diagsDir);

        var screenshotPath = Path.Combine(diagsDir, $"{label}.png");
        var htmlPath = Path.Combine(diagsDir, $"{label}.html");
        var logPath = Path.Combine(diagsDir, $"{label}.log");

        try
        {
            await _page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
            await WriteHtmlAsync(htmlPath);
            await WriteLogAsync(logPath);
            Attach(screenshotPath);
            Attach(htmlPath);
            Attach(logPath);
        }
        catch (Exception ex)
        {
            await TestContext.Out.WriteLineAsync($"Diagnostics save failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes page HTML content to the given path.
    /// </summary>
    private async Task WriteHtmlAsync(string path)
    {
        var content = await _page.ContentAsync();
        await File.WriteAllTextAsync(path, content);
    }

    /// <summary>
    /// Writes the consolidated console, request, HTTP, and JS error log.
    /// </summary>
    private async Task WriteLogAsync(string path)
    {
        using var log = File.CreateText(path);
        await log.WriteLineAsync($"URL: {_page.Url}");
        await WriteCollectionAsync(log, $"Console entries ({_consoleEntries.Count}):", _consoleEntries);
        await WriteCollectionAsync(log, $"Failed requests ({_failedRequests.Count}):", _failedRequests);
        await WriteCollectionAsync(log, $"HTTP errors ({_httpErrors.Count}):", _httpErrors.Select(x => $"{x.Status} {x.Url}"));
        await WriteCollectionAsync(log, $"JS errors ({_jsErrors.Count}):", _jsErrors);
        await log.FlushAsync();
    }

    /// <summary>
    /// Writes each item in a collection with a leading space.
    /// </summary>
    private static async Task WriteCollectionAsync(
        StreamWriter log,
        string header,
        IEnumerable<string> items)
    {
        await log.WriteLineAsync(header);
        foreach (var item in items)
        {
            await log.WriteLineAsync($"  {item}");
        }
    }

    /// <summary>
    /// Attaches a file to the current test context; swallows failures.
    /// </summary>
    private void Attach(string path)
    {
        TestContext.Out.WriteLine($"Saved diagnostics: {path}");
        try
        {
            TestContext.AddTestAttachment(path);
        }
        catch
        {
            // Attachment may fail in some runners; non-critical.
        }
    }

}
