using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// #1824 design (from #1804 decision c): FluentValidation for CreateVendor, CreateTeam, and
/// CreateDatedWorkOrders runs on the HTTP envelope only so HTTP 400 responses stay unchanged.
/// In-process MCP / IBus callers never get <c>ValidationException</c> for these; they read
/// Failed results from each command's <c>FindRuleViolations</c> seam in the handler. Both host
/// registries and the HTTP payload resolver use this one list — do not register these validators
/// as MediatR <c>IValidator&lt;T&gt;</c> and do not duplicate the three types elsewhere.
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
    /// so in-process validation stays on <c>FindRuleViolations</c> / Failed results.
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
