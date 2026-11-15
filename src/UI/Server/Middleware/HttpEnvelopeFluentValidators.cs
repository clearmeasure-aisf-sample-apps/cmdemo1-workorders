using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// FluentValidation for <c>CreateVendor</c>, <c>CreateTeam</c>, and <c>CreateDatedWorkOrders</c>
/// stays on the HTTP envelope only (#1804 decision c). Those validators are not registered as
/// <see cref="IValidator{T}"/> so <c>ValidationBehavior</c> leaves in-process MCP / IBus results
/// matching master; <see cref="WebServiceMessagePayloadValidator"/> resolves them here (same
/// outside-pipeline hand-off master used in <c>Bus.ValidateRequest</c>). Follow-up: #1824.
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

    /// <summary>
    /// Builds the deferred FluentValidation validator for a payload, if this request type
    /// keeps its rules on the HTTP envelope only.
    /// </summary>
    internal static IValidator? Resolve(object payload) =>
        payload switch
        {
            CreateVendorCommand => new CreateVendorCommandValidator(),
            CreateTeamCommand => new CreateTeamCommandValidator(),
            CreateDatedWorkOrdersCommand => new CreateDatedWorkOrdersCommandValidator(),
            _ => null
        };
}
