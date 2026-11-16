using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// #1824 deferral: FluentValidation for CreateVendor, CreateTeam, and CreateDatedWorkOrders
/// stays on the HTTP envelope only so in-process MCP / IBus callers keep master's handler
/// results. Both host registries and the HTTP payload resolver use this one list - do not
/// duplicate the three types elsewhere (#1804).
/// </summary>
public static class HttpEnvelopeValidationDeferral
{
    /// <summary>A deferred request type, its validator type, and how to build the validator without reflection.</summary>
    public sealed record DeferredValidator(Type RequestType, Type ValidatorType, Func<IValidator> Create);

    /// <summary>Request / concrete validator pairs for the HTTP-envelope host.</summary>
    public static IReadOnlyList<DeferredValidator> Entries { get; } =
    [
        Defer<CreateVendorCommand, CreateVendorCommandValidator>(),
        Defer<CreateTeamCommand, CreateTeamCommandValidator>(),
        Defer<CreateDatedWorkOrdersCommand, CreateDatedWorkOrdersCommandValidator>()
    ];

    /// <summary>True when assembly scanning must skip this type for mediator <c>IValidator&lt;T&gt;</c>.</summary>
    public static bool IsDeferredValidator(Type validatorType)
    {
        for (var i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].ValidatorType == validatorType)
            {
                return true;
            }
        }

        return false;
    }

    private static DeferredValidator Defer<TRequest, TValidator>()
        where TValidator : IValidator<TRequest>, new() =>
        new(typeof(TRequest), typeof(TValidator), () => new TValidator());
}
