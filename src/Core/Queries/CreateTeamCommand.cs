using ClearMeasure.Bootcamp.Core.Validation;
using MediatR;

// ReSharper disable NotAccessedPositionalProperty.Global -- Message and Id are read by the team UI (later item) and by tests
namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Creates a team with the employees whose user names are listed as members.
/// </summary>
public record CreateTeamCommand(string Name, string? Description, IReadOnlyList<string> MemberUserNames)
    : IRequest<CreateTeamResult>, IRemotableRequest
{
    /// <summary>
    /// In-process validation (#1824): shape rules from <c>CreateTeamCommandValidator</c> (same
    /// messages as HTTP), then duplicate team name and unknown members. An empty list means the team
    /// can be created. Handlers map these to <see cref="CreateTeamResult"/> with <c>Succeeded</c>
    /// false — never a FluentValidation exception. HTTP still runs the envelope FluentValidator for 400.
    /// </summary>
    public IReadOnlyList<string> FindRuleViolations(IEnumerable<string> existingTeamNames,
        IEnumerable<string> knownUserNames)
    {
        var shape = InProcessCreateCommandShapeValidation.ErrorMessages(this);
        if (shape.Count > 0)
        {
            return shape;
        }

        var violations = new List<string>();
        var name = Name.Trim();
        if (existingTeamNames.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            violations.Add($"A team named '{name}' already exists.");
        }

        var known = new HashSet<string>(knownUserNames, StringComparer.OrdinalIgnoreCase);
        foreach (var userName in MemberUserNames.Where(userName => !known.Contains(userName)))
        {
            violations.Add($"No employee with user name '{userName}' exists.");
        }

        return violations;
    }
}

/// <summary>
/// Result of <see cref="CreateTeamCommand"/>. When <see cref="Succeeded"/> is false nothing was written and
/// <see cref="Message"/> lists the rules that were broken.
/// </summary>
public record CreateTeamResult(bool Succeeded, string Message, Guid? Id);
