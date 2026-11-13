using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

[TestFixture]
public class AcceptanceSharedDataPolicyTests
{
    [Test]
    public void SharedDataPolicy_WhenRead_GatesOnStartLocalServer()
    {
        var policy = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/AcceptanceSharedDataPolicy.cs");

        policy.ShouldContain("ShouldLoadSharedData(bool startLocalServer)");
        policy.ShouldContain("=> startLocalServer");
    }

    [Test]
    public void ServerFixture_WhenRemoteHost_SkipsZDataLoaderAndSweep()
    {
        var fixture = RepositoryCheckout.Current.ReadText("src/AcceptanceTests/ServerFixture.cs");

        fixture.ShouldContain("AcceptanceSharedDataPolicy.ShouldLoadSharedData");
        fixture.ShouldContain("MaybeInitializeSharedData");
        fixture.ShouldContain("skipping ZDataLoader and leftover-employee sweep");
        fixture.IndexOf("ReadHostSettings()", StringComparison.Ordinal)
            .ShouldBeLessThan(fixture.IndexOf("MaybeInitializeSharedData()", StringComparison.Ordinal));
    }
}
