using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Teams;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Teams;

[TestFixture]
public class TeamWorkLoadTableEmptyStateTests
{
    [Test]
    public async Task ShouldAnnounceEmptyWorkloadWithRoleStatus()
    {
        await using var ctx = new BunitContext();

        var table = ctx.Render<TeamWorkLoadTable>(parameters => parameters.Add(p => p.Summaries, []));

        var element = table.Find($"[data-testid='{TeamsAndVendors.Elements.WorkloadEmptyState}']");
        element.GetAttribute("role").ShouldBe("status");
        element.TextContent.Trim().ShouldBe("No assignable employees found.");
    }
}
