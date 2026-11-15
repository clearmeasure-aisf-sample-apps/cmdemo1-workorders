using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
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
    public void ShouldNotRegisterDeferredValidatorsAsIValidator_WhenUiServiceRegistryIsBuilt()
    {
        using var container = BuildUiContainer();

        container.TryGetInstance<IValidator<CreateVendorCommand>>().ShouldBeNull();
        container.TryGetInstance<IValidator<CreateTeamCommand>>().ShouldBeNull();
        container.TryGetInstance<IValidator<CreateDatedWorkOrdersCommand>>().ShouldBeNull();
        container.GetInstance<IValidator<SaveDraftCommand>>().ShouldNotBeNull();
    }

    [Test]
    public void ShouldResolveHttpEnvelopeValidatorSet_WhenUiServiceRegistryIsBuilt()
    {
        using var container = BuildUiContainer();

        var set = container.GetInstance<HttpEnvelopeValidatorSet>();
        set.For(new CreateVendorCommand("", null, null, null, null))
            .ShouldBeOfType<CreateVendorCommandValidator>();
        set.For(new CreateTeamCommand("Crew", null, []))
            .ShouldBeOfType<CreateTeamCommandValidator>();
        set.For(new CreateDatedWorkOrdersCommand("a", "b", "t", "d", []))
            .ShouldBeOfType<CreateDatedWorkOrdersCommandValidator>();
    }

    private static Container BuildUiContainer() =>
        new(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<UiServiceRegistry>();
        });
}
