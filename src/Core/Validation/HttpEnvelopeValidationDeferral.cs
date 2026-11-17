using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// #1824 design (from #1804 decision c): FluentValidation for CreateVendor, CreateTeam, and
/// CreateDatedWorkOrders is listed here so host registries omit them from MediatR
/// <c>IValidator&lt;T&gt;</c> registration and HTTP 400 responses stay on
/// <c>HttpEnvelopeValidatorSet</c>. In-process MCP / IBus callers never get
/// <c>ValidationException</c>; handlers inject the concrete validator types and map shape failures
/// to Failed results, with stored-data rules on each command's <c>FindRuleViolations</c>.
/// </summary>
public static class HttpEnvelopeValidationDeferral
{
    /// <summary>Request → concrete validator pairs for the HTTP-envelope host only.</summary>
    public static IReadOnlyList<(Type RequestType, Type ValidatorType)> Entries { get; } =
    [
        (typeof(CreateVendorCommand), typeof(CreateVendorCommandValidator)),
        (typeof(CreateTeamCommand), typeof(CreateTeamCommandValidator)),
        (typeof(CreateDatedWorkOrdersCommand), typeof(CreateDatedWorkOrdersCommandValidator))
    ];

    /// <summary>
    /// True when assembly scanning must skip this type for MediatR <c>IValidator&lt;T&gt;</c>
    /// so in-process validation stays on injected concrete validators and Failed results.
    /// </summary>
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
}
