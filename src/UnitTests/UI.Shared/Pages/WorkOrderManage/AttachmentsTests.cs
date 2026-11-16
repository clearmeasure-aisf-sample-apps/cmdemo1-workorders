using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class AttachmentsTests
{
    private static readonly Employee Creator = EmployeeBuilder.Create("jpalermo")
        .WithFirstName("Jeffrey").WithLastName("Palermo").WithEmail("jp@example.com")
        .WithId(Guid.NewGuid()).Build();

    private static readonly Employee Assignee = WorkOrderManageSteps.Lovejoy();

    [Test]
    public async Task WorkOrderManage_ShouldRenderAttachmentsSection()
    {
        var workOrderId = Guid.NewGuid();
        var page = AttachmentsPage(workOrderId, Creator,
            WorkOrderManageSteps.Attachment(workOrderId, "damage-photo.jpg", Creator));
        await using var ctx = page.Build();

        var component = await RenderWithAttachmentsAsync(page, ctx);

        component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentFileName))
            .TextContent.ShouldBe("damage-photo.jpg");
    }

    [Test]
    public async Task WorkOrderManage_ShouldShowRemoveButton_WhenUserIsCreator()
    {
        var workOrderId = Guid.NewGuid();
        var page = AttachmentsPage(workOrderId, Creator,
            WorkOrderManageSteps.Attachment(workOrderId, "damage-photo.jpg", Creator));
        await using var ctx = page.Build();
        var component = await RenderWithAttachmentsAsync(page, ctx);

        var button = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove));
        button.GetAttribute("type").ShouldBe("button");
        button.GetAttribute("aria-label").ShouldBe("Remove attachment damage-photo.jpg");
    }

    [Test]
    public async Task WorkOrderManage_ShouldShowRemoveButton_WhenUserIsAssignee()
    {
        var workOrderId = Guid.NewGuid();
        var page = AttachmentsPage(workOrderId, Assignee,
            WorkOrderManageSteps.Attachment(workOrderId, "damage-photo.jpg", Creator),
            WorkOrderStatus.InProgress, Creator, Assignee);
        await using var ctx = page.Build();
        var component = await RenderWithAttachmentsAsync(page, ctx);

        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove)).Count
            .ShouldBe(1);
    }

    [Test]
    public async Task WorkOrderManage_ShouldHideRemoveButton_WhenUserIsNotCreatorOrAssignee()
    {
        var workOrderId = Guid.NewGuid();
        var stranger = EmployeeBuilder.Create("hsimpson")
            .WithFirstName("Homer").WithLastName("Simpson").WithEmail("hs@example.com")
            .WithId(Guid.NewGuid()).Build();
        var page = AttachmentsPage(workOrderId, stranger,
            WorkOrderManageSteps.Attachment(workOrderId, "damage-photo.jpg", Creator),
            WorkOrderStatus.Assigned, Creator, Assignee);
        await using var ctx = page.Build();
        var component = await RenderWithAttachmentsAsync(page, ctx);

        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRow)).Count.ShouldBe(1);
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove)).ShouldBeEmpty();
    }

    [TestCase("Complete")]
    [TestCase("Cancelled")]
    public async Task WorkOrderManage_ShouldHideRemoveButton_WhenWorkOrderIsClosed(string statusKey)
    {
        var workOrderId = Guid.NewGuid();
        var page = AttachmentsPage(workOrderId, Creator,
            WorkOrderManageSteps.Attachment(workOrderId, "damage-photo.jpg", Creator),
            WorkOrderStatus.FromKey(statusKey));
        await using var ctx = page.Build();
        var component = await RenderWithAttachmentsAsync(page, ctx);

        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove)).ShouldBeEmpty();
    }

    [Test]
    public async Task WorkOrderManage_ShouldAskForConfirmation_WhenRemoveClicked()
    {
        var workOrderId = Guid.NewGuid();
        var page = AttachmentsPage(workOrderId, Creator,
            WorkOrderManageSteps.Attachment(workOrderId, "damage-photo.jpg", Creator));
        await using var ctx = page.Build();
        var component = await RenderWithAttachmentsAsync(page, ctx);

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove)).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
            component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveConfirm))
                .Count.ShouldBe(1));
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveCancel)).Count
            .ShouldBe(1);
        component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRow)).TextContent
            .ShouldContain("Remove damage-photo.jpg?");
        page.Bus.LastRemoveCommand.ShouldBeNull();
    }

    [Test]
    public async Task WorkOrderManage_ShouldNameConfirmAndCancel_WithAttachmentFileName()
    {
        const string fileName = "damage-photo.jpg";
        var workOrderId = Guid.NewGuid();
        var page = AttachmentsPage(workOrderId, Creator,
            WorkOrderManageSteps.Attachment(workOrderId, fileName, Creator));
        await using var ctx = page.Build();
        var component = await RenderWithAttachmentsAsync(page, ctx);

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove)).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveConfirm))
                .GetAttribute("aria-label").ShouldBe($"Confirm remove attachment {fileName}");
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveCancel))
                .GetAttribute("aria-label").ShouldBe($"Cancel remove attachment {fileName}");
        });
    }

    [Test]
    public async Task WorkOrderManage_ShouldKeepAttachment_WhenRemoveCancelled()
    {
        var workOrderId = Guid.NewGuid();
        var page = AttachmentsPage(workOrderId, Creator,
            WorkOrderManageSteps.Attachment(workOrderId, "damage-photo.jpg", Creator));
        await using var ctx = page.Build();
        var component = await RenderWithAttachmentsAsync(page, ctx);
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove)).ClickAsync(new());

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveCancel))
            .ClickAsync(new());

        await AssertKeepAfterCancel(component, page);
    }

    [Test]
    public async Task WorkOrderManage_ShouldRemoveAttachment_WhenConfirmed()
    {
        var workOrderId = Guid.NewGuid();
        var first = WorkOrderManageSteps.Attachment(workOrderId, "damage-photo.jpg", Creator);
        var page = AttachmentsPage(workOrderId, Creator, first,
            extra: WorkOrderManageSteps.Attachment(workOrderId, "invoice.pdf", Creator));
        await using var ctx = page.Build();
        var component = await RenderWithAttachmentsAsync(page, ctx);
        await component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove))[0]
            .ClickAsync(new());

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveConfirm))
            .ClickAsync(new());

        await AssertRemovedFirst(component, page, workOrderId, first);
    }

    [Test]
    public async Task WorkOrderManage_ShouldHideSection_WhenLastAttachmentRemoved()
    {
        var workOrderId = Guid.NewGuid();
        var page = AttachmentsPage(workOrderId, Creator,
            WorkOrderManageSteps.Attachment(workOrderId, "damage-photo.jpg", Creator));
        await using var ctx = page.Build();
        var component = await RenderWithAttachmentsAsync(page, ctx);
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove)).ClickAsync(new());

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveConfirm))
            .ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
            component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentsSection))
                .ShouldBeEmpty());
    }

    [Test]
    public async Task WorkOrderManage_ShouldShowError_WhenRemoveFails()
    {
        const string error = "Only the creator or the assignee can remove attachments.";
        var workOrderId = Guid.NewGuid();
        var page = AttachmentsPage(workOrderId, Creator,
            WorkOrderManageSteps.Attachment(workOrderId, "damage-photo.jpg", Creator));
        page.WithRemoveResult(RemoveAttachmentResult.Failure(error));
        await using var ctx = page.Build();
        var component = await RenderWithAttachmentsAsync(page, ctx);
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove)).ClickAsync(new());

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveConfirm))
            .ClickAsync(new());

        await AssertRemoveError(component, error);
    }

    private static async Task AssertKeepAfterCancel(
        IRenderedComponent<WorkOrderManagePage> component, WorkOrderManagePageContext page)
    {
        await component.WaitForAssertionAsync(() =>
            component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove))
                .Count.ShouldBe(1));
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveConfirm))
            .ShouldBeEmpty();
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRow)).Count.ShouldBe(1);
        page.Bus.LastRemoveCommand.ShouldBeNull();
    }

    private static async Task AssertRemovedFirst(
        IRenderedComponent<WorkOrderManagePage> component,
        WorkOrderManagePageContext page,
        Guid workOrderId,
        WorkOrderAttachment first)
    {
        await component.WaitForAssertionAsync(() =>
            component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRow))
                .Count.ShouldBe(1));
        component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentFileName))
            .TextContent.ShouldBe("invoice.pdf");
        page.Bus.LastRemoveCommand.ShouldBe(new RemoveAttachmentCommand(workOrderId, first.Id, Creator.UserName));
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveError))
            .ShouldBeEmpty();
    }

    private static async Task AssertRemoveError(IRenderedComponent<WorkOrderManagePage> component, string error)
    {
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemoveError))
                .TextContent.ShouldBe(error));
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRow)).Count.ShouldBe(1);
        component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentRemove)).Count.ShouldBe(1);
    }

    private static WorkOrderManagePageContext AttachmentsPage(
        Guid workOrderId,
        Employee loggedInAs,
        WorkOrderAttachment attachment,
        WorkOrderStatus? status = null,
        Employee? creatorOverride = null,
        Employee? assignee = null,
        WorkOrderAttachment? extra = null)
    {
        var attachments = extra == null ? new[] { attachment } : new[] { attachment, extra };
        return WorkOrderManagePageContext.Create()
            .SignedInAs(loggedInAs)
            .WhenBusReceives<RemoveAttachmentCommand>()
            .WithAttachments(attachments)
            .WithNewOrder(o => o
                .WithId(workOrderId)
                .WithNumber("WO-TEST")
                .WithTitle("Test Order")
                .WithStatus(status ?? WorkOrderStatus.Draft)
                .WithCreator(creatorOverride ?? loggedInAs)
                .WithAssignee(assignee));
    }

    private static async Task<IRenderedComponent<WorkOrderManagePage>> RenderWithAttachmentsAsync(
        WorkOrderManagePageContext page, BunitContext ctx)
    {
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.AttachmentsSection))
                .Count.ShouldBe(1));
        return component;
    }
}
