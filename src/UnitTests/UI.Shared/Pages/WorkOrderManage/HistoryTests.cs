using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class HistoryTests
{
    private static readonly Employee Creator = EmployeeBuilder.Create("jpalermo")
        .WithFirstName("Jeffrey").WithLastName("Palermo").WithEmail("jp@example.com")
        .WithId(Guid.NewGuid()).Build();

    private static readonly Employee Assignee = EmployeeBuilder.Create("hsimpson")
        .WithFirstName("Homer").WithLastName("Simpson").WithEmail("hs@example.com")
        .WithId(Guid.NewGuid()).Build();

    [Test]
    public async Task WorkOrderManage_ShouldRenderEmptyHistoryMessage_WhenNoStatusChanges()
    {
        var workOrder = WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid()).WithNumber("WO-001").WithTitle("Test").WithDescription("Test")
            .WithStatus(WorkOrderStatus.Draft).CreatedBy(Creator).CreatedAt(DateTime.UtcNow).Build();
        var page = HistoryPage(workOrder);
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var historyMessage = component.FindAll(
                WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.StatusHistory));
            historyMessage.Count.ShouldBeGreaterThan(0);
            historyMessage[0].TextContent.ShouldContain("No status changes recorded.");
        });
    }

    [Test]
    public async Task WorkOrderManage_ShouldRenderStatusHistoryTable_WhenChangesExist()
    {
        var workOrderId = Guid.NewGuid();
        var history = new[]
        {
            ToChange(new HistoryChange
            {
                WorkOrderId = workOrderId,
                From = WorkOrderStatus.Draft,
                To = WorkOrderStatus.Assigned,
                ChangedDate = new DateTime(2025, 3, 1, 10, 0, 0)
            })
        };
        var workOrder = WorkOrderBuilder.Create()
            .WithId(workOrderId).WithNumber("WO-001").WithTitle("Test").WithDescription("Test")
            .WithStatus(WorkOrderStatus.Assigned).CreatedBy(Creator).AssignedTo(Assignee)
            .CreatedAt(DateTime.UtcNow).AssignedAt(DateTime.UtcNow).Build();
        var page = HistoryPage(workOrder, history);
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await AssertHistoryRow(component);
    }

    [Test]
    public async Task WorkOrderManage_ShouldRenderShelveReason_WhenHistoryHasReason()
    {
        var workOrderId = Guid.NewGuid();
        var workOrder = WorkOrderBuilder.Create()
            .WithId(workOrderId).WithNumber("WO-001").WithTitle("Test").WithDescription("Test")
            .WithStatus(WorkOrderStatus.Assigned).CreatedBy(Creator).AssignedTo(Assignee)
            .CreatedAt(DateTime.UtcNow).Build();
        var page = HistoryPage(workOrder, ShelveReasonHistory(workOrderId));
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var reasonCells = component.FindAll(
                WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.StatusHistoryReason));
            reasonCells.Count.ShouldBe(1);
            reasonCells[0].TextContent.ShouldContain("Waiting on parts");
        });
    }

    [Test]
    public async Task WorkOrderManage_ShouldDisplayHistoryNewestFirst()
    {
        var workOrderId = Guid.NewGuid();
        var workOrder = WorkOrderBuilder.Create()
            .WithId(workOrderId).WithNumber("WO-001").WithTitle("Test").WithDescription("Test")
            .WithStatus(WorkOrderStatus.InProgress).CreatedBy(Creator).AssignedTo(Assignee)
            .CreatedAt(DateTime.UtcNow).Build();
        var page = HistoryPage(workOrder, NewestFirstHistory(workOrderId));
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var toStatusCells = component.FindAll(
                WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.StatusHistoryToStatus));
            toStatusCells.Count.ShouldBe(2);
            toStatusCells[0].TextContent.ShouldContain("In Progress");
            toStatusCells[1].TextContent.ShouldContain("Assigned");
        });
    }

    private static async Task AssertHistoryRow(IRenderedComponent<WorkOrderManagePage> component)
    {
        await component.WaitForAssertionAsync(() =>
            component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.StatusHistoryRow))
                .Count.ShouldBe(1));
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.StatusHistoryFromStatus))[0]
            .TextContent.ShouldContain("Draft");
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.StatusHistoryToStatus))[0]
            .TextContent.ShouldContain("Assigned");
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.StatusHistoryChangedBy))[0]
            .TextContent.ShouldContain("Homer Simpson");
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.StatusHistoryReason))[0]
            .TextContent.Trim().ShouldBe("");
    }

    private static WorkOrderManagePageContext HistoryPage(
        WorkOrder workOrder, params WorkOrderStatusChange[] history) =>
        WorkOrderManagePageContext.Create()
            .SignedInAs(Creator)
            .Editing(workOrder)
            .WithHistory(history);

    private static WorkOrderStatusChange[] ShelveReasonHistory(Guid workOrderId) =>
    [
        ToChange(new HistoryChange
        {
            WorkOrderId = workOrderId,
            From = WorkOrderStatus.InProgress,
            To = WorkOrderStatus.Assigned,
            ChangedDate = new DateTime(2025, 3, 1, 10, 0, 0),
            Reason = "Waiting on parts"
        })
    ];

    private static WorkOrderStatusChange[] NewestFirstHistory(Guid workOrderId) =>
    [
        ToChange(new HistoryChange
        {
            WorkOrderId = workOrderId,
            From = WorkOrderStatus.Assigned,
            To = WorkOrderStatus.InProgress,
            ChangedDate = new DateTime(2025, 3, 1, 10, 0, 0)
        }),
        ToChange(new HistoryChange
        {
            WorkOrderId = workOrderId,
            From = WorkOrderStatus.Draft,
            To = WorkOrderStatus.Assigned,
            ChangedDate = new DateTime(2025, 2, 15, 10, 0, 0)
        })
    ];

    private sealed class HistoryChange
    {
        public Guid WorkOrderId { get; init; }
        public WorkOrderStatus From { get; init; } = WorkOrderStatus.None;
        public WorkOrderStatus To { get; init; } = WorkOrderStatus.None;
        public DateTime ChangedDate { get; init; }
        public string? Reason { get; init; }
    }

    private static WorkOrderStatusChange ToChange(HistoryChange change) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkOrderId = change.WorkOrderId,
            FromStatus = change.From,
            ToStatus = change.To,
            ChangedById = Assignee.Id,
            ChangedBy = Assignee,
            ChangedDate = change.ChangedDate,
            Reason = change.Reason
        };
}
