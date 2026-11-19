using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components.Teams;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Teams;

[TestFixture]
public class TeamCardsEmptyStateTests
{
    [Test]
    public async Task ShouldAnnounceEmptyTeamsWithRoleStatus()
    {
        await using var ctx = new BunitContext();

        var cards = ctx.Render<TeamCards>(parameters => parameters.Add(p => p.Teams, []));

        cards.Find($"[data-testid='{TeamsAndVendors.Elements.TeamsEmptyState}']").GetAttribute("role").ShouldBe("status");
    }
}
