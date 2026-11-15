using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components.Teams;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Teams;

[TestFixture]
public class VendorCardsEmptyStateTests
{
    [Test]
    public async Task ShouldAnnounceEmptyVendorsWithRoleStatus()
    {
        await using var ctx = new BunitContext();

        var cards = ctx.Render<VendorCards>(parameters => parameters.Add(p => p.Vendors, []));

        cards.Find($"[data-testid='{TeamsAndVendors.Elements.VendorsEmptyState}']").GetAttribute("role").ShouldBe("status");
    }
}
