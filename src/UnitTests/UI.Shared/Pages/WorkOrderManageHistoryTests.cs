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
using Palermo.BlazorMvc;
using Shouldly;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageHistoryTests
{
    private static readonly Employee Creator = new("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
    private static readonly Employee Assignee = new("hsimpson", "Homer", "Simpson", "hs@example.com") { Id = Guid.NewGuid() };

    [Test]
    public async Task WorkOrderManage_ShouldRenderEmptyHistoryMessage_WhenNoStatusChanges()
    {
        var workOrderId = Guid.NewGuid();
        var bus = new StubWorkOrderManageBusWithHistory(
            new WorkOrder
            {
                Id = workOrderId,
                Number = "WO-001",
                Title = "Test",
                Description = "Test",
                Status = WorkOrderStatus.Draft,
                Creator = Creator,
                CreatedDate = DateTime.UtcNow
            },
            Array.Empty<WorkOrderStatusChange>());
        await using var ctx = CreateContext(bus, Creator);

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var historyMessage = component.FindAll($"[data-testid='{WorkOrderManage.Elements.StatusHistory}']");
            historyMessage.Count.ShouldBeGreaterThan(0);
            historyMessage[0].TextContent.ShouldContain("No status changes recorded.");
        });
    }

    [Test]
    public async Task WorkOrderManage_ShouldRenderStatusHistoryTable_WhenChangesExist()
    {
        var workOrderId = Guid.NewGuid();
        var changedDate = new DateTime(2025, 3, 1, 10, 0, 0);
        var history = new[]
        {
            new WorkOrderStatusChange
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrderId,
                FromStatus = WorkOrderStatus.Draft,
                ToStatus = WorkOrderStatus.Assigned,
                ChangedById = Assignee.Id,
                ChangedBy = Assignee,
                ChangedDate = changedDate
            }
        };

        var workOrder = new WorkOrder
        {
            Id = workOrderId,
            Number = "WO-001",
            Title = "Test",
            Description = "Test",
            Status = WorkOrderStatus.Assigned,
            Creator = Creator,
            Assignee = Assignee,
            CreatedDate = DateTime.UtcNow,
            AssignedDate = DateTime.UtcNow
        };
        var bus = new StubWorkOrderManageBusWithHistory(workOrder, history);
        await using var ctx = CreateContext(bus, Creator);

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var rows = component.FindAll($"[data-testid='{WorkOrderManage.Elements.StatusHistoryRow}']");
            rows.Count.ShouldBe(1);
        });

        var cells = component.FindAll($"[data-testid='{WorkOrderManage.Elements.StatusHistoryFromStatus}']");
        cells[0].TextContent.ShouldContain("Draft");

        var toStatusCells = component.FindAll($"[data-testid='{WorkOrderManage.Elements.StatusHistoryToStatus}']");
        toStatusCells[0].TextContent.ShouldContain("Assigned");

        var changedByCells = component.FindAll($"[data-testid='{WorkOrderManage.Elements.StatusHistoryChangedBy}']");
        changedByCells[0].TextContent.ShouldContain("Homer Simpson");
    }

    [Test]
    public async Task WorkOrderManage_ShouldDisplayHistoryNewestFirst()
    {
        var workOrderId = Guid.NewGuid();
        var oldDate = new DateTime(2025, 2, 15, 10, 0, 0);
        var newDate = new DateTime(2025, 3, 1, 10, 0, 0);
        var history = new[]
        {
            // Query returns newest first
            new WorkOrderStatusChange
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrderId,
                FromStatus = WorkOrderStatus.Assigned,
                ToStatus = WorkOrderStatus.InProgress,
                ChangedById = Assignee.Id,
                ChangedBy = Assignee,
                ChangedDate = newDate
            },
            new WorkOrderStatusChange
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrderId,
                FromStatus = WorkOrderStatus.Draft,
                ToStatus = WorkOrderStatus.Assigned,
                ChangedById = Assignee.Id,
                ChangedBy = Assignee,
                ChangedDate = oldDate
            }
        };

        var workOrder = new WorkOrder
        {
            Id = workOrderId,
            Number = "WO-001",
            Title = "Test",
            Description = "Test",
            Status = WorkOrderStatus.InProgress,
            Creator = Creator,
            Assignee = Assignee,
            CreatedDate = DateTime.UtcNow
        };
        var bus = new StubWorkOrderManageBusWithHistory(workOrder, history);
        await using var ctx = CreateContext(bus, Creator);

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var toStatusCells = component.FindAll($"[data-testid='{WorkOrderManage.Elements.StatusHistoryToStatus}']");
            toStatusCells.Count.ShouldBe(2);
            // First one should be In Progress (newest)
            toStatusCells[0].TextContent.ShouldContain("In Progress");
            // Second one should be Assigned (oldest)
            toStatusCells[1].TextContent.ShouldContain("Assigned");
        });
    }

    private static BunitContext CreateContext(Bus bus, Employee loggedInAs)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(Guid.NewGuid()));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(loggedInAs));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "edit"));

        return ctx;
    }

    private class StubWorkOrderManageBusWithHistory(WorkOrder workOrder, WorkOrderStatusChange[] history) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)workOrder);
            }

            if (request is WorkOrderStatusHistoryQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)history);
            }

            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder(Guid workOrderId) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator) => new()
        {
            Id = workOrderId,
            Creator = creator,
            CreatedDate = DateTime.UtcNow,
            Status = WorkOrderStatus.Draft
        };
    }

    private class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string languageCode) => Task.FromResult(text);
    }
}
