using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using FluentValidation;
using Lamar;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HttpEnvelopeValidatorRegistrationTests
{
    [Test]
    public void ShouldOmitDeferredIValidators_WhenUiServiceRegistryIsBuilt()
    {
        using var container = BuildUiContainer();

        DeferredIValidatorRegistration.ShouldOmitDeferredCreateCommands(container);
        container.GetInstance<IValidator<SaveDraftCommand>>().ShouldNotBeNull();
    }

    [Test]
    public void ShouldExposeHttpEnvelopeValidatorSet_WhenUiServiceRegistryIsBuilt()
    {
        using var container = BuildUiContainer();

        container.GetInstance<HttpEnvelopeValidatorSet>().ShouldNotBeNull();
    }

    [Test]
    public void ShouldResolveInProcessCreateShapeRules_WhenUiServiceRegistryIsBuilt()
    {
        using var container = BuildUiContainer();

        container.GetInstance<CreateVendorCommandShapeRules>().ShouldNotBeNull();
        container.GetInstance<CreateTeamCommandShapeRules>().ShouldNotBeNull();
        container.GetInstance<CreateDatedWorkOrdersCommandShapeRules>().ShouldNotBeNull();
        DeferredIValidatorRegistration.ShouldOmitDeferredCreateCommands(container);
    }

    private static Container BuildUiContainer() =>
        new(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<UiServiceRegistry>();
        });
}
