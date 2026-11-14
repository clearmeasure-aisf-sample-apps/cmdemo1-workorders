using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components.Teams;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Teams;

[TestFixture]
public class VendorCardsTests
{
    [Test]
    public async Task ShouldSayThereAreNoVendors_WhenEmpty()
    {
        await using var ctx = new BunitContext();

        var cards = ctx.Render<VendorCards>(parameters => parameters.Add(p => p.Vendors, []));

        cards.Find($"[data-testid='{TeamsAndVendors.Elements.VendorsEmptyState}']").TextContent.Trim().ShouldBe("No vendors found.");
    }

    [Test]
    public async Task ShouldMarkTheVendorInactive_WhenItIsNotActive()
    {
        await using var ctx = new BunitContext();
        var vendor = TeamsMother.InactiveVendor();

        var cards = ctx.Render<VendorCards>(parameters => parameters.Add(p => p.Vendors, [vendor]));

        var status = cards.Find($"[data-testid='{TeamsAndVendors.Elements.VendorStatus}{vendor.Id}']");
        status.TextContent.ShouldBe("Inactive");
        status.ClassList.ShouldContain("inactive");
    }

    [Test]
    public async Task ShouldLinkThePhoneAsTel_WhenVendorHasAPhoneNumber()
    {
        await using var ctx = new BunitContext();
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Acme Plumbing",
            PhoneNumber = "(512) 555-0100"
        };

        var cards = ctx.Render<VendorCards>(parameters => parameters.Add(p => p.Vendors, [vendor]));

        var link = cards.Find($"[data-testid='{TeamsAndVendors.Elements.VendorPhone}{vendor.Id}'] a");
        link.GetAttribute("href").ShouldBe("tel:5125550100");
        link.TextContent.ShouldBe("(512) 555-0100");
    }
}
