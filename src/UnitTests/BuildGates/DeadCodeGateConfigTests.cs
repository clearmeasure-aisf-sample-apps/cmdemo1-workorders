using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class DeadCodeGateConfigTests
{
    private static readonly string[] UnusedSymbolInspections =
    [
        "UnusedMember.Global",
        "UnusedMember.Local",
        "UnusedType.Global",
        "UnusedType.Local",
        "UnusedParameter.Global",
        "UnusedParameter.Local",
        "UnusedVariable",
        "UnusedVariable.Compiler",
        "UnusedMethodReturnValue.Global",
        "UnusedMethodReturnValue.Local",
        "UnusedAutoPropertyAccessor.Global",
        "UnusedAutoPropertyAccessor.Local",
        "UnusedLocalFunction",
        "UnusedField.Compiler",
        "UnusedMemberInSuper.Global",
        "UnusedMemberHierarchy.Global",
        "ClassNeverInstantiated.Global",
        "NotAccessedField.Global",
        "NotAccessedPositionalProperty.Global",
    ];

    [Test]
    public void QodanaYaml_EnablesUnusedSymbolInspectionsAndFailThreshold()
    {
        var yaml = RepositoryCheckout.Current.ReadText("qodana.yaml");

        yaml.ShouldContain("failThreshold: 0");
        foreach (var inspection in UnusedSymbolInspections)
        {
            yaml.ShouldContain("- name: " + inspection);
        }
    }

    [Test]
    public void PrivateBuild_InvokesDeadCodeGates()
    {
        var script = RepositoryCheckout.Current.ReadText("PrivateBuild.ps1");

        script.ShouldContain("scripts/dead-code/Invoke-DeadCodeGates.ps1");
    }

    [Test]
    public void BuildWorkflow_CodeAnalysis_InvokesDeadCodeGates()
    {
        var job = WorkflowFile.Read(RepositoryCheckout.Current, "build.yml").Job("code-analysis").Text;

        job.ShouldContain("scripts/dead-code/Invoke-DeadCodeGates.ps1");
        job.ShouldContain("dotnet build src/ChurchBulletin.sln");
    }
}
