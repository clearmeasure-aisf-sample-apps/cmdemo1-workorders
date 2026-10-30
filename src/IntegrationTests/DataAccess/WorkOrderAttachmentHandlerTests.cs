using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderAttachmentHandlerTests : IntegratedTestBase
{
    [Test]
    public async Task AddAttachmentMetadataCommand_ShouldPersistAttachment()
    {
        var uploader = new Employee(TestData.UserName("uploader"), "Attachment", "Uploader", "uploader@example.com");
        var workOrder = new WorkOrder { Number = TestData.Number("A"), Creator = uploader };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(uploader);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var command = new AddAttachmentMetadataCommand(workOrder, uploader, "damage-photo.jpg", "image/jpeg", 4096);
        var attachment = await bus.Send(command);

        attachment.ShouldNotBeNull();
        attachment.FileName.ShouldBe("damage-photo.jpg");
        attachment.ContentType.ShouldBe("image/jpeg");
        attachment.FileSize.ShouldBe(4096L);
        attachment.WorkOrderId.ShouldBe(workOrder.Id);
        attachment.UploadedById.ShouldBe(uploader.Id);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var persisted = context.Set<WorkOrderAttachment>().SingleOrDefault(a => a.Id == attachment.Id);
            persisted.ShouldNotBeNull();
            persisted.FileName.ShouldBe("damage-photo.jpg");
        }
    }

    [Test]
    public async Task WorkOrderAttachmentsQuery_ShouldReturnAttachmentsForWorkOrder()
    {
        var uploader = new Employee(TestData.UserName("uploader"), "Attachment", "Uploader", "uploader@example.com");
        var workOrder = new WorkOrder { Number = TestData.Number("A"), Creator = uploader };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(uploader);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        await bus.Send(new AddAttachmentMetadataCommand(workOrder, uploader, "photo1.jpg", "image/jpeg", 1024));
        await bus.Send(new AddAttachmentMetadataCommand(workOrder, uploader, "invoice.pdf", "application/pdf", 2048));

        var attachments = await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id));

        attachments.Length.ShouldBe(2);
        attachments.Any(a => a.FileName == "photo1.jpg").ShouldBeTrue();
        attachments.Any(a => a.FileName == "invoice.pdf").ShouldBeTrue();
    }

    [Test]
    public async Task WorkOrderAttachmentsQuery_ShouldReturnEmptyForWorkOrderWithNoAttachments()
    {
        var creator = new Employee(TestData.UserName("uploader"), "Attachment", "Uploader", "uploader@example.com");
        var workOrder = new WorkOrder { Number = TestData.Number("A"), Creator = creator };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(workOrder);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var attachments = await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id));

        attachments.ShouldBeEmpty();
    }

    [Test]
    public async Task RemoveAttachmentCommand_ShouldDeleteAttachment()
    {
        var creator = NewEmployee("creator");
        var workOrder = await SeedWorkOrder(creator, WorkOrderStatus.Draft);
        var bus = TestHost.GetRequiredService<IBus>();
        var attachment = await bus.Send(
            new AddAttachmentMetadataCommand(workOrder, creator, "photo.jpg", "image/jpeg", 1024));

        var result = await bus.Send(new RemoveAttachmentCommand(workOrder.Id, attachment.Id, creator.UserName));

        result.Succeeded.ShouldBeTrue();
        result.Error.ShouldBeNull();
        (await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id))).ShouldBeEmpty();
    }

    [Test]
    public async Task RemoveAttachmentCommand_ByAssignee_ShouldDeleteAttachment()
    {
        var creator = NewEmployee("creator");
        var assignee = NewEmployee("assignee");
        var workOrder = await SeedWorkOrder(creator, WorkOrderStatus.InProgress, assignee);
        var bus = TestHost.GetRequiredService<IBus>();
        var attachment = await bus.Send(
            new AddAttachmentMetadataCommand(workOrder, creator, "photo.jpg", "image/jpeg", 1024));

        var result = await bus.Send(new RemoveAttachmentCommand(workOrder.Id, attachment.Id, assignee.UserName));

        result.Succeeded.ShouldBeTrue();
        (await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id))).ShouldBeEmpty();
    }

    [Test]
    public async Task RemoveAttachmentCommand_ShouldLeaveOtherAttachmentsUntouched()
    {
        var creator = NewEmployee("creator");
        var workOrder = await SeedWorkOrder(creator, WorkOrderStatus.Assigned);
        var bus = TestHost.GetRequiredService<IBus>();
        var first = await bus.Send(new AddAttachmentMetadataCommand(workOrder, creator, "a.jpg", "image/jpeg", 100));
        var middle = await bus.Send(new AddAttachmentMetadataCommand(workOrder, creator, "b.pdf", "application/pdf", 200));
        var last = await bus.Send(new AddAttachmentMetadataCommand(workOrder, creator, "c.txt", "text/plain", 300));

        var result = await bus.Send(new RemoveAttachmentCommand(workOrder.Id, middle.Id, creator.UserName));

        result.Succeeded.ShouldBeTrue();
        var remaining = await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id));
        remaining.Length.ShouldBe(2);
        ShouldMatch(remaining.Single(a => a.Id == first.Id), first);
        ShouldMatch(remaining.Single(a => a.Id == last.Id), last);
    }

    [Test]
    public async Task RemoveAttachmentCommand_WrongWorkOrder_ShouldBeRejected()
    {
        var creator = NewEmployee("creator");
        var workOrderA = await SeedWorkOrder(creator, WorkOrderStatus.Draft);
        var workOrderB = await SeedWorkOrder(creator, WorkOrderStatus.Draft, saveCreator: false);
        var bus = TestHost.GetRequiredService<IBus>();
        await bus.Send(new AddAttachmentMetadataCommand(workOrderA, creator, "a.jpg", "image/jpeg", 100));
        var attachmentB = await bus.Send(new AddAttachmentMetadataCommand(workOrderB, creator, "b.jpg", "image/jpeg", 100));

        var result = await bus.Send(new RemoveAttachmentCommand(workOrderA.Id, attachmentB.Id, creator.UserName));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Attachment does not belong to this work order.");
        (await bus.Send(new WorkOrderAttachmentsQuery(workOrderA.Id))).Length.ShouldBe(1);
        (await bus.Send(new WorkOrderAttachmentsQuery(workOrderB.Id))).Length.ShouldBe(1);
    }

    [Test]
    public async Task RemoveAttachmentCommand_MissingAttachment_ShouldBeRejected()
    {
        var creator = NewEmployee("creator");
        var workOrder = await SeedWorkOrder(creator, WorkOrderStatus.Draft);
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await bus.Send(new RemoveAttachmentCommand(workOrder.Id, Guid.NewGuid(), creator.UserName));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Attachment not found.");
    }

    [Test]
    public async Task RemoveAttachmentCommand_NonOwner_ShouldBeRejected()
    {
        var creator = NewEmployee("creator");
        var stranger = NewEmployee("stranger");
        var workOrder = await SeedWorkOrder(creator, WorkOrderStatus.Draft);
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(stranger);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var attachment = await bus.Send(
            new AddAttachmentMetadataCommand(workOrder, creator, "photo.jpg", "image/jpeg", 1024));

        var result = await bus.Send(new RemoveAttachmentCommand(workOrder.Id, attachment.Id, stranger.UserName));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Only the creator or the assignee can remove attachments.");
        (await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id))).Length.ShouldBe(1);
    }

    [Test]
    public async Task RemoveAttachmentCommand_CompleteWorkOrder_ShouldBeRejected()
    {
        var creator = NewEmployee("creator");
        var workOrder = await SeedWorkOrder(creator, WorkOrderStatus.Complete);
        var bus = TestHost.GetRequiredService<IBus>();
        var attachment = await bus.Send(
            new AddAttachmentMetadataCommand(workOrder, creator, "photo.jpg", "image/jpeg", 1024));

        var result = await bus.Send(new RemoveAttachmentCommand(workOrder.Id, attachment.Id, creator.UserName));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Attachments cannot be removed from a Complete or Cancelled work order.");
        (await bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id))).Length.ShouldBe(1);
    }

    private static Employee NewEmployee(string prefix) =>
        new(TestData.UserName(prefix), "Attachment", prefix, $"{prefix}@example.com");

    private static async Task<WorkOrder> SeedWorkOrder(Employee creator, WorkOrderStatus status,
        Employee? assignee = null, bool saveCreator = true)
    {
        var workOrder = new WorkOrder
        {
            Number = TestData.Number("A"),
            Creator = creator,
            Assignee = assignee,
            Status = status
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        if (saveCreator) context.Add(creator);
        else context.Attach(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();
        return workOrder;
    }

    private static void ShouldMatch(WorkOrderAttachment actual, WorkOrderAttachment expected)
    {
        actual.FileName.ShouldBe(expected.FileName);
        actual.ContentType.ShouldBe(expected.ContentType);
        actual.FileSize.ShouldBe(expected.FileSize);
        actual.UploadedById.ShouldBe(expected.UploadedById);
        actual.UploadedDate.ShouldBe(expected.UploadedDate, TimeSpan.FromMilliseconds(1));
    }
}
