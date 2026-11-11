using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class WorkOrderAttachmentListTests
{
    private static readonly WorkOrderAttachment Photo = new() { Id = Guid.NewGuid(), FileName = "boiler.jpg" };

    [Test]
    public async Task ShouldRenderNothing_WhenThereAreNoAttachments()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses(new StubRemoveBus(RemoveAttachmentResult.Success));

        var list = ctx.Render<WorkOrderAttachmentList>(parameters => Parameters(parameters, [], canRemove: true, () => { }));

        list.Markup.Trim().ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldOfferNoRemoveButton_WhenTheUserMayNotRemove()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses(new StubRemoveBus(RemoveAttachmentResult.Success));

        var list = ctx.Render<WorkOrderAttachmentList>(parameters => Parameters(parameters, [Photo], canRemove: false, () => { }));

        list.FindAll($"[data-testid='{nameof(WorkOrderManage.Elements.AttachmentRemove)}']").ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldRemoveAndRaiseTheChange_WhenTheRemovalIsConfirmed()
    {
        var bus = new StubRemoveBus(RemoveAttachmentResult.Success);
        await using var ctx = ComponentContextBuilder.WithStubBuses(bus);
        var changes = 0;
        var list = ctx.Render<WorkOrderAttachmentList>(parameters => Parameters(parameters, [Photo], canRemove: true, () => changes++));

        await list.Find($"[data-testid='{nameof(WorkOrderManage.Elements.AttachmentRemove)}']").ClickAsync(new());
        await list.Find($"[data-testid='{nameof(WorkOrderManage.Elements.AttachmentRemoveConfirm)}']").ClickAsync(new());

        bus.Removed.ShouldBe(new RemoveAttachmentCommand(Guid.Empty, Photo.Id, "jsmith"));
        changes.ShouldBe(1);
    }

    [Test]
    public async Task ShouldShowTheError_WhenTheRemovalFails()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses(new StubRemoveBus(RemoveAttachmentResult.Failure("Not yours.")));
        var list = ctx.Render<WorkOrderAttachmentList>(parameters => Parameters(parameters, [Photo], canRemove: true, () => { }));

        await list.Find($"[data-testid='{nameof(WorkOrderManage.Elements.AttachmentRemove)}']").ClickAsync(new());
        await list.Find($"[data-testid='{nameof(WorkOrderManage.Elements.AttachmentRemoveConfirm)}']").ClickAsync(new());

        list.Find($"[data-testid='{nameof(WorkOrderManage.Elements.AttachmentRemoveError)}']").TextContent.ShouldBe("Not yours.");
    }

    [Test]
    public async Task ShouldNameTableAndScopeColumnHeaders_WhenAttachmentsExist()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses(new StubRemoveBus(RemoveAttachmentResult.Success));

        var list = ctx.Render<WorkOrderAttachmentList>(parameters => Parameters(parameters, [Photo], canRemove: false, () => { }));

        var title = list.Find("h2");
        title.TextContent.ShouldBe("Attachments");
        list.Find("table").GetAttribute("aria-labelledby").ShouldBe(title.Id);
        list.FindAll("th").ShouldAllBe(th => th.GetAttribute("scope") == "col");
        list.FindAll("th").Count.ShouldBe(5);
    }

    [Test]
    public async Task ShouldHideActionsHeader_WhenTheUserMayRemove()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses(new StubRemoveBus(RemoveAttachmentResult.Success));

        var list = ctx.Render<WorkOrderAttachmentList>(parameters => Parameters(parameters, [Photo], canRemove: true, () => { }));

        list.FindAll("th").ShouldAllBe(th => th.GetAttribute("scope") == "col");
        list.FindAll("th").Count.ShouldBe(6);
        var actionHeader = list.FindAll("th").Last();
        var hidden = actionHeader.QuerySelector(".visually-hidden");
        hidden.ShouldNotBeNull();
        hidden.TextContent.ShouldBe("Actions");
    }

    private static void Parameters(
        ComponentParameterCollectionBuilder<WorkOrderAttachmentList> parameters,
        WorkOrderAttachment[] attachments,
        bool canRemove,
        Action changed) =>
        parameters
            .Add(p => p.Attachments, attachments)
            .Add(p => p.CanRemove, canRemove)
            .Add(p => p.WorkOrderId, Guid.Empty)
            .Add(p => p.CurrentUserName, "jsmith")
            .Add(p => p.AttachmentsChanged, EventCallback.Factory.Create(new object(), changed));

    private sealed class StubRemoveBus(RemoveAttachmentResult result) : IBus
    {
        public RemoveAttachmentCommand? Removed { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            Removed = (RemoveAttachmentCommand)request;
            return Task.FromResult((TResponse)(object)result);
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }
}
