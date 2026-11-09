using System.Diagnostics;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Runs the suppression-budget script. Every disable in src needs a reason, the count
/// cannot rise above the committed baseline, and task-marker comments fail the build.
/// </summary>
[TestFixture]
public sealed class SuppressionBudgetGateTests
{
    [Test]
    public async Task ShouldPass_WhenSelfTestRuns()
    {
        var result = await RunScript(selfTest: true);

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("missing reason fails");
        result.Output.ShouldContain("increase fails");
        result.Output.ShouldContain("decrease passes");
        result.Output.ShouldContain("suppression budget self-test passed.");
    }

    [Test]
    public async Task ShouldPass_WhenRepositoryStaysWithinSuppressionBudget()
    {
        var result = await RunScript(selfTest: false);

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("suppression budget passed:");
    }

    private static async Task<ScriptResult> RunScript(bool selfTest)
    {
        string repoRoot = FindRepoRoot();
        string script = Path.Combine(repoRoot, "scripts", "quality", "Test-SuppressionBudget.ps1");
        File.Exists(script).ShouldBeTrue(script);

        var startInfo = new ProcessStartInfo("pwsh")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(script);
        if (selfTest)
        {
            startInfo.ArgumentList.Add("-SelfTest");
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start pwsh.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(CancellationToken.None);
        string output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";
        return new ScriptResult(process.ExitCode, output);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "build.ps1")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found from test directory.");
    }

    private sealed record ScriptResult(int ExitCode, string Output);
}
