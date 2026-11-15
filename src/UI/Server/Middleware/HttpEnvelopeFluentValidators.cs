using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// FluentValidation for <c>CreateVendor</c>, <c>CreateTeam</c>, and <c>CreateDatedWorkOrders</c>
/// stays on the HTTP envelope only (#1804 decision c). Those validators are registered as
/// concrete types here and resolved by <see cref="WebServiceMessagePayloadValidator"/>;
/// they are not registered as <see cref="IValidator{T}"/> so <c>ValidationBehavior</c> leaves
/// in-process MCP / IBus results matching master. Follow-up: #1824.
/// </summary>
internal static class HttpEnvelopeFluentValidators
{
    private static readonly Type[] DeferredValidatorTypes =
    [
        typeof(CreateVendorCommandValidator),
        typeof(CreateTeamCommandValidator),
        typeof(CreateDatedWorkOrdersCommandValidator)
    ];

    /// <summary>True when assembly scanning must skip this validator for MediatR <see cref="IValidator{T}"/>.</summary>
    internal static bool IsDeferredFromMediatR(Type validatorType) =>
        DeferredValidatorTypes.Contains(validatorType);

    /// <summary>Registers the three validators as concrete services for the HTTP payload path.</summary>
    internal static void RegisterConcrete(IServiceCollection services)
    {
        foreach (var validatorType in DeferredValidatorTypes)
        {
            services.AddTransient(validatorType);
        }
    }

    /// <summary>
    /// Resolves the deferred FluentValidation validator for a payload, if this request type
    /// keeps its rules on the HTTP envelope only.
    /// </summary>
    internal static IValidator? Resolve(object payload, IServiceProvider services) =>
        payload switch
        {
            CreateVendorCommand => services.GetService<CreateVendorCommandValidator>(),
            CreateTeamCommand => services.GetService<CreateTeamCommandValidator>(),
            CreateDatedWorkOrdersCommand => services.GetService<CreateDatedWorkOrdersCommandValidator>(),
            _ => null
        };
}
