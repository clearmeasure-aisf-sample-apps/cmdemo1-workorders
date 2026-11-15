using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>
/// #9118 contracts: Cancel on an existing Assigned row must clear assignee (the live defect);
/// Save→Assign Update path already wrote ASD correctly and must not regress under load-and-apply.
/// </summary>
public class StateCommandHandlerSaveThenAssignPersistenceTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldClearAssigneeAndDate_WhenCancelOnExistingAssignedRowAfterRemoting()
    {
        var number = TestData.Number("P");
        var creator = EmployeeBuilder.Create(TestData.UserName("tl")).Build();
        var attachmentId = await GivenAssignedWithAttachmentAsync(number, creator);

        await StateCommandHandlerSteps.HandleRemotedAsync(
            new AssignedToCancelledCommand(await StateCommandHandlerSteps.LoadByNumberAsync(number), creator));

        await ThenCancelledPreservingAttachmentAsync(number, attachmentId);
    }

    [Test]
    public async Task ShouldPersistAssignedStatusAndDate_WhenSaveDraftThenRemotedAssign_OnExistingId()
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("tl")).Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("gw")).Build();
        await StateCommandHandlerSteps.PersistAsync(creator, assignee);
        var number = TestData.Number("P");
        var draft = WorkOrderBuilder.Create().WithId(Guid.Empty).WithNumber(number)
            .WithTitle("mow front grass").WithDescription("edge the walk")
            .InRoom("front lawn").CreatedBy(creator).AssignedTo(assignee).Build();
        draft.Instructions = "do a good job";

        var saveResult = await StateCommandHandlerSteps.HandleRemotedAsync(new SaveDraftCommand(draft, creator));
        var assignResult = await WhenAssignExistingDraftAsync(saveResult.WorkOrder.Id, creator);

        await ThenAssignedPersistedAsync(number, creator.UserName, assignee.UserName, assignResult);
    }

    [Test]
    public async Task ShouldKeepAddPathAssignedDateAndStatusWhenIdEmpty()
    {
        var currentUser = EmployeeBuilder.Create(TestData.UserName("add_cu")).Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("add_as")).Build();
        var order = WorkOrderBuilder.Create().WithId(Guid.Empty).WithNumber(TestData.Number("P"))
            .CreatedBy(currentUser).AssignedTo(assignee).Build();
        await StateCommandHandlerSteps.PersistAsync(currentUser, assignee);

        var result = await StateCommandHandlerSteps.HandleRemotedAsync(
            new DraftToAssignedCommand(order, currentUser));

        await ThenAddPathAssignedAsync(result.WorkOrder.Id, currentUser, assignee);
    }

    [Test]
    public async Task ShouldLeaveNearbyInProgressUnchangedWhenAssigningDifferentDraft()
    {
        var inProgressNumber = TestData.Number("P");
        var draftNumber = TestData.Number("P");
        var creator = EmployeeBuilder.Create(TestData.UserName("near_cu")).Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("near_as")).Build();
        await GivenNearbyOrdersAsync(creator, assignee, inProgressNumber, draftNumber);

        await StateCommandHandlerSteps.HandleRemotedAsync(
            new DraftToAssignedCommand(await StateCommandHandlerSteps.LoadByNumberAsync(draftNumber), creator));

        await ThenNearbyUnchangedAndDraftAssignedAsync(inProgressNumber, draftNumber, assignee.UserName);
    }

    private static async Task<Guid> GivenAssignedWithAttachmentAsync(string number, Employee creator)
    {
        var assignee = EmployeeBuilder.Create(TestData.UserName("gw")).Build();
        var order = WorkOrderBuilder.Create().WithId(Guid.Empty).WithNumber(number)
            .CreatedBy(creator).Assigned(assignee).AssignedAt(TestHost.TestTime.DateTime).Build();
        await StateCommandHandlerSteps.PersistAsync(creator, assignee, order);
        var attachmentId = Guid.NewGuid();
        await PersistAttachmentAsync(attachmentId, order.Id, creator.Id);
        return attachmentId;
    }

    private static async Task PersistAttachmentAsync(Guid attachmentId, Guid workOrderId, Guid uploadedById)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(new WorkOrderAttachment
        {
            Id = attachmentId,
            WorkOrderId = workOrderId,
            FileName = "before-cancel.txt",
            ContentType = "text/plain",
            FileSize = 12,
            UploadedById = uploadedById,
            UploadedDate = TestHost.TestTime.DateTime
        });
        await context.SaveChangesAsync();
    }

    private static async Task<StateCommandResult> WhenAssignExistingDraftAsync(Guid existingId, Employee creator)
    {
        var orderForAssign = await StateCommandHandlerSteps.LoadByIdAsync(existingId);
        orderForAssign.Id.ShouldNotBe(Guid.Empty);
        orderForAssign.Status.ShouldBe(WorkOrderStatus.Draft);
        orderForAssign.Assignee.ShouldNotBeNull();
        return await StateCommandHandlerSteps.HandleRemotedAsync(
            new DraftToAssignedCommand(orderForAssign, creator));
    }

    private static async Task GivenNearbyOrdersAsync(
        Employee creator, Employee assignee, string inProgressNumber, string draftNumber)
    {
        var inProgress = WorkOrderBuilder.Create().WithId(Guid.Empty).WithNumber(inProgressNumber)
            .WithTitle("nearby in progress").CreatedBy(creator).AssignedTo(assignee)
            .WithStatus(WorkOrderStatus.InProgress).AssignedAt(TestHost.TestTime.DateTime).Build();
        var draft = WorkOrderBuilder.Create().WithId(Guid.Empty).WithNumber(draftNumber)
            .WithTitle("draft to assign").CreatedBy(creator).AssignedTo(assignee).Build();
        await StateCommandHandlerSteps.PersistAsync(creator, assignee, inProgress, draft);
    }

    private static async Task ThenCancelledPreservingAttachmentAsync(string number, Guid attachmentId)
    {
        await using var fresh = TestHost.GetRequiredService<DbContext>();
        var reloaded = await fresh.Set<WorkOrder>().AsNoTracking().SingleAsync(w => w.Number == number);
        reloaded.Status.ShouldBe(WorkOrderStatus.Cancelled);
        reloaded.Status.Code.ShouldBe("CNL");
        reloaded.AssignedDate.ShouldBeNull();
        reloaded.Assignee.ShouldBeNull();
        (await fresh.Set<WorkOrderAttachment>().AnyAsync(a => a.Id == attachmentId)).ShouldBeTrue();
    }

    private static async Task ThenAssignedPersistedAsync(
        string number, string creatorUserName, string assigneeUserName, StateCommandResult assignResult)
    {
        assignResult.WorkOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        assignResult.WorkOrder.AssignedDate.ShouldNotBeNull();
        ThenLawnFieldsAssigned(await StateCommandHandlerSteps.LoadByNumberAsync(number), number,
            creatorUserName, assigneeUserName);
        await ThenBusAndToolsAgreeAsync(number, assigneeUserName);
    }

    private static void ThenLawnFieldsAssigned(
        WorkOrder reloaded, string number, string creatorUserName, string assigneeUserName)
    {
        reloaded.Status.ShouldBe(WorkOrderStatus.Assigned);
        reloaded.Status.Code.ShouldBe(WorkOrderStatus.Assigned.Code);
        reloaded.Status.ShouldNotBe(WorkOrderStatus.Cancelled);
        reloaded.AssignedDate.ShouldBe(TestHost.TestTime.DateTime);
        reloaded.Assignee!.UserName.ShouldBe(assigneeUserName);
        reloaded.Creator!.UserName.ShouldBe(creatorUserName);
        reloaded.Number.ShouldBe(number);
        reloaded.Title.ShouldBe("mow front grass");
        reloaded.Description.ShouldBe("edge the walk");
        reloaded.Instructions.ShouldBe("do a good job");
        reloaded.RoomNumber.ShouldBe("front lawn");
    }

    private static async Task ThenBusAndToolsAgreeAsync(string number, string assigneeUserName)
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var byNumber = await bus.Send(new WorkOrderByNumberQuery(number));
        byNumber.ShouldNotBeNull();
        byNumber.Status.ShouldBe(WorkOrderStatus.Assigned);
        byNumber.AssignedDate.ShouldNotBeNull();
        byNumber.Assignee!.UserName.ShouldBe(assigneeUserName);
        using var doc = JsonDocument.Parse(await WorkOrderTools.GetWorkOrder(bus, number));
        var detail = doc.RootElement;
        detail.GetProperty("Status").GetString().ShouldBe("Assigned");
        detail.GetProperty("AssignedDate").ValueKind.ShouldNotBe(JsonValueKind.Null);
        detail.GetProperty("AssigneeUsername").GetString().ShouldBe(assigneeUserName);
    }

    private static async Task ThenAddPathAssignedAsync(Guid id, Employee currentUser, Employee assignee)
    {
        var order = await StateCommandHandlerSteps.LoadByIdAsync(id);
        order.Status.ShouldBe(WorkOrderStatus.Assigned);
        order.AssignedDate.ShouldBe(TestHost.TestTime.DateTime);
        order.Assignee.ShouldBe(assignee);
        order.Creator.ShouldBe(currentUser);
    }

    private static async Task ThenNearbyUnchangedAndDraftAssignedAsync(
        string inProgressNumber, string draftNumber, string assigneeUserName)
    {
        await using var fresh = TestHost.GetRequiredService<DbContext>();
        var frozen = await fresh.Set<WorkOrder>().AsNoTracking()
            .SingleAsync(w => w.Number == inProgressNumber);
        frozen.Status.ShouldBe(WorkOrderStatus.InProgress);
        frozen.Title.ShouldBe("nearby in progress");
        frozen.Assignee!.UserName.ShouldBe(assigneeUserName);
        var assigned = await fresh.Set<WorkOrder>().AsNoTracking()
            .SingleAsync(w => w.Number == draftNumber);
        assigned.Status.ShouldBe(WorkOrderStatus.Assigned);
        assigned.AssignedDate.ShouldNotBeNull();
    }
}
