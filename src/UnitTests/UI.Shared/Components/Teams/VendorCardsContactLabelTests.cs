using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components.Teams;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Teams;

[TestFixture]
public class VendorCardsContactLabelTests
{
    [Test]
    public async Task ShouldLabelEmailLinkWithVendorName()
    {
        await using var ctx = new BunitContext();
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Acme Plumbing",
            EmailAddress = "acme@example.com",
            PhoneNumber = "(512) 555-0100"
        };

        var cards = ctx.Render<VendorCards>(parameters => parameters.Add(p => p.Vendors, [vendor]));

        var link = cards.Find($"[data-testid='{TeamsAndVendors.Elements.VendorEmail}{vendor.Id}'] a");
        link.GetAttribute("aria-label").ShouldBe("Email Acme Plumbing: acme@example.com");
    }

    [Test]
    public async Task ShouldLabelPhoneLinkWithVendorName()
    {
        await using var ctx = new BunitContext();
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Acme Plumbing",
            EmailAddress = "acme@example.com",
            PhoneNumber = "(512) 555-0100"
        };

        var cards = ctx.Render<VendorCards>(parameters => parameters.Add(p => p.Vendors, [vendor]));

        var link = cards.Find($"[data-testid='{TeamsAndVendors.Elements.VendorPhone}{vendor.Id}'] a");
        link.GetAttribute("aria-label").ShouldBe("Call Acme Plumbing: (512) 555-0100");
    }
}
