using ClearMeasure.Bootcamp.UnitTests.BuildGates;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates;

/// <summary>
/// The <c>dotnet-script</c> global tool the CRAP scripts run under: the installed copy in
/// <c>~/.dotnet/tools</c>, else one on <c>PATH</c>, else the pinned version installed on first use.
/// </summary>
public static class DotnetScriptTool
{
    private const string PinnedVersion = "2.0.0";

    private static string FileName => OperatingSystem.IsWindows() ? "dotnet-script.exe" : "dotnet-script";

    private static string GlobalToolPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools", FileName);

    /// <summary>A command that runs <paramref name="script"/> with <paramref name="arguments"/>.</summary>
    public static async Task<ExternalCommand> RunningAsync(string script, params string[] arguments)
    {
        var tool = Locate() ?? await InstallAsync();
        return new ExternalCommand(tool, [script, "--", .. arguments], Path.GetTempPath());
    }

    private static string? Locate()
    {
        return File.Exists(GlobalToolPath) ? GlobalToolPath : OnPath();
    }

    private static string? OnPath()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory.Trim(), FileName))
            .FirstOrDefault(File.Exists);
    }

    private static async Task<string> InstallAsync()
    {
        var install = await ExternalCommand
            .Dotnet("tool", "install", "-g", "dotnet-script", "--version", PinnedVersion)
            .RunAsync();
        return Locate() ?? throw new InvalidOperationException($"dotnet-script not found after install. {install.Output}");
    }
}
