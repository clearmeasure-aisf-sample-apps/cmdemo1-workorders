using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>
/// The database is shared with other tests, so each test uses its own tag in team names and its own
/// employees, and asserts only on those rows.
/// </summary>
[TestFixture]
public class TeamQueryHandlerTests
{
    private string _tag = "";
    private Employee[] _employees = [];

    [SetUp]
    public async Task SetUp()
    {
        _tag = TestData.Tag();
        _employees =
        [
            NewEmployee("u1", "Una", "One"),
            NewEmployee("u2", "Dos", "Two"),
            NewEmployee("u3", "Tres", "Three")
        ];

        await using var context = TestHost.GetRequiredService<DbContext>();
        foreach (var employee in _employees)
        {
            context.Add(employee);
        }

        await context.SaveChangesAsync();
    }

    [Test]
    public async Task ShouldCreateTeamWithMembersAndReadBackThroughTeamsQuery()
    {
        var name = TeamName("Grounds");
        var result = await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateTeamCommand(name, "Lawn and snow",
                [_employees[0].UserName, _employees[1].UserName.ToUpperInvariant()]));

        result.Succeeded.ShouldBeTrue(result.Message);
        result.Id.ShouldNotBeNull();
        var team = (await OwnTeams()).ShouldHaveSingleItem();
        team.Id.ShouldBe(result.Id.Value);
        team.Name.ShouldBe(name);
        team.Description.ShouldBe("Lawn and snow");
        team.MemberCount.ShouldBe(2);
        team.Members.Select(m => m.UserName)
            .ShouldBe([_employees[0].UserName, _employees[1].UserName], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldReturnTeamsOrderedByName()
    {
        var handler = new TeamQueryHandler(TestHost.GetRequiredService<DataContext>());
        (await handler.Handle(new CreateTeamCommand(TeamName("kitchen"), null, [_employees[2].UserName])))
            .Succeeded.ShouldBeTrue();
        (await handler.Handle(new CreateTeamCommand(TeamName("Grounds"), null, []))).Succeeded.ShouldBeTrue();
        (await handler.Handle(new CreateTeamCommand(TeamName("Altar Guild"), null, [_employees[0].UserName])))
            .Succeeded.ShouldBeTrue();

        var teams = await OwnTeams();

        teams.Select(t => t.Name).ShouldBe([TeamName("Altar Guild"), TeamName("Grounds"), TeamName("kitchen")]);
        teams.Select(t => t.MemberCount).ShouldBe([1, 0, 1]);
    }

    [Test]
    public async Task ShouldFailAndWriteNothing_WhenMemberUnknown()
    {
        var unknown = TestData.UserName("ghost");
        var result = await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateTeamCommand(TeamName("Grounds"), null, [_employees[0].UserName, unknown]));

        result.Succeeded.ShouldBeFalse();
        result.Id.ShouldBeNull();
        result.Message.ShouldContain(unknown);
        (await OwnTeams()).ShouldBeEmpty();
        (await OwnMembershipCount()).ShouldBe(0);
    }

    [Test]
    public async Task ShouldFail_WhenTeamNameDuplicatesIgnoringCase()
    {
        var name = TeamName("Grounds");
        (await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateTeamCommand(name, null, [_employees[0].UserName]))).Succeeded.ShouldBeTrue();

        var result = await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateTeamCommand($"  {name.ToLowerInvariant()} ", null, [_employees[1].UserName]));

        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldContain("already exists");
        (await OwnTeams()).Length.ShouldBe(1);
        (await OwnMembershipCount()).ShouldBe(1);
    }

    private string TeamName(string name) => $"{name} {_tag}";

    private static Employee NewEmployee(string prefix, string firstName, string lastName)
    {
        var userName = TestData.UserName(prefix);
        return new Employee(userName, firstName, lastName, TestData.Email(userName));
    }

    private async Task<Team[]> OwnTeams()
    {
        var teams = await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>()).Handle(new TeamsQuery());
        return teams.Where(t => t.Name.EndsWith(_tag, StringComparison.Ordinal)).ToArray();
    }

    private async Task<int> OwnMembershipCount()
    {
        var ids = _employees.Select(e => e.Id).ToList();
        await using var context = TestHost.GetRequiredService<DataContext>();
        return await context.Set<Dictionary<string, object>>("TeamMember")
            .CountAsync(m => ids.Contains(EF.Property<Guid>(m, "EmployeeId")));
    }
}
