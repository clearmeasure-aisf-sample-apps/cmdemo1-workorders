using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.UnitTests.HttpEnvelope;
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
        using var container = new Container(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<McpServiceRegistry>();
        });

        DeferredIValidatorRegistration.ShouldOmitDeferredCreateCommands(container);
        container.GetInstance<IValidator<SaveDraftCommand>>().ShouldNotBeNull();
    }
}
