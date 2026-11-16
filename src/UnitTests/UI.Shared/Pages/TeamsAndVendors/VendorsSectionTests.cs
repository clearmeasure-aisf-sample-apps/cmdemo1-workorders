using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Shouldly;
using TeamsAndVendorsPage = ClearMeasure.Bootcamp.UI.Shared.Pages.TeamsAndVendors;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.TeamsAndVendors;

[TestFixture]
public class VendorsSectionTests
{
    [Test]
    public async Task TeamsAndVendors_VendorsSection_RenderWhenVendorsExist()
    {
        var vendor = TeamsAndVendorsSteps.ActiveVendor();
        var bus = new TeamsAndVendorsBus { VendorsToReturn = [vendor] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
            component.Markup.ShouldContain("Joe's HVAC"));
    }

    [Test]
    public async Task TeamsAndVendors_VendorsSection_ShowsEmptyStateWhenNoVendors()
    {
        var bus = new TeamsAndVendorsBus { VendorsToReturn = [] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
            component.Markup.ShouldContain("No vendors found"));
    }

    [Test]
    public async Task TeamsAndVendors_VendorCard_DisplaysActiveVendorWithStatus()
    {
        var vendor = TeamsAndVendorsSteps.ActiveVendor("Active Vendor");
        var bus = new TeamsAndVendorsBus { VendorsToReturn = [vendor] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var vendorCard = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.VendorCard}{vendor.Id}']");
            vendorCard.TextContent.ShouldContain("Active Vendor");
            vendorCard.TextContent.ShouldContain("Active");
        });
    }

    [Test]
    public async Task TeamsAndVendors_VendorCard_DisplaysInactiveVendorWithStatus()
    {
        var vendor = TeamsAndVendorsSteps.InactiveVendor();
        var bus = new TeamsAndVendorsBus { VendorsToReturn = [vendor] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var vendorCard = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.VendorCard}{vendor.Id}']");
            vendorCard.TextContent.ShouldContain("Inactive Vendor");
            vendorCard.TextContent.ShouldContain("Inactive");
        });
    }

    [Test]
    public async Task TeamsAndVendors_VendorCard_DisplaysAllDetails()
    {
        var vendor = TeamsAndVendorsSteps.DetailedHvacVendor();
        var bus = new TeamsAndVendorsBus { VendorsToReturn = [vendor] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() => AssertVendorDetails(component, vendor.Id));
    }

    [Test]
    public async Task TeamsAndVendors_VendorEmail_IsMailtoLink()
    {
        var vendor = TeamsAndVendorsSteps.VendorWithEmail();
        var bus = new TeamsAndVendorsBus { VendorsToReturn = [vendor] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var emailLink = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.VendorEmail}{vendor.Id}'] a");
            emailLink.ShouldNotBeNull();
            emailLink.GetAttribute("href").ShouldBe("mailto:test@vendor.com");
        });
    }

    [Test]
    public async Task TeamsAndVendors_Vendors_InactiveAfterActive()
    {
        var activeVendor = TeamsAndVendorsSteps.ActiveVendor("Active Vendor");
        var inactiveVendor = TeamsAndVendorsSteps.InactiveVendor();
        var bus = new TeamsAndVendorsBus { VendorsToReturn = [activeVendor, inactiveVendor] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var vendorCards = component.FindAll("[class*='vendor-card']");
            vendorCards.Count.ShouldBe(2);
            var firstCardName = vendorCards[0].QuerySelector("[class*='vendor-name']")?.TextContent.Trim();
            firstCardName.ShouldBe("Active Vendor");
        });
    }

    [Test]
    public async Task WhenVendorManagementDisabled_ShouldHideVendorsAndKeepTeams()
    {
        var team = TeamsAndVendorsSteps.MaintenanceTeamWithMember();
        var vendor = TeamsAndVendorsSteps.HiddenVendor();
        var bus = new TeamsAndVendorsBus
        {
            TeamsToReturn = [team],
            VendorsToReturn = [vendor]
        };
        var page = TeamsAndVendorsPageContext.Create()
            .WithBus(bus)
            .WithFeatureFlags(new StubFeatureFlags(vendorManagement: false));
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() => AssertVendorsHiddenTeamsKept(component, team.Id));
        bus.VendorsQueriesSent.ShouldBe(0);
    }

    private static void AssertVendorDetails(
        IRenderedComponent<CascadingAuthenticationState> component,
        Guid vendorId)
    {
        var vendorCard = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.VendorCard}{vendorId}']");
        vendorCard.TextContent.ShouldContain("Joe's HVAC");
        vendorCard.TextContent.ShouldContain("HVAC");
        vendorCard.TextContent.ShouldContain("Joe Smith");
        vendorCard.TextContent.ShouldContain("joe@hvac.com");
        vendorCard.TextContent.ShouldContain("555-1234");
    }

    private static void AssertVendorsHiddenTeamsKept(
        IRenderedComponent<CascadingAuthenticationState> component,
        Guid teamId)
    {
        component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.TeamCard}{teamId}']");
        component.Markup.ShouldContain("Maintenance");
        component.Markup.ShouldNotContain("Hidden Vendor");
        component.Markup.ShouldNotContain("No vendors found");
        component.FindAll(".vendors-section").Count.ShouldBe(0);
        component.FindAll($"[data-testid='{TeamsAndVendorsPage.Elements.VendorsEmptyState}']").Count.ShouldBe(0);
    }
}
