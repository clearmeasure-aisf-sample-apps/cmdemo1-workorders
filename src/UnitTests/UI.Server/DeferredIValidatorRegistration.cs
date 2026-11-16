using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>Shared assert for UI and MCP: deferred creates are not unkeyed <see cref="IValidator{T}"/>.</summary>
internal static class DeferredIValidatorRegistration
{
    internal static void ShouldOmitDeferredCreateCommands(IServiceProvider provider)
    {
        provider.GetService<IValidator<CreateVendorCommand>>().ShouldBeNull();
        provider.GetService<IValidator<CreateTeamCommand>>().ShouldBeNull();
        provider.GetService<IValidator<CreateDatedWorkOrdersCommand>>().ShouldBeNull();
    }
}
