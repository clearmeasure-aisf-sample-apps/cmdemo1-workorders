using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware;

[TestFixture]
public class HttpEnvelopeFluentValidatorsTests
{
    [Test]
    public void IsDeferredFromMediatR_ShouldBeTrue_WhenCreateVendorValidator()
    {
        HttpEnvelopeFluentValidators.IsDeferredFromMediatR(typeof(CreateVendorCommandValidator)).ShouldBeTrue();
    }

    [Test]
    public void IsDeferredFromMediatR_ShouldBeTrue_WhenCreateTeamValidator()
    {
        HttpEnvelopeFluentValidators.IsDeferredFromMediatR(typeof(CreateTeamCommandValidator)).ShouldBeTrue();
    }

    [Test]
    public void IsDeferredFromMediatR_ShouldBeTrue_WhenCreateDatedWorkOrdersValidator()
    {
        HttpEnvelopeFluentValidators.IsDeferredFromMediatR(typeof(CreateDatedWorkOrdersCommandValidator))
            .ShouldBeTrue();
    }

    [Test]
    public void IsDeferredFromMediatR_ShouldBeFalse_WhenSaveDraftValidator()
    {
        HttpEnvelopeFluentValidators.IsDeferredFromMediatR(typeof(SaveDraftCommandValidator)).ShouldBeFalse();
    }

    [Test]
    public void Resolve_ShouldReturnConcreteValidator_WhenCreateVendorPayload()
    {
        var validator = HttpEnvelopeFluentValidators.Resolve(
            new CreateVendorCommand("", null, null, null, null));

        validator.ShouldBeOfType<CreateVendorCommandValidator>();
    }

    [Test]
    public void Resolve_ShouldReturnConcreteValidator_WhenCreateDatedWorkOrdersPayload()
    {
        var validator = HttpEnvelopeFluentValidators.Resolve(
            new CreateDatedWorkOrdersCommand("a", "b", "t", "d", []));

        validator.ShouldBeOfType<CreateDatedWorkOrdersCommandValidator>();
    }

    [Test]
    public void Resolve_ShouldReturnNull_WhenPayloadIsNotDeferred()
    {
        HttpEnvelopeFluentValidators.Resolve(new object()).ShouldBeNull();
    }
}
