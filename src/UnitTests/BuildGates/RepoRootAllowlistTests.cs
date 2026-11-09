using System.Diagnostics;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// The repo-root allowlist is a script. This fixture runs its self-test so the
/// private build fails when a root entry is outside the list or a stray artifact appears.
/// </summary>
[TestFixture]
public class RepoRootAllowlistTests
{
    [Test]
    public async Task Script_WhenSelfTestRuns_ExitsZero()
    {
        var repoRoot = FindRepoRoot();
        var script = Path.Combine(repoRoot, "scripts", "repo-root", "Test-RepoRoot.ps1");
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
        var output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.ShouldBe(0, output);
        output.ShouldContain("Repo root allowlist self-test passed.");
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
