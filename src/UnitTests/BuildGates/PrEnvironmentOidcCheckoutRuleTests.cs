using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class PrEnvironmentOidcCheckoutRuleTests
{
    [Test]
    public void Collect_WhenCheckoutOmitsRef_AddsProblem()
    {
        var job = new WorkflowJobSlice(
            "shards",
            """
              shards:
                permissions:
                  id-token: write
                steps:
                  - uses: actions/checkout@v6.0.1
                  - run: echo hi
            """);
        var problems = new List<string>();

        PrEnvironmentOidcCheckoutRule.Collect(problems, job);

        problems.Count.ShouldBe(1);
        problems[0].ShouldContain("shards");
    }

    [Test]
    public void Collect_WhenCheckoutUsesDefaultBranch_IsSilent()
    {
        var job = new WorkflowJobSlice(
            "upload",
            """
              upload:
                permissions:
                  id-token: write
                steps:
                  - uses: actions/checkout@v6.0.1
                    with:
                      ref: ${{ github.event.repository.default_branch }}
                      path: _trusted
            """);
        var problems = new List<string>();

        PrEnvironmentOidcCheckoutRule.Collect(problems, job);

        problems.ShouldBeEmpty();
    }
}
