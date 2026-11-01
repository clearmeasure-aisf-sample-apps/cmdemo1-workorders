using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Palermo.BlazorMvc;
using Shouldly;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageCopyButtonTests
{
    [Test]
    public async Task WorkOrderManage_ShouldRenderCopyButton_WhenExistingWorkOrder()
    {
        await using var ctx = new BunitContext();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();
        var workOrder = new WorkOrder
        {
            Id = workOrderId,
            Number = "WO-123",
            Status = WorkOrderStatus.Draft,
            Creator = creator,
            Title = "Test Work Order"
        };

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(workOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId, creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSingleton<IJSRuntime>(new StubJsRuntime());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/workorder/manage/{workOrder.Id}");

        var component = ctx.Render<WorkOrderManage>();

        var copyButton = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.CopyWorkOrderNumber}']");
        copyButton.ShouldNotBeNull();
        copyButton.GetAttribute("aria-label").ShouldBe("Copy work order number");
        copyButton.TextContent.ShouldContain("Copy");
    }

    [Test]
    public async Task WorkOrderManage_ShouldNotRenderCopyButton_WhenNewWorkOrder()
    {
        await using var ctx = new BunitContext();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId, creator, isNewMode: true));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSingleton<IJSRuntime>(new StubJsRuntime());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        try
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.CopyWorkOrderNumber}']");
            Assert.Fail("Expected no copy button for new work order, but one was found");
        }
        catch (ElementNotFoundException)
        {
            // Expected: button should not be present
        }
    }

    [Test]
    public async Task CopyWorkOrderNumberAsync_ShouldExecuteSuccessfully_WhenClickingCopyButton()
    {
        await using var ctx = new BunitContext();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();
        var workOrder = new WorkOrder
        {
            Id = workOrderId,
            Number = "WO-456",
            Status = WorkOrderStatus.Draft,
            Creator = creator,
            Title = "Test Work Order"
        };

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(workOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId, creator, number: "WO-456"));
        var js = new StubJsRuntime();
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSingleton<IJSRuntime>(js);
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/workorder/manage/{workOrder.Id}");

        var component = ctx.Render<WorkOrderManage>();

        var copyButton = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.CopyWorkOrderNumber}']");
        copyButton.ShouldNotBeNull();

        var click = copyButton.ClickAsync(new());
        await component.WaitForAssertionAsync(() =>
        {
            js.LastCopiedText.ShouldBe("WO-456");
            component.Find("#copy-status").TextContent.ShouldContain("Copied");
        });
        await click;
    }

    [Test]
    public async Task CopyWorkOrderNumberAsync_ShouldHandleJSException_Gracefully()
    {
        await using var ctx = new BunitContext();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();
        var workOrder = new WorkOrder
        {
            Id = workOrderId,
            Number = "WO-789",
            Status = WorkOrderStatus.Draft,
            Creator = creator,
            Title = "Test Work Order"
        };

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(workOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId, creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSingleton<IJSRuntime>(new StubJsRuntime(throwJsException: true));
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/workorder/manage/{workOrder.Id}");

        var component = ctx.Render<WorkOrderManage>();

        var copyButton = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.CopyWorkOrderNumber}']");
        copyButton.ShouldNotBeNull();

        await copyButton.ClickAsync(new());

        var statusElement = component.Find("#copy-status");
        statusElement.TextContent.ShouldNotContain("Copied");
    }

    [Test]
    public async Task CopyWorkOrderNumberAsync_ShouldHandleJSDisconnectedException_Gracefully()
    {
        await using var ctx = new BunitContext();

        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();
        var workOrder = new WorkOrder
        {
            Id = workOrderId,
            Number = "WO-999",
            Status = WorkOrderStatus.Draft,
            Creator = creator,
            Title = "Test Work Order"
        };

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(workOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId, creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSingleton<IJSRuntime>(new StubJsRuntime(throwDisconnectedException: true));
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/workorder/manage/{workOrder.Id}");

        var component = ctx.Render<WorkOrderManage>();

        var copyButton = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.CopyWorkOrderNumber}']");
        copyButton.ShouldNotBeNull();

        await copyButton.ClickAsync(new());

        var statusElement = component.Find("#copy-status");
        statusElement.TextContent.ShouldNotContain("Copied");
    }

    private class StubWorkOrderManageBus(WorkOrder? workOrder = null) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = Array.Empty<Employee>();
                return Task.FromResult((TResponse)(object)employees);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            if (request is WorkOrderByNumberQuery && workOrder != null)
            {
                return Task.FromResult((TResponse)(object)workOrder);
            }

            if (request is WorkOrderStatusHistoryQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderStatusChange>());
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder(Guid workOrderId, Employee creator, bool isNewMode = false, string number = "WO-NEW") : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser)
        {
            return new WorkOrder
            {
                Id = isNewMode ? Guid.Empty : workOrderId,
                Number = number,
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "New Work Order"
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

    private class StubJsRuntime(bool throwJsException = false, bool throwDisconnectedException = false) : IJSRuntime
    {
        private readonly StubClipboardModule _module = new(throwJsException, throwDisconnectedException);

        public string? LastCopiedText => _module.LastCopiedText;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import" && typeof(TValue) == typeof(IJSObjectReference))
            {
                return ValueTask.FromResult((TValue)(object)_module);
            }

            throw new InvalidOperationException($"Unexpected InvokeAsync: {identifier}");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    private class StubClipboardModule(bool throwJsException = false, bool throwDisconnectedException = false) : IJSObjectReference
    {
        public string? LastCopiedText { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "copyToClipboard")
            {
                if (args is [string copied, ..])
                {
                    LastCopiedText = copied;
                }

                if (throwJsException)
                {
                    throw new JSException("Clipboard permission denied");
                }

                if (throwDisconnectedException)
                {
                    throw new JSDisconnectedException("JS runtime disconnected");
                }

                return ValueTask.FromResult((TValue)(object)true);
            }

            throw new InvalidOperationException($"Unexpected InvokeAsync: {identifier}");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }
    }
}
