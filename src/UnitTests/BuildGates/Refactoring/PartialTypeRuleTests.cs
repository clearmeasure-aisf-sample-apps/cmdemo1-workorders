using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class PartialTypeRuleTests
{
    [Test]
    public void ShouldFlagPartialClass_WhenItIsSplitAcrossFiles()
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/McpServer/Tools/WorkOrderTools.cs", "public partial class WorkOrderTools { }")
            .WithFile("src/McpServer/Tools/WorkOrderTools.Save.cs", "public partial class WorkOrderTools { }")
            .Build();

        var violations = SourceSetBuilder.KeysOf(new PartialTypeRule(), sources);

        violations.ShouldBe(
        [
            "src/McpServer/Tools/WorkOrderTools.cs :: WorkOrderTools",
            "src/McpServer/Tools/WorkOrderTools.Save.cs :: WorkOrderTools"
        ]);
    }

    [Test]
    public void ShouldPassCodeBehind_WhenItsRazorComponentIsNextToIt()
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/UI.Shared/NavMenu.Razor.cs", "public partial class NavMenu { }")
            .WithRazorComponent("src/UI.Shared/NavMenu.razor")
            .Build();

        var violations = SourceSetBuilder.KeysOf(new PartialTypeRule(), sources);

        violations.ShouldBeEmpty();
    }

    [Test]
    public void ShouldFlagSecondCodeBehindFile_WhenAPageIsSplitToDodgeTheLimit()
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/UI.Shared/Pages/WorkOrderSearch.razor.cs", "public partial class WorkOrderSearch { }")
            .WithFile("src/UI.Shared/Pages/WorkOrderSearch.Filters.razor.cs", "public partial class WorkOrderSearch { }")
            .WithRazorComponent("src/UI.Shared/Pages/WorkOrderSearch.razor")
            .Build();

        var violations = SourceSetBuilder.KeysOf(new PartialTypeRule(), sources);

        violations.ShouldBe(["src/UI.Shared/Pages/WorkOrderSearch.Filters.razor.cs :: WorkOrderSearch"]);
    }

    [Test]
    public void ShouldPassPartialType_WhenASourceGeneratorRequiresIt()
    {
        var sources = new SourceSetBuilder()
            .WithFile(
                "src/LlmGateway/TranslationGuard.cs",
                """
                internal static partial class TranslationGuard
                {
                    [GeneratedRegex("^[a-z]{2}$")]
                    private static partial Regex Bcp47Regex();
                }
                """)
            .Build();

        var violations = SourceSetBuilder.KeysOf(new PartialTypeRule(), sources);

        violations.ShouldBeEmpty();
    }
}
