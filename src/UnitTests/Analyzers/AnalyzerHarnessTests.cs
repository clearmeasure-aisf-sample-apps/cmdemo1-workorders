using ClearMeasure.NpoAnalyzers;
using Microsoft.CodeAnalysis;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers;

[TestFixture]
public class AnalyzerHarnessTests
{
    [Test]
    public async Task ShouldReturnDiagnostics_When_SnippetTriggersAnalyzer()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            public class H {
              public void Run(IEnumerable<int> values) {
                var bad = values.ToList().Where(x => x > 0).ToList();
              }
            }
            """;

        var diagnostics = await AnalyzerHarness.AnalyzeAsync(new PrematureMaterializationAnalyzer(), source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == "CM0004");
    }

    [Test]
    public async Task ShouldThrow_When_SnippetDoesNotCompile()
    {
        const string source = "public class Broken { this is not valid csharp }";

        var act = () => AnalyzerHarness.AnalyzeAsync(new PrematureMaterializationAnalyzer(), source);

        await Should.ThrowAsync<InvalidOperationException>(act);
    }

    [Test]
    public async Task ShouldHonorAssemblyName_When_CompilingSnippet()
    {
        const string source = """
            public class DbContext {}
            public class Page {
              public DbContext Database { get; set; } = null!;
            }
            """;

        var diagnostics = await AnalyzerHarness.AnalyzeAsync(
            new DbContextInUiAnalyzer(),
            source,
            "Contoso.UI");

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == "CM0005");
    }
}
