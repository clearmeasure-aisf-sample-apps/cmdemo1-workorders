using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates;

/// <summary>
/// What <c>scripts/Write-BuildFacts.ps1</c> reads, fabricated: a small git repository and the Build run's
/// artifacts in the layout <c>.github/workflows/build.yml</c> uploads them.
/// </summary>
internal static class BuildFactsInputsMother
{
    public const string Commit = "0123456789abcdef0123456789abcdef01234567";

    public const string AppVersion = "2.4.15";

    public const string QodanaSarif = "qodana-report/qodana.sarif.json";

    public const string UnitCoverage = "code-coverage-linux/UnitTests/run/In/host/coverage.cobertura.xml";

    private const string IntegrationCoverage = "code-coverage-linux/IntegrationTests/run/In/host/coverage.cobertura.xml";

    private const string UnitCoverageXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <coverage line-rate="0.5" branch-rate="0.1666" version="1.9" timestamp="1" lines-covered="2" lines-valid="4" branches-covered="1" branches-valid="6">
          <sources><source>/repo/src/</source></sources>
          <packages>
            <package name="App" line-rate="0.5" branch-rate="0.1666" complexity="4">
              <classes>
                <class name="App.Service" filename="App/Service.cs" line-rate="0.5" branch-rate="0.1666" complexity="4">
                  <methods>
                    <method name="Run" signature="()" line-rate="0.5" branch-rate="0.1666" complexity="3">
                      <lines>
                        <line number="1" hits="1" branch="False" />
                        <line number="2" hits="1" branch="True" condition-coverage="50% (1/2)" />
                        <line number="3" hits="0" branch="True" condition-coverage="0% (0/4)" />
                      </lines>
                    </method>
                    <method name="get_Name" signature="()" line-rate="0" branch-rate="1" complexity="1">
                      <lines><line number="4" hits="0" branch="False" /></lines>
                    </method>
                  </methods>
                  <lines>
                    <line number="1" hits="1" branch="False" />
                    <line number="2" hits="1" branch="True" condition-coverage="50% (1/2)" />
                    <line number="3" hits="0" branch="True" condition-coverage="0% (0/4)" />
                    <line number="4" hits="0" branch="False" />
                  </lines>
                </class>
              </classes>
            </package>
          </packages>
        </coverage>
        """;

    private const string IntegrationCoverageXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <coverage line-rate="0.5" branch-rate="0.6666" version="1.9" timestamp="1" lines-covered="2" lines-valid="4" branches-covered="4" branches-valid="6">
          <sources><source>/repo/src/</source></sources>
          <packages>
            <package name="App" line-rate="0.5" branch-rate="0.6666" complexity="12">
              <classes>
                <class name="App.Service" filename="App/Service.cs" line-rate="0.5" branch-rate="0.6666" complexity="4">
                  <methods>
                    <method name="Run" signature="()" line-rate="0.6666" branch-rate="0.6666" complexity="3">
                      <lines />
                    </method>
                    <method name="get_Name" signature="()" line-rate="0" branch-rate="1" complexity="1">
                      <lines />
                    </method>
                  </methods>
                  <lines>
                    <line number="1" hits="0" branch="False" />
                    <line number="2" hits="7" branch="True" condition-coverage="100% (2/2)" />
                    <line number="3" hits="7" branch="True" condition-coverage="50% (2/4)" />
                    <line number="4" hits="0" branch="False" />
                  </lines>
                </class>
                <class name="App.Handler" filename="App/Handler.cs" line-rate="1" branch-rate="1" complexity="8">
                  <methods>
                    <method name="Handle" signature="(System.String)" line-rate="1" branch-rate="1" complexity="8">
                      <lines />
                    </method>
                  </methods>
                  <lines />
                </class>
              </classes>
            </package>
          </packages>
        </coverage>
        """;

    // rollup-file-scores.csx: the wrapper is camelCase, the file scores are PascalCase.
    private const string CrapByFileJson = """
        {
          "schemaVersion": "1.0",
          "threshold": 6,
          "fileCount": 3,
          "productionFileCount": 2,
          "files": [
            { "FilePath": "/repo/src/UnitTests/ServiceTests.cs", "MethodCount": 9, "CrappyMethodCount": 3, "MaxCrap": 42.0, "IsProduction": false },
            { "FilePath": "/repo/src/App/Service.cs", "MethodCount": 2, "CrappyMethodCount": 0, "MaxCrap": 5.8312, "IsProduction": true },
            { "FilePath": "/repo/src/App/Handler.cs", "MethodCount": 1, "CrappyMethodCount": 0, "MaxCrap": 2, "IsProduction": true }
          ]
        }
        """;

    private const string CrapViolationsJson = """{ "schemaVersion": "1.0", "threshold": 6, "violationCount": 0, "methods": [] }""";

    /// <summary>
    /// Writes a repository under <paramref name="folder"/>: five counted sources, files the count skips,
    /// and one untracked file. Returns its absolute path.
    /// </summary>
    public static async Task<string> WriteRepositoryAsync(ScratchTree work, string folder)
    {
        WriteTrackedFiles(work, folder);
        await GitAsync(work.PathOf(folder), "init", "--quiet");
        await GitAsync(work.PathOf(folder), "add", "--all");
        work.Write($"{folder}/src/App/Scratch.cs", "public class Scratch;\n");
        return work.PathOf(folder);
    }

    /// <summary>Writes every artifact the script reads under <paramref name="folder"/>. Returns its absolute path.</summary>
    public static string WriteEveryArtifact(ScratchTree work, string folder)
    {
        WriteTestResults(work, folder);
        work.Write($"{folder}/{UnitCoverage}", UnitCoverageXml)
            .Write($"{folder}/{IntegrationCoverage}", IntegrationCoverageXml)
            .Write($"{folder}/crap-metrics-linux/crap-by-file.json", CrapByFileJson)
            .Write($"{folder}/crap-metrics-linux/crap-production-violations.json", CrapViolationsJson)
            .Write($"{folder}/{QodanaSarif}", Sarif("unchanged", "unchanged", "new", "absent"));
        return work.PathOf(folder);
    }

    /// <summary>One result per baseline state, in the shape Qodana writes when it compares with a baseline.</summary>
    public static string Sarif(params string[] baselineStates)
    {
        var results = baselineStates.Select(state =>
            $$"""{ "ruleId": "ConvertToPrimaryConstructor", "baselineState": "{{state}}" }""");
        return $$"""{ "version": "2.1.0", "runs": [ { "results": [ {{string.Join(", ", results)}} ] } ] }""";
    }

    private static void WriteTrackedFiles(ScratchTree work, string folder)
    {
        work.Write($"{folder}/src/App/Program.cs", "namespace App;\n\npublic static class Program\n{\n    public static void Main() { }\n}\n")
            .Write($"{folder}/src/App/Page.razor", "@page \"/\"\r\n\r\n<h1>Home</h1>\r\n")
            .Write($"{folder}/src/Database/001_Create.sql", "CREATE TABLE dbo.WorkOrder\n\t(Id INT)\n   \n")
            .Write($"{folder}/README.md", "# Example\n\nText.\n")
            .Write($"{folder}/src/App/Service.cs", "namespace App;\npublic class Service;\n");

        // Tracked, but not written by hand or not a language that is counted.
        work.Write($"{folder}/src/App/Generated/Client.cs", "public class Client;\n")
            .Write($"{folder}/src/App/Model.Designer.cs", "public class Model;\n")
            .Write($"{folder}/src/App/wwwroot/lib/vendor/vendor.js", "var a = 1;\n")
            .Write($"{folder}/src/App/wwwroot/css/site.min.css", "a{b:c}\n")
            .Write($"{folder}/src/App/bin/Release/Leftover.cs", "public class Leftover;\n")
            .Write($"{folder}/src/App/appsettings.json", "{ \"a\": 1 }\n");
    }

    // upload-artifact keeps the path below the common parent of what it uploads: build/test for the unit and
    // integration results, the folder of the single trx file for the acceptance results. The sqlite run repeats
    // the unit tests on another platform and is not counted again.
    private static void WriteTestResults(ScratchTree work, string folder)
    {
        work.Write($"{folder}/test-results-linux/UnitTests/unit.trx", Trx("ClearMeasure.Bootcamp.UnitTests.dll", 5, 4))
            .Write($"{folder}/test-results-linux/IntegrationTests/integration.trx", Trx("ClearMeasure.Bootcamp.IntegrationTests.dll", 3, 3))
            .Write($"{folder}/test-results-acceptance/acceptance.trx", Trx("ClearMeasure.Bootcamp.AcceptanceTests.dll", 2, 2))
            .Write($"{folder}/test-results-sqlite/UnitTests/unit.trx", Trx("ClearMeasure.Bootcamp.UnitTests.dll", 99, 99));
    }

    private static string Trx(string assembly, int total, int executed) =>
        $"""
         <?xml version="1.0" encoding="utf-8"?>
         <TestRun id="5d0f3e2a-1111-4222-8333-444455556666" name="run" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
           <TestDefinitions>
             <UnitTest name="Should_Work" storage="/repo/src/tests/bin/release/net10.0/{assembly.ToLowerInvariant()}" id="6f1c7c8e-1111-4222-8333-444455556666">
               <TestMethod codeBase="/repo/src/Tests/bin/Release/net10.0/{assembly}" adapterTypeName="executor://nunit3testexecutor/" className="Example.Tests" name="Should_Work" />
             </UnitTest>
           </TestDefinitions>
           <ResultSummary outcome="Completed">
             <Counters total="{total}" executed="{executed}" passed="{executed}" failed="0" error="0" notExecuted="{total - executed}" />
           </ResultSummary>
         </TestRun>
         """;

    private static async Task GitAsync(string repository, params string[] arguments)
    {
        var result = await new ExternalCommand("git", arguments, repository).RunAsync();
        result.ExitCode.ShouldBe(0, result.Output);
    }
}
