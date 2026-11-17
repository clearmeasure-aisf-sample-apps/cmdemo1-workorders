using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Reads and creates teams.
/// </summary>
public class TeamQueryHandler(DataContext context)
    : IRequestHandler<TeamsQuery, Team[]>,
        IRequestHandler<CreateTeamCommand, CreateTeamResult>
{
    // A field, not the primary-constructor parameter, so EF 11 can precompile the queries for Native AOT.
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private readonly DataContext _context = context;

    /// <summary>
    /// Returns every team with its members, ordered by name ignoring case. Sorting happens in memory so
    /// SQLite and SQL Server give the same order.
    /// </summary>
    public async ValueTask<Team[]> Handle(TeamsQuery request, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        var teams = await _context.Set<Team>()
            .Include(t => t.Members)
            .ToListAsync(ct);

        return teams.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// Creates the team when its name is unique and every member user name matches an employee, both
    /// ignoring case. Otherwise returns the violations and writes nothing.
    /// </summary>
    public async ValueTask<CreateTeamResult> Handle(CreateTeamCommand request,
        CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        var existingNames = await _context.Set<Team>()
            .Select(t => t.Name)
            .ToListAsync(ct);
        var employees = await _context.Set<Employee>().ToListAsync(ct);

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

        _context.Add(team);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateTeamResult(true, $"Team '{team.Name}' created.", team.Id);
    }
}
