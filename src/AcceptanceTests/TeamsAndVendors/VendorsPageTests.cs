namespace ClearMeasure.Bootcamp.AcceptanceTests.TeamsAndVendors;

[TestFixture]
public class VendorsPageTests : TeamsAndVendorsAcceptanceTestBase
{
    [Test]
    public async Task ShouldDisplayVendorCreatedThroughCommand()
    {
        await LoginAsCurrentUser();
        var vendor = await CreateTaggedVendorAsync(new TaggedVendorSpec
        {
            NameSuffix = "Vendor",
            Trade = $"Trade-{TestTag}",
            ContactName = $"Contact {TestTag}",
            EmailAddress = $"vendor-{TestTag}@example.test",
            PhoneNumber = "555-1234"
        });

        await GotoTeamsPageAsync();
        var card = VendorCard(vendor.Id!.Value);
        await Expect(card).ToBeVisibleAsync();
        await Expect(card).ToContainTextAsync($"Vendor {TestTag}");
        await Expect(card).ToContainTextAsync($"Trade-{TestTag}");
        await Expect(card).ToContainTextAsync($"Contact {TestTag}");
    }

    [Test]
    public async Task ShouldDisplayVendorDetailsCorrectly()
    {
        await LoginAsCurrentUser();
        var vendor = await CreateTaggedVendorAsync(new TaggedVendorSpec
        {
            NameSuffix = "Complete",
            Trade = $"HVAC-{TestTag}",
            ContactName = $"Jane {TestTag}",
            EmailAddress = $"jane-{TestTag}@example.test",
            PhoneNumber = "555-5678"
        });

        await GotoTeamsPageAsync();
        var card = VendorCard(vendor.Id!.Value);
        await Expect(card).ToBeVisibleAsync();
        await Expect(card).ToContainTextAsync($"Complete {TestTag}");
        await Expect(card).ToContainTextAsync($"HVAC-{TestTag}");
        await Expect(card).ToContainTextAsync($"Jane {TestTag}");
        await Expect(card).ToContainTextAsync($"jane-{TestTag}@example.test");
        await Expect(card).ToContainTextAsync("555-5678");
        await Expect(card).ToContainTextAsync("Active");
    }

    [Test]
    public async Task ShouldDisplayInactiveVendorWithInactiveStatus()
    {
        await LoginAsCurrentUser();
        var vendor = await CreateTaggedVendorAsync(new TaggedVendorSpec
        {
            NameSuffix = "Inactive",
            Trade = $"Elec-{TestTag}",
            ContactName = $"Bob {TestTag}",
            EmailAddress = $"bob-{TestTag}@example.test",
            PhoneNumber = "555-9999",
            IsActive = false
        });

        await GotoTeamsPageAsync();
        var card = VendorCard(vendor.Id!.Value);
        await Expect(card).ToBeVisibleAsync();
        await Expect(card).ToContainTextAsync("Inactive");
    }

    [Test]
    public async Task ShouldDisplayVendorEmailAsMailtoLink()
    {
        await LoginAsCurrentUser();
        var email = $"mail-{TestTag}@example.test";
        var vendor = await CreateTaggedVendorAsync(new TaggedVendorSpec
        {
            NameSuffix = "Email",
            EmailAddress = email
        });

        await GotoTeamsPageAsync();
        var emailLink = Page.Locator($"[data-testid*='VendorEmail'][data-testid*='{vendor.Id}'] a");
        await Expect(emailLink).ToBeVisibleAsync();
        await Expect(emailLink).ToHaveAttributeAsync("href", $"mailto:{email}");
    }
}
