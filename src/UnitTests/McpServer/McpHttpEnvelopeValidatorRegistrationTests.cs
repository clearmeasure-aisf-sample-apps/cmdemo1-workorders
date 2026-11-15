using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.McpServer;
using FluentValidation;
using Lamar;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class McpHttpEnvelopeValidatorRegistrationTests
{
    [Test]
    public void ShouldNotRegisterDeferredValidatorsAsIValidator_WhenMcpServiceRegistryIsBuilt()
    {
        using var container = new Container(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<McpServiceRegistry>();
        });

        container.TryGetInstance<IValidator<CreateVendorCommand>>().ShouldBeNull();
        container.TryGetInstance<IValidator<CreateTeamCommand>>().ShouldBeNull();
        container.TryGetInstance<IValidator<CreateDatedWorkOrdersCommand>>().ShouldBeNull();
        container.GetInstance<IValidator<SaveDraftCommand>>().ShouldNotBeNull();
    }

    [TestCase(typeof(CreateVendorCommandValidator))]
    [TestCase(typeof(CreateTeamCommandValidator))]
    [TestCase(typeof(CreateDatedWorkOrdersCommandValidator))]
    public void ShouldTreatCatalogValidatorsAsDeferred_WhenHttpEnvelopeValidationDeferralIsRead(Type validatorType)
    {
        HttpEnvelopeValidationDeferral.IsDeferredValidator(validatorType).ShouldBeTrue();
    }
}
