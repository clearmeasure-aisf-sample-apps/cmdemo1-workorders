using System.Diagnostics;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates.RefactoringRationale;

/// <summary>
/// Steps of a refactoring pull request in a throwaway git repository: a base commit with an oversized file, the
/// changes of the pull request, its description, and a run of the rationale script over them.
/// </summary>
public sealed class RefactoringPullRequestSteps : IDisposable
{
    public const string OversizedFile = "src/McpServer/Tools/WorkOrderTools.cs";
    public const string ListedFile = "src/Core/WorkOrderRules.cs";
    public const string SizeBaseline = "src/UnitTests/BuildGates/size-baseline.json";
    private const string GuardrailBaseline = "src/UnitTests/BuildGates/Refactoring/Baselines/static-helper-classes.txt";
    private const string BaseTag = "pull-request-base";

    private readonly string _repositoryRoot;
    private readonly string _workDirectory;
    private readonly string _bodyPath;

    public RefactoringPullRequestSteps(string repositoryRoot)
    {
        _repositoryRoot = repositoryRoot;
        _workDirectory = Path.Combine(Path.GetTempPath(), $"refactoring-rationale-{Guid.NewGuid():N}");
        _bodyPath = Path.Combine(_workDirectory, "pr-body.md");
        Directory.CreateDirectory(_workDirectory);
        Git("init", "--quiet");
        Git("config", "user.email", "steps@example.com");
        Git("config", "user.name", "Refactoring steps");
        Git("config", "core.autocrlf", "false");
        File.WriteAllText(_bodyPath, "");
    }

    public void CommitBaseWithOversizedFile()
    {
        var methods = Enumerable.Range(1, 310).Select(n => $"    public int Value{n}() => {n};\n");
        Write(OversizedFile, $"namespace Tools;\n\npublic class WorkOrderTools\n{{\n{string.Concat(methods)}}}\n");
        WriteBaseCommonFiles();
        CommitBase();
    }

    public void CommitBaseWithSizeBaselineListing()
    {
        Write(ListedFile, "namespace Core;\n\npublic class WorkOrderRules\n{\n}\n");
        Write(SizeBaseline, $$"""{ "violations": [ { "path": "{{ListedFile}}", "rule": "MethodLength" } ] }""");
        WriteBaseCommonFiles();
        CommitBase();
    }

    public void ExtractType(string typeName)
    {
        Write(OversizedFile, File.ReadAllText(PathOf(OversizedFile)).Replace("Value1()", $"Value1() => new {typeName}().Count; public int Old1()"));
        Write($"src/McpServer/Tools/{typeName}.cs", $"namespace Tools;\n\npublic sealed record {typeName}\n{{\n    public int Count => 1;\n}}\n");
    }

    public void ChangeListedFile() =>
        Write(ListedFile, "namespace Core;\n\npublic class WorkOrderRules\n{\n    public bool IsOpen => true;\n}\n");

    public void ChangeSmallFile() => Write("src/Core/Small.cs", "namespace Core;\n\npublic class Small;\n");

    public void AddFixtureFor(string fixtureName) =>
        Write($"src/UnitTests/Tools/{fixtureName}.cs", $"namespace UnitTests.Tools;\n\npublic class {fixtureName}\n{{\n}}\n");

    public void AddGuardrailBaselineLine() =>
        File.AppendAllText(PathOf(GuardrailBaseline), "src/Core/SearchUtils.cs :: SearchUtils\n");

    public void DescribeWithRationale(string row) =>
        File.WriteAllText(
            _bodyPath,
            $"""
             ## Summary

             Refactors the work-order tools.

             ## Refactoring rationale

             <!-- One row per move. -->
             | Smell | Refactoring | New abstraction | Why |
             |-------|-------------|-----------------|-----|
             {row}

             ## Testing

             Unit tests.
             """);

    public void DescribeWithoutRationale() => File.WriteAllText(_bodyPath, "## Summary\n\nRefactors the work-order tools.\n");

    public ScriptRun RunRationaleCheck(string oversizedBaselinePath = "")
    {
        Git("add", "--all");
        Git("commit", "--quiet", "--allow-empty", "-m", "pull request");
        var script = Path.Combine(_repositoryRoot, "scripts", "refactoring", "Test-RefactoringRationale.ps1");
        string[] arguments =
        [
            "-NoProfile", "-File", script, "-BaseRef", BaseTag, "-PrBodyPath", _bodyPath, "-RepoRoot", _workDirectory,
            "-OversizedBaselinePath", oversizedBaselinePath
        ];
        return Run("pwsh", arguments);
    }

    public void Dispose()
    {
        if (!Directory.Exists(_workDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(_workDirectory, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(_workDirectory, true);
    }

    private void WriteBaseCommonFiles()
    {
        Write("docs/refactoring-oversized-code.md", File.ReadAllText(Path.Combine(_repositoryRoot, "docs", "refactoring-oversized-code.md")));
        Write(GuardrailBaseline, "# baseline\nsrc/Core/WorkOrderHelper.cs :: WorkOrderHelper\n");
    }

    private void CommitBase()
    {
        Git("add", "--all");
        Git("commit", "--quiet", "-m", "base");
        Git("tag", BaseTag);
    }

    private void Write(string relativePath, string content)
    {
        var path = PathOf(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private string PathOf(string relativePath) =>
        Path.Combine(_workDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private void Git(params string[] arguments)
    {
        var run = Run("git", arguments);
        if (run.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {run.Output}");
        }
    }

    private ScriptRun Run(string fileName, string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = _workDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(120_000))
        {
            process.Kill(true);
            throw new TimeoutException($"{fileName} did not finish in two minutes.");
        }

        return new ScriptRun(process.ExitCode, standardOutput.Result + standardError.Result);
    }
}
