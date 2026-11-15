using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class PrEnvironmentOidcJobGateTests
{
    [Test]
    public void Problems_WhenOidcJobChecksOutBranchHead_ReportsJob()
    {
        const string yaml = """
            jobs:
              probe-oidc-branch:
                permissions:
                  id-token: write
                steps:
                  - name: Checkout code
                    uses: actions/checkout@v6.0.1
                  - run: echo hi
            """;

        var problems = PrEnvironmentOidcJobGate.Problems(yaml);

        problems.ShouldContain(p => p.Contains("probe-oidc-branch", StringComparison.Ordinal));
    }

    [Test]
    public void Problems_WhenOidcJobUsesDefaultBranchRef_IsClean()
    {
        const string yaml = """
            jobs:
              upload:
                permissions:
                  id-token: write
                steps:
                  - uses: actions/checkout@v6.0.1
                    with:
                      ref: ${{ github.event.repository.default_branch }}
                      path: _trusted
                      persist-credentials: false
            """;

        PrEnvironmentOidcJobGate.Problems(yaml).ShouldBeEmpty();
    }

    [Test]
    public void PrEnvironmentWorkflow_WhenOidcJobsPresent_NeverCheckoutBranchHead()
    {
        var workflow = RepositoryCheckout.Current.ReadText(".github/workflows/pr-environment.yml");

        PrEnvironmentOidcJobGate.Problems(workflow).ShouldBeEmpty();
    }
}
