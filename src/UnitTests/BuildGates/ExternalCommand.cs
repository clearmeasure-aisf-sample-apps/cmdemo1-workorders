using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// A tool a build gate runs as a separate process, such as a PowerShell gate script or <c>dotnet build</c>.
/// </summary>
/// <param name="FileName">Executable name or path.</param>
/// <param name="Arguments">Arguments, passed without shell quoting.</param>
/// <param name="WorkingDirectory">Directory the process starts in.</param>
public sealed record ExternalCommand(string FileName, IReadOnlyList<string> Arguments, string WorkingDirectory)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    /// <summary><c>pwsh -NoProfile -File</c> <paramref name="script"/>, started in the temp directory.</summary>
    public static ExternalCommand Pwsh(string script, params string[] arguments)
    {
        return new ExternalCommand("pwsh", ["-NoProfile", "-File", script, .. arguments], Path.GetTempPath());
    }

    /// <summary><c>dotnet</c> with <paramref name="arguments"/>, started in the temp directory.</summary>
    public static ExternalCommand Dotnet(params string[] arguments)
    {
        return new ExternalCommand("dotnet", arguments, Path.GetTempPath());
    }

    /// <summary>The same command started in <paramref name="directory"/>.</summary>
    public ExternalCommand In(string directory)
    {
        return this with { WorkingDirectory = directory };
    }

    /// <summary>
    /// Runs to completion and returns the exit code with standard output followed by standard error.
    /// A process still running after five minutes is killed and fails the gate.
    /// </summary>
    public async Task<CommandOutcome> RunAsync()
    {
        using var process = Process.Start(StartInfo())
            ?? throw new InvalidOperationException($"{FileName} did not start.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(Timeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            var partial = await CombineAsync(standardOutput, standardError);
            throw new TimeoutException(
                $"{FileName} {string.Join(' ', Arguments)} ran longer than {Timeout}.{Environment.NewLine}{partial}");
        }

        return new CommandOutcome(process.ExitCode, await CombineAsync(standardOutput, standardError));
    }

    private static async Task<string> CombineAsync(Task<string> standardOutput, Task<string> standardError)
    {
        return $"{await standardOutput}{Environment.NewLine}{await standardError}";
    }

    private ProcessStartInfo StartInfo()
    {
        var startInfo = new ProcessStartInfo(FileName)
        {
            WorkingDirectory = WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
