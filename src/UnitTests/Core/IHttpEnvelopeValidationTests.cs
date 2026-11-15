using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core;

/// <summary>
/// Characterization: CreateVendor, CreateTeam, and CreateDatedWorkOrders keep FluentValidation on
/// the HTTP envelope only via <see cref="IHttpEnvelopeValidation"/> (#1804 review).
/// </summary>
[TestFixture]
public class IHttpEnvelopeValidationTests
{
    [Test]
    public void ShouldMarkCreateVendor_WhenHttpEnvelopeValidationIsRequired()
    {
        typeof(IHttpEnvelopeValidation).IsAssignableFrom(typeof(CreateVendorCommand)).ShouldBeTrue();
    }

    [Test]
    public void ShouldMarkCreateTeam_WhenHttpEnvelopeValidationIsRequired()
    {
        typeof(IHttpEnvelopeValidation).IsAssignableFrom(typeof(CreateTeamCommand)).ShouldBeTrue();
    }

    [Test]
    public void ShouldMarkCreateDatedWorkOrders_WhenHttpEnvelopeValidationIsRequired()
    {
        typeof(IHttpEnvelopeValidation).IsAssignableFrom(typeof(CreateDatedWorkOrdersCommand)).ShouldBeTrue();
    }

    [Test]
    public void ShouldStayRemotable_WhenHttpEnvelopeValidationIsApplied()
    {
        typeof(IRemotableRequest).IsAssignableFrom(typeof(CreateVendorCommand)).ShouldBeTrue();
        typeof(IRemotableRequest).IsAssignableFrom(typeof(CreateTeamCommand)).ShouldBeTrue();
        typeof(IRemotableRequest).IsAssignableFrom(typeof(CreateDatedWorkOrdersCommand)).ShouldBeTrue();
    }
}
