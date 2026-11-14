using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

[TestFixture]
public class BrowserFixtureSetupFallbackTests
{
    [Test]
    public void BrowserFixtureSetup_WhenRead_ProbesThenFallsBackOn404()
    {
        var setup = RepositoryCheckout.Current.ReadText("src/AcceptanceTests/BrowserFixtureSetup.cs");
        var factory = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/AcceptanceEmployeeProvisionerFactory.cs");
        var provisioner = RepositoryCheckout.Current.ReadText(
            "src/UnitTests/Acceptance/AcceptanceEmployeeProvisioner.cs");
        var employees = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/TestUsers/AcceptanceTestEmployees.cs");

        setup.ShouldContain("CreateEmployeeAsync");
        setup.ShouldContain("AcceptanceEmployeeProvisionerFactory");
        setup.ShouldContain("useStorageStateSignIn");
        setup.ShouldContain("request.UseStorageStateSignIn;");
        setup.ShouldNotContain("provision.TestSupportAvailable");
        factory.ShouldContain("ProbeStatusAsync");
        factory.ShouldContain("AcceptanceTestEmployees.Add");
        provisioner.ShouldContain("HttpStatusCode.NotFound");
        employees.ShouldContain("TestHost.NewDbContext()");
        employees.ShouldContain("!{testTag}");
        employees.ShouldContain("TestSupportEmployeeFields");
        employees.ShouldContain("ApplyOptionalNames");
        employees.ShouldContain("ApplyAdditionalCreateRoles");
    }
}
