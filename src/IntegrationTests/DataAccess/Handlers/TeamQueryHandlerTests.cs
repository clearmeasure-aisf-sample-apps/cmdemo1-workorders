using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class TeamQueryHandlerTests
{
    [SetUp]
    public async Task SetUp()
    {
        new DatabaseTests().Clean();

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(new Employee("u1", "Una", "One", "u1@example.com"));
        context.Add(new Employee("u2", "Dos", "Two", "u2@example.com"));
        context.Add(new Employee("u3", "Tres", "Three", "u3@example.com"));
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task ShouldCreateTeamWithMembersAndReadBackThroughTeamsQuery()
    {
        var result = await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateTeamCommand("Grounds", "Lawn and snow", ["u1", "U2"]));

        result.Succeeded.ShouldBeTrue(result.Message);
        result.Id.ShouldNotBeNull();
        var teams = await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>()).Handle(new TeamsQuery());
        var team = teams.ShouldHaveSingleItem();
        team.Id.ShouldBe(result.Id.Value);
        team.Name.ShouldBe("Grounds");
        team.Description.ShouldBe("Lawn and snow");
        team.MemberCount.ShouldBe(2);
        team.Members.Select(m => m.UserName).ShouldBe(["u1", "u2"], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldReturnTeamsOrderedByName()
    {
        var handler = new TeamQueryHandler(TestHost.GetRequiredService<DataContext>());
        (await handler.Handle(new CreateTeamCommand("kitchen", null, ["u3"]))).Succeeded.ShouldBeTrue();
        (await handler.Handle(new CreateTeamCommand("Grounds", null, []))).Succeeded.ShouldBeTrue();
        (await handler.Handle(new CreateTeamCommand("Altar Guild", null, ["u1"]))).Succeeded.ShouldBeTrue();

        var teams = await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>()).Handle(new TeamsQuery());

        teams.Select(t => t.Name).ShouldBe(["Altar Guild", "Grounds", "kitchen"]);
        teams.Select(t => t.MemberCount).ShouldBe([1, 0, 1]);
    }

    [Test]
    public async Task ShouldFailAndWriteNothing_WhenMemberUnknown()
    {
        var result = await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateTeamCommand("Grounds", null, ["u1", "ghost"]));

        result.Succeeded.ShouldBeFalse();
        result.Id.ShouldBeNull();
        result.Message.ShouldContain("ghost");
        await using var context = TestHost.GetRequiredService<DataContext>();
        (await context.Set<Team>().CountAsync()).ShouldBe(0);
        (await context.Set<Dictionary<string, object>>("TeamMember").CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task ShouldFail_WhenTeamNameDuplicatesIgnoringCase()
    {
        (await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateTeamCommand("Grounds", null, ["u1"]))).Succeeded.ShouldBeTrue();

        var result = await new TeamQueryHandler(TestHost.GetRequiredService<DataContext>())
            .Handle(new CreateTeamCommand("  grounds ", null, ["u2"]));

        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldContain("already exists");
        await using var context = TestHost.GetRequiredService<DataContext>();
        (await context.Set<Team>().CountAsync()).ShouldBe(1);
        (await context.Set<Dictionary<string, object>>("TeamMember").CountAsync()).ShouldBe(1);
    }
}
