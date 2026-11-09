using System.Diagnostics;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates;

[TestFixture]
public class CrapGateScriptTests
{
    [Test]
    public void AssertCrapGate_WhenNoProductionViolations_ExitsZero()
    {
        int exitCode = RunAssertScript("crap-production-violations-pass.json");

        exitCode.ShouldBe(0);
    }

    [Test]
    public void AssertCrapGate_WhenProductionMethodOverThreshold_ExitsOne()
    {
        int exitCode = RunAssertScript("crap-production-violations-fail.json");

        exitCode.ShouldBe(1);
    }

    [TestCase("crap-production-violations-negative.json")]
    [TestCase("crap-production-violations-malformed.json")]
    [TestCase("crap-production-violations-inconsistent.json")]
    public void AssertCrapGate_WhenViolationCountIsInvalid_ExitsTwo(string fixtureFileName)
    {
        int exitCode = RunAssertScript(fixtureFileName);

        exitCode.ShouldBe(2);
    }

    [Test]
    public void FlattenCobertura_WhenAsyncStateMachine_CopiesHitsOntoOriginalMethod()
    {
        string repoRoot = FindRepoRoot();
        string script = Path.Combine(repoRoot, "scripts", "crap",
            "flatten-cobertura.csx");
        string fixture = Path.Combine(TestContext.CurrentContext.TestDirectory, "BuildGates", "Fixtures",
            "cobertura-async-state-machine.xml");
        string output = Path.Combine(Path.GetTempPath(), $"crap-flat-{Guid.NewGuid():N}.xml");
        File.Exists(script).ShouldBeTrue(script);
        File.Exists(fixture).ShouldBeTrue(fixture);

        string logs = RunDotnetScript(script, output, fixture, out int exitCode);
        exitCode.ShouldBe(0, logs);

        string xml = File.ReadAllText(output);
        xml.ShouldContain("name=\"ExecuteCommandAsync\"");
        xml.ShouldContain("number=\"90\"");
        File.Delete(output);
    }

    [Test]
    public void FlattenCobertura_WhenOnlyStateMachineClassExists_SynthesizesParentMethod()
    {
        string repoRoot = FindRepoRoot();
        string script = Path.Combine(repoRoot, "scripts", "crap",
            "flatten-cobertura.csx");
        string fixture = Path.Combine(TestContext.CurrentContext.TestDirectory, "BuildGates", "Fixtures",
            "cobertura-orphan-async-state-machine.xml");
        string output = Path.Combine(Path.GetTempPath(), $"crap-orphan-{Guid.NewGuid():N}.xml");
        File.Exists(fixture).ShouldBeTrue(fixture);

        string logs = RunDotnetScript(script, output, fixture, out int exitCode);
        exitCode.ShouldBe(0, logs);

        string xml = File.ReadAllText(output);
        xml.ShouldContain("name=\"ClearMeasure.Bootcamp.UI.Server.RequestBodyBufferingPipeline\"");
        xml.ShouldContain("name=\"InvokeAsync\"");
        File.Delete(output);
    }

    private static string RunDotnetScript(string script, string output, string fixture, out int exitCode)
    {
        string tool = EnsureDotnetScript();
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = tool,
            ArgumentList = { script, "--", output, fixture },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        process.ShouldNotBeNull();
        process.WaitForExit(60_000).ShouldBeTrue();
        exitCode = process.ExitCode;
        return process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
    }

    private static string EnsureDotnetScript()
    {
        string fileName = OperatingSystem.IsWindows() ? "dotnet-script.exe" : "dotnet-script";
        string installed = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dotnet", "tools", fileName);
        if (File.Exists(installed))
        {
            return installed;
        }

        string? existing = FindOnPath(fileName);
        if (existing != null)
        {
            return existing;
        }

        using var install = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "tool", "install", "-g", "dotnet-script", "--version", "2.0.0" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        install.ShouldNotBeNull();
        install.WaitForExit(120_000).ShouldBeTrue();
        string logs = install.StandardError.ReadToEnd() + install.StandardOutput.ReadToEnd();
        if (File.Exists(installed))
        {
            return installed;
        }

        string? pathLookup = FindOnPath(fileName);
        pathLookup.ShouldNotBeNull($"dotnet-script not found after install. {logs}");
        return pathLookup;
    }

    private static string? FindOnPath(string fileName)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (string dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(dir.Trim(), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static int RunAssertScript(string fixtureFileName)
    {
        string repoRoot = FindRepoRoot();
        string script = Path.Combine(repoRoot, "scripts", "crap",
            "assert-crap-gate.ps1");
        string fixture = Path.Combine(TestContext.CurrentContext.TestDirectory, "BuildGates", "Fixtures",
            fixtureFileName);
        File.Exists(script).ShouldBeTrue(script);
        File.Exists(fixture).ShouldBeTrue(fixture);

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "pwsh",
            ArgumentList = { "-NoProfile", "-File", script, "-ViolationsPath", fixture },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        process.ShouldNotBeNull();
        process.WaitForExit(30_000).ShouldBeTrue();
        return process.ExitCode;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "ChurchBulletin.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root from test directory.");
    }
}
