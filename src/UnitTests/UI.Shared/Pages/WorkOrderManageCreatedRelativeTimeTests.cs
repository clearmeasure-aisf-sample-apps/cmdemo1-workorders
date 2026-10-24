using System.Globalization;
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
public class WorkOrderManageCreatedRelativeTimeTests
{
    private static readonly Employee Creator = new("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };

    [Test]
    public async Task WorkOrderManage_ShouldShowRelativeCreatedTime_BesideTheFullDate()
    {
        var now = new DateTimeOffset(2026, 6, 15, 15, 0, 0, TimeSpan.Zero);
        var created = now.UtcDateTime.AddDays(-3);
        var clock = new FixedTimeProvider(now);
        var workOrder = SampleWorkOrder(created);
        await using var ctx = CreateContext(new StubWorkOrderManageBus(workOrder), clock);

        var component = ctx.Render<WorkOrderManage>();

        var createdElement = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.CreatedDate}']");
        var relativeElement = component.Find(
            $"[data-testid='{WorkOrderManage.Elements.CreatedRelativeTime}']");

        relativeElement.TextContent.Trim().ShouldBe("3 days ago");
        relativeElement.GetAttribute("title").ShouldBe(createdElement.TextContent.Trim());
        createdElement.TextContent.ShouldNotContain("ago");
        createdElement.TextContent.Trim().ShouldBe(created.ToString("G", CultureInfo.CurrentCulture));
    }

    [Test]
    public async Task WorkOrderManage_ShouldOmitRelativeTime_WhenCreatedDateIsNull()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 6, 15, 15, 0, 0, TimeSpan.Zero));
        var workOrder = SampleWorkOrder(null);
        await using var ctx = CreateContext(new StubWorkOrderManageBus(workOrder), clock);

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForElementAsync($"[data-testid='{WorkOrderManage.Elements.CreatedDate}']");
        component.FindAll($"[data-testid='{WorkOrderManage.Elements.CreatedRelativeTime}']").Count.ShouldBe(0);
    }

    private static WorkOrder SampleWorkOrder(DateTime? created) => new()
    {
        Id = Guid.NewGuid(),
        Number = "WO-001",
        Title = "Test",
        Description = "Test",
        Status = WorkOrderStatus.Draft,
        Creator = Creator,
        CreatedDate = created
    };

    private static BunitContext CreateContext(Bus bus, TimeProvider clock)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(clock);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(Creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "edit"));
        return ctx;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubWorkOrderManageBus(WorkOrder workOrder) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)workOrder);
            }

            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            if (request is WorkOrderStatusHistoryQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderStatusChange>());
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private sealed class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser) => new()
        {
            Id = Guid.NewGuid(),
            Creator = currentUser,
            Status = WorkOrderStatus.Draft
        };
    }

    private sealed class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private sealed class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string languageCode) => Task.FromResult(text);
    }
}
