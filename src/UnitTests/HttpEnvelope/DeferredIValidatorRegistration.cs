using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;
using Lamar;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.HttpEnvelope;

/// <summary>Shared assert for UI and MCP: deferred creates are not unkeyed <see cref="IValidator{T}"/>.</summary>
internal static class DeferredIValidatorRegistration
{
    internal static void ShouldOmitDeferredCreateCommands(IContainer container)
    {
        container.TryGetInstance<IValidator<CreateVendorCommand>>().ShouldBeNull();
        container.TryGetInstance<IValidator<CreateTeamCommand>>().ShouldBeNull();
        container.TryGetInstance<IValidator<CreateDatedWorkOrdersCommand>>().ShouldBeNull();
    }
}
