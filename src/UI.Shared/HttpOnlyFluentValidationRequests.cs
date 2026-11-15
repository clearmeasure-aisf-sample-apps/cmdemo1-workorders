using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Request types whose FluentValidation rules stay on the HTTP payload path only.
/// In-process MCP / Blazor callers already return today's domain results for the same
/// invalid inputs; enabling those rules in <see cref="ValidationBehavior{TRequest,TResponse}"/>
/// would change behavior (#1804).
/// </summary>
public static class HttpOnlyFluentValidationRequests
{
    /// <summary>
    /// True when in-process validation must defer to the handler / MCP tool so today's results stay.
    /// </summary>
    public static bool AppliesTo(object request) =>
        request is CreateVendorCommand or CreateTeamCommand;
}
