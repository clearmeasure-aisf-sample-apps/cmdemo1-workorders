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
    /// Returns the rules this command breaks given the data already stored: a team name that matches an
    /// existing one ignoring case, and each member user name that matches no employee ignoring case.
    /// An empty list means the team can be created.
    /// </summary>
    public IReadOnlyList<string> FindRuleViolations(IEnumerable<string> existingTeamNames,
        IEnumerable<string> knownUserNames)
    {
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
