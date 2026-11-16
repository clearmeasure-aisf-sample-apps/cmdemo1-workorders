using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HttpEnvelopeValidatorRegistrationTests
{
    [Test]
    public void ShouldOmitDeferredIValidators_WhenUiServicesAreAdded()
    {
        using var provider = BuildUiProvider();

        DeferredIValidatorRegistration.ShouldOmitDeferredCreateCommands(provider);
        provider.GetService<IValidator<SaveDraftCommand>>().ShouldNotBeNull();
    }

    [Test]
    public void ShouldExposeHttpEnvelopeValidatorSet_WhenUiServicesAreAdded()
    {
        using var provider = BuildUiProvider();

        provider.GetService<HttpEnvelopeValidatorSet>().ShouldNotBeNull();
    }

    private static ServiceProvider BuildUiProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddUiServices();
        return services.BuildServiceProvider();
    }
}
