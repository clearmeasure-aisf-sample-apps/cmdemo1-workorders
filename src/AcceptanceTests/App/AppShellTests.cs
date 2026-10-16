using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>App shell of the #251 redesign: navy sidebar, Inter, responsive rail, user menu, dark tokens.</summary>
[TestFixture]
public class AppShellTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldRenderNavySidebarWithSections()
    {
        await Page.SetViewportSizeAsync(1280, 800);
        var rail = Page.Locator("#app-navigation-rail");

        var background = await rail.EvaluateAsync<string>("e => getComputedStyle(e).backgroundImage");

        background.ShouldContain("rgb(11, 18, 32)");
        background.ShouldContain("rgb(17, 26, 46)");
        await Expect(Page.Locator("h2.nav-section")).ToHaveTextAsync(["Workspace", "My Queue", "Administration"]);
        await Expect(rail.GetByText("Menu", new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId(nameof(MainLayout.Elements.BrandSubtitle))).ToHaveTextAsync("Kalahari Church · Facilities");
    }

    [Test, Retry(2)]
    public async Task ShouldUseInterFont()
    {
        var fontFamily = await Page.EvaluateAsync<string>("() => getComputedStyle(document.body).fontFamily");

        fontFamily.TrimStart('"', '\'').ShouldStartWith("Inter");
    }

    [Test, Retry(2)]
    public async Task ShouldCollapseSidebarToIcons_Below1024()
    {
        await Page.SetViewportSizeAsync(1000, 800);
        var rail = Page.Locator("#app-navigation-rail");

        await Expect(rail).ToBeVisibleAsync();
        var box = await rail.BoundingBoxAsync();
        box!.Width.ShouldBeLessThanOrEqualTo(96);
        var labelWidth = await Page.Locator("#app-navigation-rail .nav-text").First
            .EvaluateAsync<double>("e => e.getBoundingClientRect().width");
        labelWidth.ShouldBeLessThanOrEqualTo(1);
        var workOrders = Page.GetByRole(AriaRole.Link, new() { NameRegex = new System.Text.RegularExpressions.Regex("^Work Orders") });
        await Expect(workOrders).ToBeVisibleAsync();
        await workOrders.ClickAsync();
        await Page.WaitForURLAsync("**/workorder/search");
    }

    [Test, Retry(2)]
    public async Task ShouldFitWithoutHorizontalScroll_At1280()
    {
        // Measure the layout with this test's own realistic rows, not whatever other tests left in the database.
        await using (var context = TestHost.NewDbContext())
        {
            context.Attach(CurrentUser);
            foreach (var status in new[] { WorkOrderStatus.Draft, WorkOrderStatus.Assigned, WorkOrderStatus.InProgress })
            {
                context.Add(new WorkOrder
                {
                    Number = TestData.Number("A"),
                    Creator = CurrentUser,
                    Assignee = CurrentUser,
                    Status = status,
                    Title = $"[{TestTag}] Replace hallway light fixtures near the fellowship hall entrance",
                    Building = "Sanctuary",
                    RoomNumber = "Fellowship Hall 2",
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    CreatedDate = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        await Page.SetViewportSizeAsync(1280, 800);
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?Q={TestTag}");
        await Expect(Page.Locator(".grid-data tbody tr")).ToHaveCountAsync(3);

        var pageFits = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth <= document.documentElement.clientWidth");
        var tableFits = await Page.Locator(".table-scroll").EvaluateAsync<bool>("e => e.scrollWidth <= e.clientWidth");
        var filtersPerRow = await Page.EvaluateAsync<int>(
            "() => new Set([...document.querySelectorAll('.filter-group')].map(g => Math.round(g.getBoundingClientRect().top))).size");
        var cardRows = await Page.EvaluateAsync<int>(
            "() => new Set([...document.querySelectorAll('.summary-card')].map(c => Math.round(c.getBoundingClientRect().top))).size");

        pageFits.ShouldBeTrue();
        tableFits.ShouldBeTrue();
        filtersPerRow.ShouldBe(1);
        cardRows.ShouldBe(1);
    }

    [Test, Retry(2)]
    public async Task ShouldOpenUserMenuWithKeyboard_AndCloseOnEscape()
    {
        var toggle = Page.GetByTestId(nameof(Logout.Elements.UserMenuToggle));
        var logout = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));

        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(logout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(logout).ToBeHiddenAsync();
        await Expect(toggle).ToBeFocusedAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldApplyDarkTokens_WhenThemeToggled()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        var filtersCard = Page.Locator(".filters-card");
        var lightCard = await filtersCard.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
        var lightPage = await Page.EvaluateAsync<string>("() => getComputedStyle(document.body).backgroundColor");
        lightCard.ShouldBe("rgb(255, 255, 255)");
        lightPage.ShouldBe("rgb(245, 247, 251)");

        await Click(nameof(MainLayout.Elements.DarkModeToggle));
        await Page.WaitForFunctionAsync("() => document.documentElement.getAttribute('data-theme') === 'dark'");

        await Expect(filtersCard).Not.ToHaveCSSAsync("background-color", lightCard);
        (await Page.EvaluateAsync<string>("() => getComputedStyle(document.body).backgroundColor")).ShouldNotBe(lightPage);
        var badges = Page.Locator(".grid-data .status-badge");
        if (await badges.CountAsync() > 0)
        {
            (await badges.First.TextContentAsync()).ShouldNotBeNullOrWhiteSpace();
        }

        await Click(nameof(MainLayout.Elements.DarkModeToggle));
        await Page.WaitForFunctionAsync("() => document.documentElement.getAttribute('data-theme') === 'light'");
    }

    [Test, Retry(2)]
    public async Task ShouldCaptureSearchScreenshots_LightAndDark()
    {
        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Expect(Page.Locator(".grid-data")).ToBeVisibleAsync();
        var folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
        Directory.CreateDirectory(folder);

        foreach (var theme in new[] { "light", "dark" })
        {
            await SetThemeAsync(theme);
            foreach (var width in new[] { 1440, 1280, 900 })
            {
                await Page.SetViewportSizeAsync(width, 1000);
                await Page.WaitForTimeoutAsync(300);
                var path = Path.Combine(folder, $"work-order-search-{theme}-{width}.png");
                await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
                TestContext.AddTestAttachment(path);
                File.Exists(path).ShouldBeTrue();
            }
        }

        await SetThemeAsync("light");
    }

    private async Task SetThemeAsync(string theme)
    {
        var current = await Page.EvaluateAsync<string>("() => document.documentElement.getAttribute('data-theme')");
        if (current == theme)
        {
            return;
        }

        await Click(nameof(MainLayout.Elements.DarkModeToggle));
        await Page.WaitForFunctionAsync("t => document.documentElement.getAttribute('data-theme') === t", theme);
    }
}
