using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using FluentValidation;
using Lamar;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class McpHttpEnvelopeValidatorRegistrationTests
{
    [Test]
    public void ShouldOmitDeferredIValidators_WhenMcpServiceRegistryIsBuilt()
    {
        using var container = BuildMcpContainer();

        DeferredIValidatorRegistration.ShouldOmitDeferredCreateCommands(container);
        container.GetInstance<IValidator<SaveDraftCommand>>().ShouldNotBeNull();
    }

    [Test]
    public void ShouldResolveInProcessCreateShapeRules_WhenMcpServiceRegistryIsBuilt()
    {
        using var container = BuildMcpContainer();

        container.GetInstance<CreateVendorCommandShapeRules>().ShouldNotBeNull();
        container.GetInstance<CreateTeamCommandShapeRules>().ShouldNotBeNull();
        container.GetInstance<CreateDatedWorkOrdersCommandShapeRules>().ShouldNotBeNull();
        DeferredIValidatorRegistration.ShouldOmitDeferredCreateCommands(container);
    }

    private static Container BuildMcpContainer() =>
        new(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<McpServiceRegistry>();
        });
}
