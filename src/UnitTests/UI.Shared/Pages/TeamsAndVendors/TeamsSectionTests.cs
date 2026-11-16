using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.AspNetCore.Components.Authorization;
using Shouldly;
using TeamsAndVendorsPage = ClearMeasure.Bootcamp.UI.Shared.Pages.TeamsAndVendors;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.TeamsAndVendors;

[TestFixture]
public class TeamsSectionTests
{
    [Test]
    public async Task TeamsAndVendors_TeamsSection_RenderWhenTeamsExist()
    {
        var team = TeamsAndVendorsSteps.MaintenanceTeamWithMember();
        var bus = new TeamsAndVendorsBus { TeamsToReturn = [team] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            component.Markup.ShouldContain("Maintenance");
            component.Markup.ShouldContain("Maintenance team");
        });
    }

    [Test]
    public async Task TeamsAndVendors_TeamsSection_ShowsEmptyStateWhenNoTeams()
    {
        var bus = new TeamsAndVendorsBus { TeamsToReturn = [] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
            component.Markup.ShouldContain("No teams found"));
    }

    [Test]
    public async Task TeamsAndVendors_TeamCard_DisplaysTeamNameDescription()
    {
        var team = new Team("Kitchen", "Kitchen operations") { Id = Guid.NewGuid() };
        var bus = new TeamsAndVendorsBus { TeamsToReturn = [team] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var teamCard = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.TeamCard}{team.Id}']");
            teamCard.TextContent.ShouldContain("Kitchen");
            teamCard.TextContent.ShouldContain("Kitchen operations");
        });
    }

    [Test]
    public async Task TeamsAndVendors_TeamCard_DisplaysMemberCount()
    {
        var emp1 = TeamsAndVendorsSteps.HomerFulfiller();
        var team = TeamsAndVendorsSteps.TeamWithMember("Test Team", null, emp1);
        var bus = new TeamsAndVendorsBus { TeamsToReturn = [team] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var memberCountElement = component.Find(
                $"[data-testid='{TeamsAndVendorsPage.Elements.TeamMemberCount}{team.Id}']");
            memberCountElement.TextContent.ShouldContain("1 member");
        });
    }

    [Test]
    public async Task TeamsAndVendors_TeamMembers_LinksToAssigneeFilteredSearch()
    {
        var emp1 = TeamsAndVendorsSteps.HomerFulfiller();
        var team = TeamsAndVendorsSteps.TeamWithMember("Test Team", null, emp1);
        var bus = new TeamsAndVendorsBus { TeamsToReturn = [team] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() => AssertMemberSearchLink(component, emp1.UserName));
    }

    private static void AssertMemberSearchLink(
        IRenderedComponent<CascadingAuthenticationState> component,
        string userName)
    {
        var memberLink = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.TeamMemberLink}{userName}']");
        memberLink.ShouldNotBeNull();
        var href = memberLink.GetAttribute("href");
        href.ShouldNotBeNullOrEmpty();
        href.ShouldContain("workorder/search");
        href.ShouldContain("Assignee=hsimpson");
        href.ShouldContain("OpenOnly=true");
    }
}
