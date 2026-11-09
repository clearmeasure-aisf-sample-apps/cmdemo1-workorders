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
        var repoRoot = RepoFiles.Root();
        var script = Path.Combine(repoRoot, "scripts", "repo-root", "Test-RepoRoot.ps1");
        File.Exists(script).ShouldBeTrue(script);
        var output = await RunSelfTest(repoRoot, script);

        output.ShouldContain("Repo root allowlist self-test passed.");
    }

    private static async Task<string> RunSelfTest(string repoRoot, string script)
    {
        var startInfo = new ProcessStartInfo("pwsh", $"-NoProfile -File \"{script}\" -SelfTest")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("pwsh did not start.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(CancellationToken.None);
        var output = stdout + Environment.NewLine + stderr;
        process.ExitCode.ShouldBe(0, output);
        return output;
    }
}
