using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderAttachmentTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldAddAttachmentMetadataAndDisplayOnManagePage()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        var attachment = new WorkOrderAttachment
        {
            Id = Guid.NewGuid(),
            WorkOrderId = order.Id,
            FileName = "damage-photo.jpg",
            ContentType = "image/jpeg",
            FileSize = 2048,
            UploadedById = CurrentUser.Id,
            UploadedBy = CurrentUser,
            UploadedDate = DateTime.UtcNow
        };

        var command = new AddAttachmentMetadataCommand(
            order, CurrentUser, attachment.FileName, attachment.ContentType, attachment.FileSize);
        await Bus.Send(command);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var attachmentsSection = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentsSection));
        await Expect(attachmentsSection).ToBeVisibleAsync();

        var fileNameCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentFileName));
        await Expect(fileNameCell).ToContainTextAsync("damage-photo.jpg");

        var contentTypeCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentContentType));
        await Expect(contentTypeCell).ToContainTextAsync("image/jpeg");

        var fileSizeCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentFileSize));
        await Expect(fileSizeCell).ToContainTextAsync("2048");

        var uploaderCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentUploadedBy));
        await Expect(uploaderCell).ToContainTextAsync(CurrentUser.GetFullName());
    }

    [Test, Retry(2)]
    public async Task ShouldRemoveAttachmentFromManagePage()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await Bus.Send(new AddAttachmentMetadataCommand(order, CurrentUser, "remove-me.jpg", "image/jpeg", 1024));
        order = await ClickWorkOrderNumberFromSearchPage(order);

        var fileNameCell = Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentFileName));
        await Expect(fileNameCell).ToContainTextAsync("remove-me.jpg");

        await Click(nameof(WorkOrderManage.Elements.AttachmentRemove));
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentRemoveConfirm))).ToBeVisibleAsync();
        await Click(nameof(WorkOrderManage.Elements.AttachmentRemoveConfirm));

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentRow))).ToHaveCountAsync(0);
        Page.Url.ShouldContain("/workorder/manage");
        (await Bus.Send(new WorkOrderAttachmentsQuery(order.Id))).ShouldBeEmpty();
    }

    [Test, Retry(2)]
    public async Task ShouldNotShowRemoveButtonOnCompletedWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await CompleteExistingWorkOrder(order);
        await Bus.Send(new AddAttachmentMetadataCommand(order, CurrentUser, "kept.jpg", "image/jpeg", 1024));
        await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentsSection))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.AttachmentRemove))).ToHaveCountAsync(0);
    }
}
