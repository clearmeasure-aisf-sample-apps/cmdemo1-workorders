using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

[TestFixture]
public class TeamJsonTests
{
    [Test]
    public void From_Should_ListEachMemberByUserName()
    {
        var team = new Team("Facilities", "Building upkeep");
        team.AddMember(EmployeeBuilder.Create("hsimpson").WithFirstName("Homer").WithLastName("Simpson").Build());

        var json = TeamJson.From(team);

        json.Name.ShouldBe("Facilities");
        json.Description.ShouldBe("Building upkeep");
        json.Members.ShouldBe([new TeamMemberJson("hsimpson", "Homer", "Simpson")]);
    }
}
