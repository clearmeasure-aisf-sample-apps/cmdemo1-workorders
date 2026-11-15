using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// #1824 deferral: FluentValidation for CreateVendor, CreateTeam, and CreateDatedWorkOrders
/// stays on the HTTP envelope only so in-process MCP / IBus callers keep master's handler
/// results. Both host registries and the HTTP payload resolver use this one list — do not
/// duplicate the three types elsewhere (#1804).
/// </summary>
public static class HttpEnvelopeValidationDeferral
{
    /// <summary>Concrete validators that must not register as <c>IValidator&lt;T&gt;</c> for MediatR.</summary>
    public static IReadOnlyList<Type> ValidatorTypes { get; } =
    [
        typeof(CreateVendorCommandValidator),
        typeof(CreateTeamCommandValidator),
        typeof(CreateDatedWorkOrdersCommandValidator)
    ];

    /// <summary>Request → concrete validator pairs for the HTTP-envelope host.</summary>
    public static IReadOnlyList<(Type RequestType, Type ValidatorType)> Entries { get; } =
    [
        (typeof(CreateVendorCommand), typeof(CreateVendorCommandValidator)),
        (typeof(CreateTeamCommand), typeof(CreateTeamCommandValidator)),
        (typeof(CreateDatedWorkOrdersCommand), typeof(CreateDatedWorkOrdersCommandValidator))
    ];

    /// <summary>True when assembly scanning must skip this type for MediatR <c>IValidator&lt;T&gt;</c>.</summary>
    public static bool IsDeferredValidator(Type validatorType) =>
        ValidatorTypes.Contains(validatorType);

    /// <summary>Concrete validator type for a request that is deferred to the HTTP envelope, else null.</summary>
    public static Type? ValidatorTypeForRequest(Type requestType)
    {
        foreach (var (candidateRequest, validatorType) in Entries)
        {
            if (candidateRequest == requestType)
            {
                return validatorType;
            }
        }

        return null;
    }
}
