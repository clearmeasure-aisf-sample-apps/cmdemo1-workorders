using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using ClearMeasure.Bootcamp.UnitTests.HttpEnvelope;
using FluentValidation;
using Lamar;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HttpEnvelopeValidatorRegistrationTests
{
    [Test]
    public void ShouldOmitDeferredIValidatorsAndExposeValidatorSet_WhenUiServiceRegistryIsBuilt()
    {
        using var container = BuildUiContainer();

        DeferredIValidatorRegistration.ShouldOmitDeferredCreateCommands(container);
        container.GetInstance<IValidator<SaveDraftCommand>>().ShouldNotBeNull();
        container.GetInstance<HttpEnvelopeValidatorSet>().ShouldNotBeNull();
    }

    private static Container BuildUiContainer() =>
        new(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<UiServiceRegistry>();
        });
}
