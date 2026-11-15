using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class PrEnvironmentOidcCheckoutRuleTests
{
    [Test]
    public void Collect_WhenNameThenUsesOmitsRef_AddsProblem()
    {
        var problems = Collect("probe-oidc-branch", """
                  - name: Checkout code
                    uses: actions/checkout@v6.0.1
                  - run: echo hi
            """);

        problems.Count.ShouldBe(1);
        problems[0].ShouldContain("probe-oidc-branch");
    }

    [Test]
    public void Collect_WhenUsesFirstOmitsRef_AddsProblem()
    {
        var problems = Collect("shards", """
                  - uses: actions/checkout@v6.0.1
                  - run: echo hi
            """);

        problems.Count.ShouldBe(1);
        problems[0].ShouldContain("shards");
    }

    [Test]
    public void Collect_WhenCheckoutUsesDefaultBranch_IsSilent()
    {
        var problems = Collect("upload", """
                  - name: Checkout trusted scripts (default branch)
                    uses: actions/checkout@v6.0.1
                    with:
                      ref: ${{ github.event.repository.default_branch }}
                      path: _trusted
                      persist-credentials: false
            """);

        problems.ShouldBeEmpty();
    }

    [Test]
    public void Collect_WhenRepositoryIsGithubRepositoryExpression_AddsProblem()
    {
        var problems = Collect("spoof", """
                  - name: Checkout
                    uses: actions/checkout@v6.0.1
                    with:
                      repository: ${{ github.repository }}
                      ref: ${{ github.sha }}
            """);

        problems.Count.ShouldBe(1);
        problems[0].ShouldContain("spoof");
    }

    [Test]
    public void Collect_WhenCmdemo1SystemMain_IsSilent()
    {
        var problems = Collect("lease", """
                  - name: Checkout pool manager (cmdemo1-system)
                    uses: actions/checkout@v6.0.1
                    with:
                      repository: clearmeasure-aisf-sample-apps/cmdemo1-system
                      path: _system
                      ref: main
                      persist-credentials: false
            """);

        problems.ShouldBeEmpty();
    }

    private static List<string> Collect(string jobId, string stepsYaml)
    {
        var job = new WorkflowJobSlice(
            jobId,
            $"""
              {jobId}:
                permissions:
                  id-token: write
                steps:
            {stepsYaml}
            """);
        var problems = new List<string>();
        PrEnvironmentOidcCheckoutRule.Collect(problems, job);
        return problems;
    }
}
