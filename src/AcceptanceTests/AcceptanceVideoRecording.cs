namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Playwright WebM recording for PR acceptance artifacts (#1671).
/// Enabled when <c>ACCEPTANCE_RECORD_VIDEO</c> is truthy; off by default.
/// </summary>
internal static class AcceptanceVideoRecording
{
    /// <summary>
    /// When enabled, sets <see cref="BrowserNewContextOptions.RecordVideoDir"/> under
    /// <c>playwright-videos/&lt;Class&gt;.&lt;Method&gt;/</c> for the current NUnit test.
    /// </summary>
    internal static void ApplyTo(BrowserNewContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!IsEnabled())
        {
            return;
        }

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

    private static bool IsEnabled()
    {
        var value = Environment.GetEnvironmentVariable("ACCEPTANCE_RECORD_VIDEO");
        return value is not null &&
               (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }
}
