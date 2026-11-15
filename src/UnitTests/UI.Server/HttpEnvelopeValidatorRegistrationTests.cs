using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server;
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
        using var container = new Container(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<UiServiceRegistry>();
        });

        container.TryGetInstance<IValidator<CreateVendorCommand>>().ShouldBeNull();
        container.TryGetInstance<IValidator<CreateTeamCommand>>().ShouldBeNull();
        container.TryGetInstance<IValidator<CreateDatedWorkOrdersCommand>>().ShouldBeNull();
        container.GetInstance<IValidator<SaveDraftCommand>>().ShouldNotBeNull();
    }
}
