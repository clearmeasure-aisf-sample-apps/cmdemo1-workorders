using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.IntegrationTests.TestSupport;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpTeamToolTests
{
    [Test]
    public async Task ShouldListAllTeams()
    {
        var emp1 = new Employee(TestData.UserName("jdoe"), "John", "Doe", "john@test.com");
        var emp2 = new Employee(TestData.UserName("jsmith"), "Jane", "Smith", "jane@test.com");
        var tag = TestData.Tag();
        var team1 = new Team($"Maintenance-{tag}", "Handles building maintenance");
        var team2 = new Team($"Kitchen-{tag}", "Prepares meals");
        team1.AddMember(emp1);
        team2.AddMember(emp2);

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(emp1);
        context.Add(emp2);
        context.Add(team1);
        context.Add(team2);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await TeamTools.ListTeams(bus);

        result.ShouldContain(team1.Name);
        result.ShouldContain(team2.Name);
        result.ShouldContain(team1.Description!);
        result.ShouldContain(emp1.FirstName);
        result.ShouldContain(emp2.FirstName);
    }

    [Test]
    public async Task ShouldReturnJsonArray()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await TeamTools.ListTeams(bus);

        using var json = JsonDocument.Parse(result);
        json.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Test]
    public async Task ShouldCreateTeamWithMembers()
    {
        var emp1 = new Employee(TestData.UserName("jdoe"), "John", "Doe", "john@test.com");
        var emp2 = new Employee(TestData.UserName("jsmith"), "Jane", "Smith", "jane@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(emp1);
        context.Add(emp2);
        await context.SaveChangesAsync();

        var teamName = $"Engineering-{TestData.Tag()}";
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await TeamTools.CreateTeam(bus, teamName, "Software and IT", [emp1.UserName, emp2.UserName]);

        result.ShouldContain("\"succeeded\": true");
        result.ShouldContain(teamName);

        // Verify the team was created
        var listResult = await TeamTools.ListTeams(bus);
        listResult.ShouldContain(teamName);
        listResult.ShouldContain(emp1.UserName);
        listResult.ShouldContain(emp2.UserName);
    }

    [Test]
    public async Task ShouldReturnErrorOnDuplicateTeamName()
    {
        var teamName = $"Maintenance-{TestData.Tag()}";
        var team = new Team(teamName, "Original team");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(team);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await TeamTools.CreateTeam(bus, teamName, "Another team");

        result.ShouldContain("already exists");
        result.ShouldNotContain("\"succeeded\": true");
    }

    [Test]
    public async Task ShouldReturnErrorOnDuplicateTeamNameIgnoringCase()
    {
        var teamName = $"Maintenance-{TestData.Tag()}";
        var team = new Team(teamName, "Original team");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(team);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await TeamTools.CreateTeam(bus, teamName.ToUpperInvariant(), "Another team");

        result.ShouldContain("already exists");
        result.ShouldNotContain("\"succeeded\": true");
    }

    [Test]
    public async Task ShouldReturnErrorOnUnknownMember()
    {
        var teamName = $"NewTeam-{TestData.Tag()}";
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await TeamTools.CreateTeam(bus, teamName, null, ["nonexistent_user"]);

        result.ShouldContain("No employee");
        result.ShouldNotContain("\"succeeded\":true");
    }
}
