using System.IO.Compression;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;
using static ClearMeasure.Bootcamp.IntegrationTests.BuildGates.BuildFactsInputsMother;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates;

/// <summary>
/// Runs <c>scripts/Write-BuildFacts.ps1</c> against a small fabricated repository and fabricated Build artifacts
/// (the layout <c>.github/workflows/build.yml</c> uploads), and reads the file it writes.
/// </summary>
[TestFixture]
public class BuildFactsScriptTests
{
    private ScratchTree _work = null!;
    private string _repository = null!;
    private string _artifacts = null!;
    private BuildFactsRun _run = null!;

    [OneTimeSetUp]
    public async Task WriteTheFactsOfAFabricatedBuild()
    {
        _work = new ScratchTree("build-facts");
        _repository = await WriteRepositoryAsync(_work, "repository");
        _artifacts = WriteEveryArtifact(_work, "artifacts");
        _run = await RunScriptAsync(_artifacts);
    }

    [OneTimeTearDown]
    public void DeleteWorkDirectory()
    {
        _work.Dispose();
    }

    [Test]
    public void WriteBuildFacts_WhenEveryArtifactIsThere_WritesTheIdentityOfTheBuild()
    {
        var root = _run.Facts;
        root.EnumerateObject().Select(property => property.Name).ShouldBe(
        [
            "version", "commit", "commitUrl", "builtAt", "buildUrl", "code", "tests", "coverage", "complexity",
            "crap", "analysis"
        ]);
        root.GetProperty("version").GetString().ShouldBe(AppVersion);
        root.GetProperty("commit").GetString().ShouldBe(Commit);
        root.GetProperty("commitUrl").GetString().ShouldBe($"https://github.com/example-org/example-repo/commit/{Commit}");
        root.GetProperty("builtAt").GetString().ShouldBe("2026-10-06T05:00:00Z");
        root.GetProperty("buildUrl").GetString().ShouldBe("https://github.com/example-org/example-repo/actions/runs/424242");
    }

    [Test]
    public void WriteBuildFacts_WhenEveryArtifactIsThere_CountsTrackedSourceLinesPerLanguage()
    {
        var code = _run.Facts.GetProperty("code");
        code.GetProperty("linesOfCode").GetInt32().ShouldBe(13);
        code.GetProperty("files").GetInt32().ShouldBe(5);
        code.GetProperty("languages").EnumerateArray()
            .Select(language => (
                language.GetProperty("name").GetString(),
                language.GetProperty("lines").GetInt32(),
                language.GetProperty("files").GetInt32()))
            .ShouldBe(
            [
                ("C#", 7, 2),
                ("Markdown", 2, 1),
                ("Razor", 2, 1),
                ("SQL", 2, 1)
            ]);
    }

    [Test]
    public void WriteBuildFacts_WhenEveryArtifactIsThere_CountsEachKindOfTestOnce()
    {
        var tests = _run.Facts.GetProperty("tests");
        tests.GetProperty("unit").GetInt32().ShouldBe(4);
        tests.GetProperty("integration").GetInt32().ShouldBe(3);
        tests.GetProperty("acceptance").GetInt32().ShouldBe(2);
    }

    [Test]
    public void WriteBuildFacts_WhenEveryArtifactIsThere_MergesCoverageAndReadsComplexity()
    {
        var coverage = _run.Facts.GetProperty("coverage");
        coverage.GetProperty("linePercent").GetDouble().ShouldBe(75.0);
        coverage.GetProperty("branchPercent").GetDouble().ShouldBe(66.7);
        var complexity = _run.Facts.GetProperty("complexity");
        complexity.GetProperty("average").GetDouble().ShouldBe(4.0);
        complexity.GetProperty("max").GetInt32().ShouldBe(8);
        complexity.GetProperty("methods").GetInt32().ShouldBe(3);
    }

    [Test]
    public void WriteBuildFacts_WhenEveryArtifactIsThere_ReadsCrapOfProductionCodeAndQodanaProblems()
    {
        var crap = _run.Facts.GetProperty("crap");
        crap.GetProperty("max").GetDouble().ShouldBe(5.8);
        crap.GetProperty("threshold").GetInt32().ShouldBe(6);
        crap.GetProperty("overThreshold").GetInt32().ShouldBe(0);
        _run.Facts.GetProperty("analysis").GetProperty("qodanaProblems").GetInt32().ShouldBe(3);
    }

    [Test]
    public async Task WriteBuildFacts_WhenNoArtifactIsThere_WritesNullSectionsAndSucceeds()
    {
        var empty = _work.PathOf("no-artifacts");
        Directory.CreateDirectory(empty);

        var run = await RunScriptAsync(empty);

        var root = run.Facts;
        root.GetProperty("version").GetString().ShouldBe(AppVersion);
        root.GetProperty("code").GetProperty("files").GetInt32().ShouldBe(5);
        foreach (var section in new[] { "tests", "coverage", "complexity", "crap", "analysis" })
        {
            root.GetProperty(section).ValueKind.ShouldBe(JsonValueKind.Null, section);
        }
    }

    [Test]
    public async Task WriteBuildFacts_WhenArtifactsAreUnreadable_WritesNullForThoseSectionsOnly()
    {
        var broken = WriteEveryArtifact(_work, "broken-artifacts");
        _work.Write($"broken-artifacts/{QodanaSarif}", "{ \"runs\": [")
            .Write($"broken-artifacts/{UnitCoverage}", "<coverage><packages>");

        var run = await RunScriptAsync(broken);

        var root = run.Facts;
        root.GetProperty("analysis").ValueKind.ShouldBe(JsonValueKind.Null);
        root.GetProperty("coverage").ValueKind.ShouldBe(JsonValueKind.Null);
        root.GetProperty("complexity").ValueKind.ShouldBe(JsonValueKind.Null);
        root.GetProperty("tests").GetProperty("unit").GetInt32().ShouldBe(4);
        root.GetProperty("crap").GetProperty("threshold").GetInt32().ShouldBe(6);
        run.Log.ShouldContain("::warning title=Build facts::The section 'analysis' could not be read");
        run.Log.ShouldContain("::warning title=Build facts::The section 'coverage reports' could not be read");
    }

    [Test]
    public async Task WriteBuildFacts_WhenTheQodanaReportIsAZipInTheArtifact_ReadsTheSarifInsideIt()
    {
        // What the Qodana action uploads: one qodana-report.zip with its results directory.
        var artifacts = ArtifactsWithZippedQodanaReport("zipped-qodana-artifacts", archive =>
        {
            WriteEntry(archive, "log/idea.log", "started");
            WriteEntry(archive, "qodana-short.sarif.json", Sarif("new"));
            WriteEntry(archive, "report/index.html", "<html></html>");
            WriteEntry(archive, "report/results/qodana.sarif.json", Sarif("new", "new"));
            WriteEntry(archive, "qodana.sarif.json", Sarif("unchanged", "new", "absent", "unchanged", "unchanged", "new"));
        });

        var run = await RunScriptAsync(artifacts);

        run.Facts.GetProperty("analysis").GetProperty("qodanaProblems").GetInt32().ShouldBe(5);
        run.Log.ShouldContain("PASS analysis");
        Directory.GetFileSystemEntries(Path.Combine(artifacts, "qodana-report")).Select(Path.GetFileName)
            .ShouldBe(["qodana-report.zip"]);
    }

    [Test]
    public async Task WriteBuildFacts_WhenTheZipInTheArtifactHasNoSarif_WritesNullForAnalysis()
    {
        var artifacts = ArtifactsWithZippedQodanaReport(
            "zip-without-sarif-artifacts",
            archive => WriteEntry(archive, "qodana-short.sarif.json", Sarif("new")));

        var run = await RunScriptAsync(artifacts);

        run.Facts.GetProperty("analysis").ValueKind.ShouldBe(JsonValueKind.Null);
        run.Facts.GetProperty("tests").GetProperty("unit").GetInt32().ShouldBe(4);
        run.Log.ShouldContain("SKIP analysis: no input");
    }

    [Test]
    public async Task WriteBuildFacts_WhenTheZipInTheArtifactIsUnreadable_WritesNullForAnalysisAndWarns()
    {
        var artifacts = WriteEveryArtifact(_work, "broken-zip-artifacts");
        File.Delete(Path.Combine(artifacts, QodanaSarif));
        _work.Write("broken-zip-artifacts/qodana-report/qodana-report.zip", "not a zip");

        var run = await RunScriptAsync(artifacts);

        run.Facts.GetProperty("analysis").ValueKind.ShouldBe(JsonValueKind.Null);
        run.Facts.GetProperty("crap").GetProperty("threshold").GetInt32().ShouldBe(6);
        run.Log.ShouldContain("::warning title=Build facts::The section 'analysis' could not be read");
    }

    [Test]
    public async Task WriteBuildFacts_WhenGivenAPackage_PutsTheFileInItsRoot()
    {
        var package = PackageWithAStaleFactsFile();

        var run = await RunScriptAsync(_artifacts, "-Package", package);

        ShouldHoldTheAppAndFreshFacts(package);
        run.Facts.GetProperty("version").GetString().ShouldBe(AppVersion);
    }

    [Test]
    public async Task WriteBuildFacts_WhenThePackageIsMissing_Fails()
    {
        var output = _work.PathOf($"facts-{Guid.NewGuid():N}.json");

        var result = await WriteBuildFactsAsync(
            "-OutputPath", output, "-RepoRoot", _repository, "-Commit", Commit, "-Package", _work.PathOf("missing.zip"));

        result.ExitCode.ShouldNotBe(0, result.Output);
        result.Output.ShouldContain("Package not found");
    }

    private async Task<BuildFactsRun> RunScriptAsync(string artifacts, params string[] more)
    {
        var output = _work.PathOf($"facts-{Guid.NewGuid():N}.json");

        var result = await WriteBuildFactsAsync(
        [
            "-OutputPath", output, "-RepoRoot", _repository, "-Version", AppVersion, "-Commit", Commit,
            "-Repository", "example-org/example-repo", "-RunId", "424242", "-BuiltAt", "2026-10-06T05:00:00Z",
            "-ArtifactsPath", artifacts, .. more
        ]);

        result.ExitCode.ShouldBe(0, result.Output);
        using var facts = JsonDocument.Parse(await File.ReadAllTextAsync(output));
        return new BuildFactsRun(facts.RootElement.Clone(), result.Output);
    }

    private static Task<CommandOutcome> WriteBuildFactsAsync(params string[] arguments)
    {
        var script = RepositoryCheckout.Current.PathOf("scripts/Write-BuildFacts.ps1");
        return ExternalCommand.Pwsh(script, arguments).RunAsync();
    }

    private string ArtifactsWithZippedQodanaReport(string folder, Action<ZipArchive> fill)
    {
        var artifacts = WriteEveryArtifact(_work, folder);
        File.Delete(Path.Combine(artifacts, QodanaSarif));
        using var archive = ZipFile.Open(Path.Combine(artifacts, "qodana-report", "qodana-report.zip"), ZipArchiveMode.Create);
        fill(archive);
        return artifacts;
    }

    private string PackageWithAStaleFactsFile()
    {
        var package = _work.PathOf($"example-ui.{AppVersion}.zip");
        using var archive = ZipFile.Open(package, ZipArchiveMode.Create);
        WriteEntry(archive, "ClearMeasure.Bootcamp.UI.Server.dll", "the app");
        WriteEntry(archive, "wwwroot/index.html", "<html></html>");
        WriteEntry(archive, "build-facts.json", "{ \"version\": \"stale\" }");
        return package;
    }

    private static void ShouldHoldTheAppAndFreshFacts(string package)
    {
        using var stamped = ZipFile.OpenRead(package);
        stamped.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal).ShouldBe(
        [
            "ClearMeasure.Bootcamp.UI.Server.dll", "build-facts.json", "wwwroot/index.html"
        ]);
        using var reader = new StreamReader(stamped.GetEntry("build-facts.json")!.Open());
        using var inPackage = JsonDocument.Parse(reader.ReadToEnd());
        inPackage.RootElement.GetProperty("version").GetString().ShouldBe(AppVersion);
        inPackage.RootElement.GetProperty("tests").GetProperty("unit").GetInt32().ShouldBe(4);
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open());
        writer.Write(content);
    }

    private sealed record BuildFactsRun(JsonElement Facts, string Log);
}
