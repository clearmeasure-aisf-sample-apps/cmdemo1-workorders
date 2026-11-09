using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class QodanaTestScopeGateTests
{
    [Test]
    public void QodanaYaml_StillHasWholeSolutionFailThresholdZero()
    {
        var yaml = RepositoryCheckout.Current.ReadText("qodana.yaml");

        yaml.ShouldContain("failThreshold: 0");
        yaml.ShouldContain("solution: src/ChurchBulletin.sln");
    }

    [Test]
    public void BuildYml_QodanaStep_StillPassesFailThresholdZero()
    {
        var workflow = RepositoryCheckout.Current.ReadText(".github/workflows/build.yml");

        workflow.ShouldContain("--fail-threshold,0");
        workflow.ShouldContain("--solution,src/ChurchBulletin.sln");
    }

    [Test]
    public void UnitTests_EditorConfig_DowngradesRedundantUsingAndNullableSuppression()
    {
        AssertEditorConfigDowngrades("src/UnitTests/.editorconfig");
    }

    [Test]
    public void IntegrationTests_EditorConfig_DowngradesRedundantUsingAndNullableSuppression()
    {
        AssertEditorConfigDowngrades("src/IntegrationTests/.editorconfig");
    }

    [Test]
    public void AcceptanceTests_EditorConfig_DowngradesRedundantUsingAndNullableSuppression()
    {
        AssertEditorConfigDowngrades("src/AcceptanceTests/.editorconfig");
    }

    [Test]
    public void SrcEditorConfig_DoesNotDowngradeQodanaGatingInspections()
    {
        var config = RepositoryCheckout.Current.ReadText("src/.editorconfig");

        config.ShouldNotContain("resharper_redundant_using_directive_highlighting = none");
        config.ShouldNotContain("resharper_redundant_nullable_warning_suppression_highlighting = none");
    }

    private static void AssertEditorConfigDowngrades(string relativePath)
    {
        var config = RepositoryCheckout.Current.ReadText(relativePath);

        config.ShouldContain("resharper_redundant_using_directive_highlighting = none");
        config.ShouldContain("resharper_redundant_nullable_warning_suppression_highlighting = none");
        config.ShouldContain("root = false");
    }

}
