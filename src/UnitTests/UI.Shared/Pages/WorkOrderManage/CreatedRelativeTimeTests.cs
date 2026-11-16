using Bunit;
using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class CreatedRelativeTimeTests
{
    [Test]
    public async Task WorkOrderManage_ShouldShowRelativeCreatedTime_BesideTheFullDate()
    {
        var now = new DateTimeOffset(2026, 6, 15, 15, 0, 0, TimeSpan.Zero);
        var created = now.UtcDateTime.AddDays(-3);
        var creator = WorkOrderManageSteps.Viewer();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .Editing(SampleWorkOrder(creator, created))
            .WithClock(new WorkOrderManageFixedClock(now));
        await using var ctx = page.Build();

        var component = page.Render(ctx);
        var createdElement = await component.WaitForElementAsync(
            WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.CreatedDate));
        var relativeElement = component.Find(
            WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.CreatedRelativeTime));

        relativeElement.TextContent.Trim().ShouldBe("3 days ago");
        relativeElement.GetAttribute("title").ShouldBe(createdElement.TextContent.Trim());
        createdElement.TextContent.ShouldNotContain("ago");
        createdElement.TextContent.Trim().ShouldBe(created.ToString("G", CultureInfo.CurrentCulture));
    }

    [Test]
    public async Task WorkOrderManage_ShouldOmitRelativeTime_WhenCreatedDateIsNull()
    {
        var clock = new WorkOrderManageFixedClock(new DateTimeOffset(2026, 6, 15, 15, 0, 0, TimeSpan.Zero));
        var creator = WorkOrderManageSteps.Viewer();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(creator)
            .Editing(SampleWorkOrder(creator, null))
            .WithClock(clock);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForElementAsync(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.CreatedDate));
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.CreatedRelativeTime))
            .Count.ShouldBe(0);
    }

    private static WorkOrder SampleWorkOrder(Employee creator, DateTime? created) =>
        WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid())
            .WithNumber("WO-001")
            .WithTitle("Test")
            .WithDescription("Test")
            .WithStatus(WorkOrderStatus.Draft)
            .CreatedBy(creator)
            .CreatedAt(created)
            .Build();
}
