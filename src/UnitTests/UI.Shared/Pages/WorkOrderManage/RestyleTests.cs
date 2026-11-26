using System.Text.RegularExpressions;
using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class RestyleTests
{
    private static readonly Employee Creator = EmployeeBuilder.Create("jpalermo")
        .WithFirstName("Jeffrey").WithLastName("Palermo").WithEmail("jp@example.com")
        .WithId(Guid.NewGuid()).Build();

    [Test]
    public async Task ShouldRenderPageHeaderIconsAndExistingTestIds()
    {
        var page = NewRestylePage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() => AssertHeaderIcons(component));
    }

    [Test]
    public async Task ShouldRenderSearchStatusPill_WhenAssigned()
    {
        var order = WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid()).WithNumber("WO-AS").WithTitle("Assigned").WithDescription("Assigned")
            .WithStatus(WorkOrderStatus.Assigned).CreatedBy(Creator).CreatedAt(DateTime.UtcNow).Build();
        var page = WorkOrderManagePageContext.Create().SignedInAs(Creator).Editing(order);
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var expected = StatusPillPresenter.Present(order, false);
            var badge = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Status));
            badge.GetAttribute("class").ShouldBe(expected.CssClass);
            badge.TextContent.ShouldBe(expected.Text);
        });
    }

    [Test]
    public async Task ShouldRenderOverdueStatusPill_WhenDueDateIsPast()
    {
        var clock = FixedUtcClock.At(new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero));
        var order = WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid()).WithNumber("WO-OD").WithTitle("Overdue").WithDescription("Overdue")
            .WithStatus(WorkOrderStatus.Assigned).CreatedBy(Creator).CreatedAt(DateTime.UtcNow)
            .DueOn(new DateOnly(2026, 6, 14)).Build();
        var page = WorkOrderManagePageContext.Create().SignedInAs(Creator).Editing(order).WithClock(clock);
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var expected = StatusPillPresenter.Present(order, true);
            var badge = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Status));
            badge.GetAttribute("class").ShouldBe(expected.CssClass);
            badge.TextContent.ShouldBe("Overdue");
            component.FindAll(".visually-hidden")
                .Select(element => element.TextContent.Trim())
                .ShouldContain(expected.HiddenText!);
        });
    }

    [Test]
    public void ShouldKeepFormStylesheetFreeOfHexColours()
    {
        var path = Path.GetFullPath(Path.Combine(
            NUnit.Framework.TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI.Shared", "Pages", "WorkOrderManage.razor.css"));
        File.Exists(path).ShouldBeTrue($"Expected scoped stylesheet at {path}");
        var css = File.ReadAllText(path);

        Regex.IsMatch(css, "#[0-9a-fA-F]{3,8}").ShouldBeFalse();
        css.ShouldNotContain("rgba(");
        css.ShouldNotContain("hsl(");
        AssertCssTokens(css);
    }

    private static void AssertHeaderIcons(IRenderedComponent<WorkOrderManagePage> component)
    {
        component.FindComponent<PageHeader>().ShouldNotBeNull();
        var heading = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.ManageHeadingTooltip));
        heading.TagName.ShouldBe("H1", StringCompareShould.IgnoreCase);
        heading.GetAttribute("title").ShouldBe("Create or edit a work order");
        component.Find("h1 [data-testid='" + WorkOrderManagePage.Elements.WorkOrderNumber + "']").TextContent
            .ShouldBe("WO-TIP");
        component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.RequiredFieldsNote)).TextContent
            .Trim().ShouldBe("Fields marked with * are required.");
        AssertSpeakDictateLabels(component);
    }

    private static void AssertSpeakDictateLabels(IRenderedComponent<WorkOrderManagePage> component)
    {
        var speak = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.SpeakTitle));
        speak.GetAttribute("aria-label").ShouldBe("Speak title");
        component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.SpeakTitle) + " .bi-volume-up")
            .ShouldNotBeNull();
        var dictate = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle));
        dictate.GetAttribute("aria-label").ShouldBe("Dictate title");
        component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle) + " .bi-mic")
            .ShouldNotBeNull();
        component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.SpeakDescription))
            .GetAttribute("aria-label").ShouldBe("Speak description");
        component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateDescription))
            .GetAttribute("aria-label").ShouldBe("Dictate description");
    }

    private static void AssertCssTokens(string css)
    {
        css.ShouldContain("rgb(254, 240, 138)");
        css.ShouldContain("rgb(254, 202, 202)");
        css.ShouldContain("var(--control-height)");
        css.ShouldContain(".token-card");
        css.ShouldContain(".btn-app--primary");
        css.ShouldContain(".status-badge--overdue");
        css.ShouldContain(".status-badge--waiting");
        css.ShouldContain(".due-date-field ::deep .due-date-today");
        css.ShouldContain(".due-date-field ::deep .due-date-overdue");
    }

    private static WorkOrderManagePageContext NewRestylePage() =>
        WorkOrderManagePageContext.Create()
            .SignedInAs(Creator)
            .WithNewOrder(o => o.WithId(Guid.NewGuid()).WithNumber("WO-TIP").WithTitle("Heading tooltip"));
}
