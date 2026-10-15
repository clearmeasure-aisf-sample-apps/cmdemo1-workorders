using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageAttachmentsTests
{
    [Test]
    public async Task WorkOrderManage_ShouldRenderAttachmentsSection()
    {
        await using var ctx = new BunitContext();

        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();

        var attachments = new[]
        {
            new WorkOrderAttachment
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrderId,
                FileName = "damage-photo.jpg",
                ContentType = "image/jpeg",
                FileSize = 2048,
                UploadedById = uploader.Id,
                UploadedBy = uploader,
                UploadedDate = new DateTime(2025, 3, 1, 10, 0, 0)
            }
        };

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(attachments));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(uploader));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var section = component.Find($"[data-testid='{WorkOrderManage.Elements.AttachmentsSection}']");
            section.ShouldNotBeNull();
        });

        var fileNameCell = component.Find($"[data-testid='{WorkOrderManage.Elements.AttachmentFileName}']");
        fileNameCell.TextContent.ShouldBe("damage-photo.jpg");
    }

    [Test]
    public async Task WorkOrderManage_ShouldShowRemoveButton_WhenUserIsCreator()
    {
        var workOrderId = Guid.NewGuid();
        var bus = new StubWorkOrderManageBus([NewAttachment(workOrderId, "damage-photo.jpg")]);
        await using var ctx = CreateContext(bus, Creator, new StubWorkOrderBuilder(workOrderId));

        var component = await RenderWithAttachmentsAsync(ctx);

        var button = component.Find(TestId(WorkOrderManage.Elements.AttachmentRemove));
        button.GetAttribute("type").ShouldBe("button");
        button.GetAttribute("aria-label").ShouldBe("Remove attachment damage-photo.jpg");
    }

    [Test]
    public async Task WorkOrderManage_ShouldShowRemoveButton_WhenUserIsAssignee()
    {
        var workOrderId = Guid.NewGuid();
        var bus = new StubWorkOrderManageBus([NewAttachment(workOrderId, "damage-photo.jpg")]);
        var builder = new StubWorkOrderBuilder(workOrderId, WorkOrderStatus.InProgress, Creator, Assignee);
        await using var ctx = CreateContext(bus, Assignee, builder);

        var component = await RenderWithAttachmentsAsync(ctx);

        component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRemove)).Count.ShouldBe(1);
    }

    [Test]
    public async Task WorkOrderManage_ShouldHideRemoveButton_WhenUserIsNotCreatorOrAssignee()
    {
        var workOrderId = Guid.NewGuid();
        var stranger = new Employee("hsimpson", "Homer", "Simpson", "hs@example.com") { Id = Guid.NewGuid() };
        var bus = new StubWorkOrderManageBus([NewAttachment(workOrderId, "damage-photo.jpg")]);
        var builder = new StubWorkOrderBuilder(workOrderId, WorkOrderStatus.Assigned, Creator, Assignee);
        await using var ctx = CreateContext(bus, stranger, builder);

        var component = await RenderWithAttachmentsAsync(ctx);

        component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRow)).Count.ShouldBe(1);
        component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRemove)).ShouldBeEmpty();
    }

    [TestCase("Complete")]
    [TestCase("Cancelled")]
    public async Task WorkOrderManage_ShouldHideRemoveButton_WhenWorkOrderIsClosed(string statusKey)
    {
        var workOrderId = Guid.NewGuid();
        var bus = new StubWorkOrderManageBus([NewAttachment(workOrderId, "damage-photo.jpg")]);
        var builder = new StubWorkOrderBuilder(workOrderId, WorkOrderStatus.FromKey(statusKey));
        await using var ctx = CreateContext(bus, Creator, builder);

        var component = await RenderWithAttachmentsAsync(ctx);

        component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRemove)).ShouldBeEmpty();
    }

    [Test]
    public async Task WorkOrderManage_ShouldAskForConfirmation_WhenRemoveClicked()
    {
        var workOrderId = Guid.NewGuid();
        var bus = new StubWorkOrderManageBus([NewAttachment(workOrderId, "damage-photo.jpg")]);
        await using var ctx = CreateContext(bus, Creator, new StubWorkOrderBuilder(workOrderId));
        var component = await RenderWithAttachmentsAsync(ctx);

        await component.Find(TestId(WorkOrderManage.Elements.AttachmentRemove)).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
            component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRemoveConfirm)).Count.ShouldBe(1));
        component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRemoveCancel)).Count.ShouldBe(1);
        component.Find(TestId(WorkOrderManage.Elements.AttachmentRow)).TextContent
            .ShouldContain("Remove damage-photo.jpg?");
        bus.LastRemoveCommand.ShouldBeNull();
    }

    [Test]
    public async Task WorkOrderManage_ShouldKeepAttachment_WhenRemoveCancelled()
    {
        var workOrderId = Guid.NewGuid();
        var bus = new StubWorkOrderManageBus([NewAttachment(workOrderId, "damage-photo.jpg")]);
        await using var ctx = CreateContext(bus, Creator, new StubWorkOrderBuilder(workOrderId));
        var component = await RenderWithAttachmentsAsync(ctx);
        await component.Find(TestId(WorkOrderManage.Elements.AttachmentRemove)).ClickAsync(new());

        await component.Find(TestId(WorkOrderManage.Elements.AttachmentRemoveCancel)).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
            component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRemove)).Count.ShouldBe(1));
        component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRemoveConfirm)).ShouldBeEmpty();
        component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRow)).Count.ShouldBe(1);
        bus.LastRemoveCommand.ShouldBeNull();
    }

    [Test]
    public async Task WorkOrderManage_ShouldRemoveAttachment_WhenConfirmed()
    {
        var workOrderId = Guid.NewGuid();
        var first = NewAttachment(workOrderId, "damage-photo.jpg");
        var bus = new StubWorkOrderManageBus([first, NewAttachment(workOrderId, "invoice.pdf")]);
        await using var ctx = CreateContext(bus, Creator, new StubWorkOrderBuilder(workOrderId));
        var component = await RenderWithAttachmentsAsync(ctx);
        await component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRemove))[0].ClickAsync(new());

        await component.Find(TestId(WorkOrderManage.Elements.AttachmentRemoveConfirm)).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
            component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRow)).Count.ShouldBe(1));
        component.Find(TestId(WorkOrderManage.Elements.AttachmentFileName)).TextContent.ShouldBe("invoice.pdf");
        bus.LastRemoveCommand.ShouldBe(new RemoveAttachmentCommand(workOrderId, first.Id, Creator.UserName));
        component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRemoveError)).ShouldBeEmpty();
    }

    [Test]
    public async Task WorkOrderManage_ShouldHideSection_WhenLastAttachmentRemoved()
    {
        var workOrderId = Guid.NewGuid();
        var bus = new StubWorkOrderManageBus([NewAttachment(workOrderId, "damage-photo.jpg")]);
        await using var ctx = CreateContext(bus, Creator, new StubWorkOrderBuilder(workOrderId));
        var component = await RenderWithAttachmentsAsync(ctx);
        await component.Find(TestId(WorkOrderManage.Elements.AttachmentRemove)).ClickAsync(new());

        await component.Find(TestId(WorkOrderManage.Elements.AttachmentRemoveConfirm)).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
            component.FindAll(TestId(WorkOrderManage.Elements.AttachmentsSection)).ShouldBeEmpty());
    }

    [Test]
    public async Task WorkOrderManage_ShouldShowError_WhenRemoveFails()
    {
        const string error = "Only the creator or the assignee can remove attachments.";
        var workOrderId = Guid.NewGuid();
        var bus = new StubWorkOrderManageBus([NewAttachment(workOrderId, "damage-photo.jpg")])
        {
            RemoveResult = RemoveAttachmentResult.Failure(error)
        };
        await using var ctx = CreateContext(bus, Creator, new StubWorkOrderBuilder(workOrderId));
        var component = await RenderWithAttachmentsAsync(ctx);
        await component.Find(TestId(WorkOrderManage.Elements.AttachmentRemove)).ClickAsync(new());

        await component.Find(TestId(WorkOrderManage.Elements.AttachmentRemoveConfirm)).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
            component.Find(TestId(WorkOrderManage.Elements.AttachmentRemoveError)).TextContent.ShouldBe(error));
        component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRow)).Count.ShouldBe(1);
        component.FindAll(TestId(WorkOrderManage.Elements.AttachmentRemove)).Count.ShouldBe(1);
    }

    private static readonly Employee Creator =
        new("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };

    private static readonly Employee Assignee =
        new("tlovejoy", "Timothy", "Lovejoy", "tl@example.com") { Id = Guid.NewGuid() };

    private static WorkOrderAttachment NewAttachment(Guid workOrderId, string fileName) => new()
    {
        Id = Guid.NewGuid(),
        WorkOrderId = workOrderId,
        FileName = fileName,
        ContentType = "image/jpeg",
        FileSize = 2048,
        UploadedById = Creator.Id,
        UploadedBy = Creator,
        UploadedDate = new DateTime(2025, 3, 1, 10, 0, 0)
    };

    private static BunitContext CreateContext(StubWorkOrderManageBus bus, Employee loggedInAs,
        StubWorkOrderBuilder builder)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(builder);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(loggedInAs));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));
        return ctx;
    }

    private static async Task<IRenderedComponent<WorkOrderManage>> RenderWithAttachmentsAsync(BunitContext ctx)
    {
        var component = ctx.Render<WorkOrderManage>();
        await component.WaitForAssertionAsync(() =>
            component.FindAll($"[data-testid='{WorkOrderManage.Elements.AttachmentsSection}']").Count.ShouldBe(1));
        return component;
    }

    private static string TestId(WorkOrderManage.Elements element) => $"[data-testid='{element}']";

    private class StubWorkOrderManageBus(IEnumerable<WorkOrderAttachment> attachments) : Bus(null!)
    {
        private readonly List<WorkOrderAttachment> _attachments = attachments.ToList();

        public RemoveAttachmentCommand? LastRemoveCommand { get; private set; }

        public RemoveAttachmentResult RemoveResult { get; init; } = RemoveAttachmentResult.Success;

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = Array.Empty<Employee>();
                return Task.FromResult<TResponse>((TResponse)(object)employees);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)_attachments.ToArray());
            }

            if (request is RemoveAttachmentCommand remove)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Remove(remove));
            }

            if (request is WorkOrderStatusHistoryQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderStatusChange>());
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }

        private RemoveAttachmentResult Remove(RemoveAttachmentCommand command)
        {
            LastRemoveCommand = command;
            if (RemoveResult.Succeeded)
            {
                _attachments.RemoveAll(a => a.Id == command.AttachmentId);
            }

            return RemoveResult;
        }
    }

    private class StubWorkOrderBuilder(
        Guid workOrderId,
        WorkOrderStatus? status = null,
        Employee? creatorOverride = null,
        Employee? assignee = null) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator)
        {
            return new WorkOrder
            {
                Id = workOrderId,
                Number = "WO-TEST",
                Status = status ?? WorkOrderStatus.Draft,
                Creator = creatorOverride ?? creator,
                Assignee = assignee,
                Title = "Test Order"
            };
        }
    }

    private class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode)
        {
            return Task.FromResult(text);
        }
    }
}
