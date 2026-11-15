using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class WorkflowYamlStepsTests
{
    [Test]
    public void In_WhenNameThenUsesStep_ReturnsOneBlockContainingBothKeys()
    {
        // Six-space step markers match .github/workflows (and WorkflowFile).
        const string yaml = """
                  - name: Checkout code
                    uses: actions/checkout@v6.0.1
                  - run: echo hi
            """;

        var steps = WorkflowYamlSteps.In(yaml).ToList();

        steps.Count.ShouldBe(2);
        steps[0].ShouldContain("name: Checkout code");
        steps[0].ShouldContain("uses: actions/checkout@");
        steps[1].ShouldContain("run: echo hi");
    }
}
