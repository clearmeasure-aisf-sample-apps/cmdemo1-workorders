using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components.Teams;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Teams;

[TestFixture]
public class TeamCardsTests
{
    [Test]
    public async Task ShouldSayThereAreNoTeams_WhenEmpty()
    {
        await using var ctx = new BunitContext();

        var cards = ctx.Render<TeamCards>(parameters => parameters.Add(p => p.Teams, []));

        cards.Find($"[data-testid='{TeamsAndVendors.Elements.TeamsEmptyState}']").TextContent.Trim().ShouldBe("No teams found.");
    }

    [Test]
    public async Task ShouldLinkEachMemberToTheirOpenWork_WhenATeamHasMembers()
    {
        await using var ctx = new BunitContext();
        var team = TeamsMother.MaintenanceTeamWith(new Employee("ned", "Ned", "Flanders", "ned@example.com"));

        var cards = ctx.Render<TeamCards>(parameters => parameters.Add(p => p.Teams, [team]));

        cards.Find($"[data-testid='{TeamsAndVendors.Elements.TeamMemberLink}ned']").GetAttribute("href")
            .ShouldBe(TeamWorkLoadTable.OpenWorkSearch("ned"));
        cards.Find($"[data-testid='{TeamsAndVendors.Elements.TeamMemberCount}{team.Id}']").TextContent.Trim().ShouldBe("1 member");
    }
}
