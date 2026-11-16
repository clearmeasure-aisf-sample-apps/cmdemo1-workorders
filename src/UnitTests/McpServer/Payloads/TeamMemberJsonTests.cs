using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

[TestFixture]
public class TeamMemberJsonTests
{
    [Test]
    public void Serialize_Should_WriteUserNameAndNames_InsideATeam()
    {
        TeamJson[] teams = [new(Guid.Empty, "Facilities", null, [new TeamMemberJson("hsimpson", "Homer", "Simpson")])];

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(teams, McpJsonContext.Default.TeamJsonArray));

        var member = json.RootElement[0].GetProperty("Members")[0];
        member.GetProperty("UserName").GetString().ShouldBe("hsimpson");
        member.GetProperty("LastName").GetString().ShouldBe("Simpson");
    }
}
