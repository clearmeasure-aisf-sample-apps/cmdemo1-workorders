using System.Diagnostics;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// The tdd SQL dependency budget is a script and a KQL query. The self-test is the
/// deterministic check; this fixture runs it so the private build fails when the query,
/// threshold, credential skip, or scheduled-query-rule drift comparison regresses.
/// </summary>
[TestFixture]
public class SqlDependencyBudgetAlertTests
{
    [Test]
    public async Task Script_WhenSelfTestRuns_ExitsZero()
    {
        string repoRoot = FindRepoRoot();
        string script = Path.Combine(repoRoot, "scripts", "appinsights", "Invoke-SqlDependencyBudget.ps1");
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
        startInfo.ArgumentList.Add("-SelfTest");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start pwsh.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(CancellationToken.None);
        string output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.ShouldBe(0, output);
        output.ShouldContain("SQL dependency budget self-test passed.");
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
}
