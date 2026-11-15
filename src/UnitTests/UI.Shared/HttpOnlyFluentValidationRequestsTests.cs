using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class HttpOnlyFluentValidationRequestsTests
{
    [Test]
    public void AppliesTo_WhenCreateVendor_IsTrue()
    {
        HttpOnlyFluentValidationRequests.AppliesTo(new CreateVendorCommand("Acme", "HVAC", null, null, null))
            .ShouldBeTrue();
    }

    [Test]
    public void AppliesTo_WhenCreateTeam_IsTrue()
    {
        HttpOnlyFluentValidationRequests.AppliesTo(new CreateTeamCommand("Crew", null, []))
            .ShouldBeTrue();
    }

    [Test]
    public void AppliesTo_WhenUnrelatedRequest_IsFalse()
    {
        HttpOnlyFluentValidationRequests.AppliesTo(new object()).ShouldBeFalse();
    }
}
