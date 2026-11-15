using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class PrEnvironmentOidcCheckoutRuleTests
{
    [Test]
    public void Collect_WhenNameThenUsesOmitsRef_AddsProblem()
    {
        var job = new WorkflowJobSlice(
            "probe-oidc-branch",
            """
              probe-oidc-branch:
                permissions:
                  id-token: write
                steps:
                  - name: Checkout code
                    uses: actions/checkout@v6.0.1
                  - run: echo hi
            """);
        var problems = new List<string>();

        PrEnvironmentOidcCheckoutRule.Collect(problems, job);

        problems.Count.ShouldBe(1);
        problems[0].ShouldContain("probe-oidc-branch");
    }

    [Test]
    public void Collect_WhenUsesFirstOmitsRef_AddsProblem()
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
                  - name: Checkout trusted scripts (default branch)
                    uses: actions/checkout@v6.0.1
                    with:
                      ref: ${{ github.event.repository.default_branch }}
                      path: _trusted
                      persist-credentials: false
            """);
        var problems = new List<string>();

        PrEnvironmentOidcCheckoutRule.Collect(problems, job);

        problems.ShouldBeEmpty();
    }

    [Test]
    public void Collect_WhenRepositoryIsGithubRepositoryExpression_AddsProblem()
    {
        var job = new WorkflowJobSlice(
            "spoof",
            """
              spoof:
                permissions:
                  id-token: write
                steps:
                  - name: Checkout
                    uses: actions/checkout@v6.0.1
                    with:
                      repository: ${{ github.repository }}
                      ref: ${{ github.sha }}
            """);
        var problems = new List<string>();

        PrEnvironmentOidcCheckoutRule.Collect(problems, job);

        problems.Count.ShouldBe(1);
        problems[0].ShouldContain("spoof");
    }

    [Test]
    public void Collect_WhenCmdemo1SystemMain_IsSilent()
    {
        var job = new WorkflowJobSlice(
            "lease",
            """
              lease:
                permissions:
                  id-token: write
                steps:
                  - name: Checkout pool manager (cmdemo1-system)
                    uses: actions/checkout@v6.0.1
                    with:
                      repository: clearmeasure-aisf-sample-apps/cmdemo1-system
                      path: _system
                      ref: main
                      persist-credentials: false
            """);
        var problems = new List<string>();

        PrEnvironmentOidcCheckoutRule.Collect(problems, job);

        problems.ShouldBeEmpty();
    }
}
