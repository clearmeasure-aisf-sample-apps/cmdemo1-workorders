using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.Extensions.DependencyInjection;
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
    public void Resolve_ShouldReturnConcreteValidatorFromDiSet_WhenCreateVendorPayload()
    {
        var services = DeferredValidatorServices();

        var validator = HttpEnvelopeFluentValidators.Resolve(
            new CreateVendorCommand("", null, null, null, null), services);

        validator.ShouldBeOfType<CreateVendorCommandValidator>();
    }

    [Test]
    public void Resolve_ShouldReturnConcreteValidatorFromDiSet_WhenCreateDatedWorkOrdersPayload()
    {
        var services = DeferredValidatorServices();

        var validator = HttpEnvelopeFluentValidators.Resolve(
            new CreateDatedWorkOrdersCommand("a", "b", "t", "d", []), services);

        validator.ShouldBeOfType<CreateDatedWorkOrdersCommandValidator>();
    }

    [Test]
    public void Resolve_ShouldReturnNull_WhenPayloadIsNotDeferred()
    {
        HttpEnvelopeFluentValidators.Resolve(new object(), DeferredValidatorServices()).ShouldBeNull();
    }

    [Test]
    public void Resolve_ShouldReturnNull_WhenSetIsNotRegistered()
    {
        var empty = new ServiceCollection().BuildServiceProvider();

        HttpEnvelopeFluentValidators.Resolve(
            new CreateVendorCommand("", null, null, null, null), empty).ShouldBeNull();
    }

    private static ServiceProvider DeferredValidatorServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<HttpEnvelopeValidatorSet>();
        return services.BuildServiceProvider();
    }
}
