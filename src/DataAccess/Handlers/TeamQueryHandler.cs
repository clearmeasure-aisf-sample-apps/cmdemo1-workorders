using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Reads and creates teams.
/// </summary>
public class TeamQueryHandler(DataContext context)
    : IRequestHandler<TeamsQuery, Team[]>,
        IRequestHandler<CreateTeamCommand, CreateTeamResult>
{
    /// <summary>
    /// Returns every team with its members, ordered by name ignoring case. Sorting happens in memory so
    /// SQLite and SQL Server give the same order.
    /// </summary>
    public async Task<Team[]> Handle(TeamsQuery request, CancellationToken cancellationToken = default)
    {
        var teams = await context.Set<Team>()
            .Include(t => t.Members)
            .ToListAsync(cancellationToken);

        return teams.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// Creates the team when its name is unique and every member user name matches an employee, both
    /// ignoring case. Otherwise returns the violations and writes nothing.
    /// </summary>
    public async Task<CreateTeamResult> Handle(CreateTeamCommand request,
        CancellationToken cancellationToken = default)
    {
        var existingNames = await context.Set<Team>()
            .Select(t => t.Name)
            .ToListAsync(cancellationToken);
        var employees = await context.Set<Employee>().ToListAsync(cancellationToken);

        var violations = request.FindRuleViolations(existingNames, employees.Select(e => e.UserName));
        if (violations.Count > 0)
        {
            return new CreateTeamResult(false, string.Join(" ", violations), null);
        }

        var team = new Team(request.Name.Trim(), request.Description);
        var requested = new HashSet<string>(request.MemberUserNames, StringComparer.OrdinalIgnoreCase);
        foreach (var employee in employees.Where(e => requested.Contains(e.UserName)))
        {
            team.AddMember(employee);
        }

        context.Add(team);
        await context.SaveChangesAsync(cancellationToken);

        return new CreateTeamResult(true, $"Team '{team.Name}' created.", team.Id);
    }
}
