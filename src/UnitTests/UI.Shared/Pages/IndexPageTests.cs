using Bunit;
using Bunit.TestDoubles;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using StubUserSession = ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.StubUserSession;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class IndexPageTests
{
    private static BunitContext CreateContext(StubBus bus, Employee? user, bool signedIn = true)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        var auth = ctx.AddAuthorization();
        if (signedIn)
        {
            auth.SetAuthorized("tlovejoy");
        }

        return ctx;
    }

    private static Employee User(bool canCreate)
    {
        var user = new Employee("tlovejoy", "Timothy", "Lovejoy", "tl@example.com");
        user.AddRole(new Role("r", canCreate, false));
        return user;
    }

    private static string TestId(IndexPage.Elements element, string suffix = "") =>
        $"[data-testid='{element}{suffix}']";

    [Test]
    public async Task ShouldDisplayHomeTagline()
    {
        await using var ctx = CreateContext(new StubBus(), User(true));

        var component = ctx.Render<IndexPage>();

        var tagline = component.Find(TestId(IndexPage.Elements.HomeTagline));
        tagline.TextContent.ShouldBe("Track and manage work orders in one place.");
    }

    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnHomeHeading()
    {
        await using var ctx = CreateContext(new StubBus(), User(true));

        var component = ctx.Render<IndexPage>();

        var heading = component.Find(TestId(IndexPage.Elements.HomeHeadingTooltip));
        heading.GetAttribute("title").ShouldBe("Work order home");
        heading.TextContent.ShouldBe("TechWeek Work Orders");
    }

    [Test]
    public async Task ShouldRenderPageHeaderKpiAndStatusCards()
    {
        await using var ctx = CreateContext(new StubBus(), User(true));

        var component = ctx.Render<IndexPage>();

        component.FindComponents<PageHeader>().Count.ShouldBe(1);
        component.FindComponents<WorkOrderSummaryCards>().Count.ShouldBe(1);
        component.FindComponents<KpiCard>().Count.ShouldBe(10);
    }

    [Test]
    public async Task ShouldNotRenderHeroIllustrationOrQuotes()
    {
        await using var ctx = CreateContext(new StubBus(), User(true));

        var component = ctx.Render<IndexPage>();

        component.FindAll(".church-hero, .church-illustration, .lovejoy-quote, .info-card").Count.ShouldBe(0);
        component.FindAll("[data-testid='GreetingBanner']").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldSendSearchSummaryForSignedInUser_AndLinkCards()
    {
        var bus = new StubBus { Summary = new() { AssignedToMeCount = 4, DueThisWeekCount = 5 } };
        await using var ctx = CreateContext(bus, User(true));

        var component = ctx.Render<IndexPage>();

        bus.LastSummaryUserName.ShouldBe("tlovejoy");
        component.Find("[data-testid='SummaryCardAssignedToMe'] .summary-card-value").TextContent.ShouldBe("4");
        component.Find("[data-testid='SummaryCardDueThisWeek'] .summary-card-value").TextContent.ShouldBe("5");
        component.Find("[data-testid='SummaryCardDueThisWeek']").GetAttribute("href")
            .ShouldBe("workorder/search?DueThisWeekOnly=true");
        foreach (var card in Enum.GetValues<SummaryCard>())
        {
            component.Find($"[data-testid='SummaryCard{card}']").GetAttribute("href")
                .ShouldBe(WorkOrderSearchUrlState.ForCard(card).ToRelativeUri());
        }
    }

    [Test]
    public async Task ShouldRenderStatusCards_WithCountsAndSearchLinks()
    {
        var bus = new StubBus { StatusCounts = new Dictionary<string, int> { ["Draft"] = 3, ["Complete"] = 8 } };
        await using var ctx = CreateContext(bus, User(true));

        var component = ctx.Render<IndexPage>();

        component.Find(TestId(IndexPage.Elements.StatusDashboard)).GetAttribute("aria-label")
            .ShouldBe("Work orders by status");
        component.Find(TestId(IndexPage.Elements.StatusCardCount, "Draft")).TextContent.ShouldBe("3");
        component.Find(TestId(IndexPage.Elements.StatusCardCount, "Assigned")).TextContent.ShouldBe("0");
        component.Find(TestId(IndexPage.Elements.StatusCard, "Draft")).GetAttribute("href")
            .ShouldBe("workorder/search?Status=Draft");
        component.Find(TestId(IndexPage.Elements.StatusCard, "Cancelled")).ClassList
            .ShouldContain("summary-card--status-cancelled");
    }

    [Test]
    public async Task ShouldListRecentActivity()
    {
        var completed = new WorkOrder
        {
            Number = "ABC1234", Title = "Fix the organ", Status = WorkOrderStatus.Complete,
            CreatedDate = new DateTime(2026, 10, 1, 12, 0, 0), CompletedDate = new DateTime(2026, 10, 14, 3, 0, 0)
        };
        var created = new WorkOrder
        {
            Number = "DEF5678", Title = "Paint the hall", Status = WorkOrderStatus.Draft,
            CreatedDate = new DateTime(2026, 10, 2, 18, 0, 0)
        };
        var bus = new StubBus { RecentWorkOrders = [completed, created] };
        await using var ctx = CreateContext(bus, User(true));

        var component = ctx.Render<IndexPage>();

        var items = component.FindAll($"{TestId(IndexPage.Elements.RecentActivity)} li");
        items.Count.ShouldBe(2);
        var link = component.Find(TestId(IndexPage.Elements.RecentActivityLink, "ABC1234"));
        link.GetAttribute("href").ShouldBe("/workorder/manage/ABC1234?mode=Edit");
        link.TextContent.ShouldBe("WO-ABC1234");
        var item = component.Find(TestId(IndexPage.Elements.RecentActivityItem, "ABC1234"));
        item.QuerySelector(".activity-title")!.TextContent.ShouldBe("Fix the organ");
        item.QuerySelector(".activity-meta")!.TextContent.ShouldBe("Completed Oct 13, 2026 · Complete");
        component.Find($"{TestId(IndexPage.Elements.RecentActivityItem, "DEF5678")} .activity-meta").TextContent
            .ShouldBe("Created Oct 2, 2026 · Draft");
        component.FindAll(TestId(IndexPage.Elements.RecentActivityEmpty)).Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldShowEmptyRecentActivity()
    {
        await using var ctx = CreateContext(new StubBus(), User(true));

        var component = ctx.Render<IndexPage>();

        component.Find(TestId(IndexPage.Elements.RecentActivityEmpty)).TextContent
            .ShouldBe("No work order activity yet.");
        component.FindAll($"{TestId(IndexPage.Elements.RecentActivity)} li").Count.ShouldBe(0);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldShowNewRequest_OnlyWhenUserCanCreate(bool canCreate)
    {
        await using var ctx = CreateContext(new StubBus(), User(canCreate));

        var component = ctx.Render<IndexPage>();

        component.FindAll($".page-actions {TestId(IndexPage.Elements.QuickLinkNewRequest)}").Count
            .ShouldBe(canCreate ? 1 : 0);
        var workOrders = component.Find(TestId(IndexPage.Elements.QuickLinkWorkOrders));
        workOrders.GetAttribute("href").ShouldBe("workorder/search");
        workOrders.GetAttribute("aria-label").ShouldBe("All Work Orders");
        component.Find(TestId(IndexPage.Elements.QuickLinkMyQueue)).GetAttribute("href")
            .ShouldBe(WorkOrderSearchUrlState.ForCard(SummaryCard.AssignedToMe).ToRelativeUri());
    }

    [Test]
    public async Task ShouldShowSignInPrompt_AndSendNoQuery_WhenAnonymous()
    {
        var bus = new StubBus();
        await using var ctx = CreateContext(bus, null, signedIn: false);

        var component = ctx.Render<IndexPage>();

        component.Find(TestId(IndexPage.Elements.SignInPrompt)).TextContent.ShouldBe("Sign in to see your work orders.");
        bus.SendCallCount.ShouldBe(0);
        component.FindComponents<KpiCard>().Count.ShouldBe(0);
        component.FindAll(TestId(IndexPage.Elements.QuickLinkWorkOrders)).Count.ShouldBe(0);
        component.FindAll(TestId(IndexPage.Elements.StatusDashboard)).Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldReload_OnWorkOrderChangedEvent()
    {
        var bus = new StubBus();
        await using var ctx = CreateContext(bus, User(true));
        var component = ctx.Render<IndexPage>();
        bus.SummaryRequestCount.ShouldBe(1);

        component.Instance.Handle(new WorkOrderChangedEvent(new StateCommandResult(new WorkOrder(), "Save", "saved")));

        await component.WaitForAssertionAsync(() => bus.SummaryRequestCount.ShouldBe(2));
    }
}
