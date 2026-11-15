using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware;

[TestFixture]
public class HttpEnvelopeFluentValidatorsTests
{
    [TestCase(typeof(CreateVendorCommandValidator), true)]
    [TestCase(typeof(CreateTeamCommandValidator), true)]
    [TestCase(typeof(CreateDatedWorkOrdersCommandValidator), true)]
    [TestCase(typeof(SaveDraftCommandValidator), false)]
    public void IsDeferredFromMediatR_ShouldMatchCatalog(Type validatorType, bool deferred)
    {
        HttpEnvelopeFluentValidators.IsDeferredFromMediatR(validatorType).ShouldBe(deferred);
    }

    [Test]
    public void Resolve_ShouldReturnVendorValidator_WhenSetIsRegistered()
    {
        var services = new ServiceCollection().AddSingleton<HttpEnvelopeValidatorSet>().BuildServiceProvider();

        HttpEnvelopeFluentValidators.Resolve(new CreateVendorCommand("", null, null, null, null), services)
            .ShouldBeOfType<CreateVendorCommandValidator>();
    }

    [Test]
    public void Resolve_ShouldReturnNull_WhenSetIsMissing()
    {
        HttpEnvelopeFluentValidators.Resolve(
            new CreateVendorCommand("", null, null, null, null),
            new ServiceCollection().BuildServiceProvider()).ShouldBeNull();
    }
}
