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

    [Test]
    public void Resolve_ShouldReturnNull_WhenPayloadIsNotDeferred()
    {
        var services = new ServiceCollection().AddSingleton<HttpEnvelopeValidatorSet>().BuildServiceProvider();

        HttpEnvelopeFluentValidators.Resolve(new object(), services).ShouldBeNull();
    }
}
