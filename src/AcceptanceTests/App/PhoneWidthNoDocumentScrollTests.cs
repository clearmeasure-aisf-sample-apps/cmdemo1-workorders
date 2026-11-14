using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.UI.Shared;
using System.Collections;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// At 390px the document matches the viewport on every route, split per route for parallel workers.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class PhoneWidthNoDocumentScrollTests : AppAcceptanceTestBase
{
    /// <summary>/login stays anonymous; other routes sign in in the case.</summary>
    protected override bool UseStorageStateSignIn => false;

    /// <summary>One case per 390px route key (including dark re-checks).</summary>
    public static IEnumerable RouteKeys()
    {
        yield return "login";
        yield return "home";
        yield return "search";
        yield return "search-board";
        yield return "manage-new";
        yield return "print";
        yield return "reports";
        yield return "buildings";
        yield return "teams";
        yield return "ai-agent";
        yield return "settings";
        yield return "counter";
        yield return "fetchdata";
        yield return "not-found";
        yield return "client-health";
        yield return "client-health-detailed";
        yield return "search-dark";
        yield return "search-board-dark";
        yield return "reports-dark";
    }

    [TestCaseSource(nameof(RouteKeys))]
    [Retry(2)]
    public async Task ShouldMatchDocumentWidthToViewport_At390_OnRoute(string routeKey)
    {
        var route = PhoneWidthRoutes.ByKey(routeKey);
        await Page.SetViewportSizeAsync(390, 844);
        await SignInIfNeededAsync(route);
        var url = await ResolveUrlAsync(route);
        await ApplyDarkThemeIfNeededAsync(route);
        await Page.GotoAsync(url);
        await WaitReadyAsync(route);
        await AssertDocumentFitsAsync(url);
        await AssertRouteExtrasAsync(route);
    }

    private async Task SignInIfNeededAsync(PhoneWidthRoute route)
    {
        if (!route.RequiresSignIn)
        {
            return;
        }

        await Page.GotoAsync("/");
        await LoginAsCurrentUser();
    }

    private async Task ApplyDarkThemeIfNeededAsync(PhoneWidthRoute route)
    {
        if (route.UseDarkTheme)
        {
            await EnableDarkThemeAsync();
        }
    }

    private async Task AssertRouteExtrasAsync(PhoneWidthRoute route)
    {
        if (route.InnerScrollSelector is not null)
        {
            await AssertScrollsInsideAsync(route.InnerScrollSelector);
        }

        if (route.AssertTopBarWraps)
        {
            await AssertTopBarWrapsAsync();
        }
    }

    private async Task<string> ResolveUrlAsync(PhoneWidthRoute route)
    {
        if (route.Key.StartsWith("search", StringComparison.Ordinal))
        {
            // Own Medium row only — peer High-priority / long titles must not appear in this view.
            await Mother.WorkOrderAsync(CurrentUser.UserName, WorkOrderStatus.Draft, new WorkOrderSeedFields
            {
                Title = $"[{TestTag}] phone width",
                RoomNumber = "1",
                Priority = WorkOrderPriority.Medium
            });
            return route.UrlTemplate.Contains('?', StringComparison.Ordinal)
                ? $"{route.UrlTemplate}&Q={TestTag}"
                : $"{route.UrlTemplate}?Q={TestTag}";
        }

        if (!route.NeedsWorkOrderNumber)
        {
            return route.UrlTemplate;
        }

        var workOrder = await Mother.DraftWorkOrderAsync(
            CurrentUser.UserName,
            title: $"[{TestTag}] Replace hallway light fixtures near the fellowship hall entrance",
            roomNumber: "Fellowship Hall 2");
        workOrder.Number.ShouldNotBeNullOrWhiteSpace();
        return route.UrlTemplate.Replace("{number}", Uri.EscapeDataString(workOrder.Number), StringComparison.Ordinal);
    }

    private async Task EnableDarkThemeAsync()
    {
        await Mother.WorkOrderAsync(CurrentUser.UserName, WorkOrderStatus.Draft, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] phone dark",
            Priority = WorkOrderPriority.Medium
        });
        await Page.GotoAsync($"/workorder/search?Q={TestTag}");
        await Expect(Page.Locator(".table-scroll")).ToBeVisibleAsync();
        await Page.GetByTestId(nameof(MainLayout.Elements.DarkModeToggle)).ClickAsync();
        await Page.WaitForFunctionAsync("() => document.documentElement.getAttribute('data-theme') === 'dark'");
    }

    private async Task WaitReadyAsync(PhoneWidthRoute route)
    {
        switch (route.WaitKind)
        {
            case PhoneWidthRoute.ReadyKind.TestId:
                await Expect(Page.GetByTestId(route.ReadyToken)).ToBeVisibleAsync();
                break;
            case PhoneWidthRoute.ReadyKind.Css:
                await Expect(Page.Locator(route.ReadyToken)).ToBeVisibleAsync();
                break;
            case PhoneWidthRoute.ReadyKind.Heading:
                await WaitForHeadingRouteAsync(route);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(route), route.WaitKind, "Unknown ready kind.");
        }
    }

    private async Task WaitForHeadingRouteAsync(PhoneWidthRoute route)
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = route.ReadyToken })).ToBeVisibleAsync();
        if (route.Key == "buildings")
        {
            await Expect(Page.Locator(".buildings-loading")).ToHaveCountAsync(0);
        }

        if (route.Key == "teams")
        {
            await Expect(Page.Locator(".teams-loading")).ToHaveCountAsync(0);
            await Expect(Page.Locator(".teams-table")).ToBeVisibleAsync();
        }
    }

    private async Task AssertDocumentFitsAsync(string url)
    {
        // Theme/CSS paint can briefly expand scrollWidth; wait for a stable fit before measuring.
        await Page.WaitForFunctionAsync(
            "() => document.documentElement.scrollWidth === window.innerWidth");
        var measured = await Page.EvaluateAsync<string>(
            """
            () => {
                let widest = '';
                let right = 0;
                for (const el of document.querySelectorAll('body *')) {
                    const rect = el.getBoundingClientRect();
                    if (rect.width < 1 || rect.height < 1 || rect.right <= right) continue;
                    right = rect.right;
                    const testId = el.getAttribute('data-testid') || '';
                    const className = typeof el.className === 'string' ? el.className.split(' ')[0] : '';
                    widest = el.tagName + (className ? '.' + className : '') + (testId ? '#' + testId : '') + '@' + Math.round(rect.right);
                }
                return document.documentElement.scrollWidth + ':' + window.innerWidth + ':' + widest;
            }
            """);
        var parts = measured.Split(':');
        parts[0].ShouldBe(parts[1], $"{url} widest={parts[2]}");
    }

    private async Task AssertScrollsInsideAsync(string selector)
    {
        var scrollsInside = await Page.Locator(selector).EvaluateAsync<bool>(
            "element => element.scrollWidth > element.clientWidth");
        scrollsInside.ShouldBeTrue();
    }

    private async Task AssertTopBarWrapsAsync()
    {
        var wraps = await Page.Locator(".header-content").EvaluateAsync<bool>(
            "element => getComputedStyle(element).flexWrap === 'wrap'");
        wraps.ShouldBeTrue();
        var inside = await Page.EvaluateAsync<bool>(
            """
            () => {
                const limit = window.innerWidth + 1;
                return [...document.querySelectorAll('.header-content > *')]
                    .every(child => child.getBoundingClientRect().right <= limit);
            }
            """);
        inside.ShouldBeTrue();
    }
}
