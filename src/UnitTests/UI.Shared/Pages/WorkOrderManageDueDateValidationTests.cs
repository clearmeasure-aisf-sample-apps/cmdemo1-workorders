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
public class WorkOrderManageDueDateValidationTests
{
    [Test]
    public async Task ShouldShowMessageAndNotSave_WhenNewWorkOrderDueDateIsInThePast()
    {
        await using var ctx = new BunitContext();
        var user = Creator();
        var bus = new StubDueDateBus();
        var navigationManager = Register(ctx, bus, user);
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();
        await FillRequiredFields(component);
        await component.Find($"[data-testid='{WorkOrderManage.Elements.DueDate}']")
            .ChangeAsync(new() { Value = "2020-01-01" });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.DueDateValidation}']").TextContent
                .ShouldContain(WorkOrder.DueDateInThePastMessage);
            bus.LastCommand.ShouldBeNull();
            navigationManager.Uri.ShouldNotContain("/workorder/search");
        });
    }

    [Test]
    public async Task ShouldSave_WhenNewWorkOrderDueDateIsToday()
    {
        await using var ctx = new BunitContext();
        var user = Creator();
        var bus = new StubDueDateBus();
        var navigationManager = Register(ctx, bus, user);
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));
        var today = ChurchTimeZone.Today(TimeProvider.System);

        var component = ctx.Render<WorkOrderManage>();
        await FillRequiredFields(component);
        await component.Find($"[data-testid='{WorkOrderManage.Elements.DueDate}']")
            .ChangeAsync(new() { Value = today.ToString("yyyy-MM-dd") });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            var command = bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            command.WorkOrder.DueDate.ShouldBe(today);
            command.PreviousDueDate.ShouldBeNull();
            navigationManager.Uri.ShouldContain("/workorder/search");
        });
    }

    [Test]
    public async Task ShouldSave_WhenNewWorkOrderDueDateIsEmpty()
    {
        await using var ctx = new BunitContext();
        var user = Creator();
        var bus = new StubDueDateBus();
        var navigationManager = Register(ctx, bus, user);
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();
        await FillRequiredFields(component);
        await component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            var command = bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            command.WorkOrder.DueDate.ShouldBeNull();
            navigationManager.Uri.ShouldContain("/workorder/search");
        });
    }

    [Test]
    public async Task ShouldSave_WhenExistingPastDueDateIsUnchanged()
    {
        await using var ctx = new BunitContext();
        var user = Creator();
        var past = ChurchTimeZone.Today(TimeProvider.System).AddDays(-5);
        var existing = Draft(user, "WO-PAST", past);
        var bus = new StubDueDateBus(existing);
        var navigationManager = Register(ctx, bus, user);
        navigationManager.NavigateTo("http://localhost/?Mode=Edit&Id=WO-PAST");

        var component = ctx.Render<WorkOrderManage>(parameters => parameters.Add(p => p.Id, "WO-PAST"));
        await component.WaitForAssertionAsync(() =>
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").GetAttribute("value").ShouldBe("Existing"));
        await component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']")
            .ChangeAsync(new() { Value = "Edited title" });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            var command = bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            command.PreviousDueDate.ShouldBe(past);
            command.WorkOrder.DueDate.ShouldBe(past);
            command.WorkOrder.Title.ShouldBe("Edited title");
            navigationManager.Uri.ShouldContain("/workorder/search");
        });
    }

    private static async Task FillRequiredFields(IRenderedComponent<WorkOrderManage> component)
    {
        await component.WaitForAssertionAsync(() =>
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").ShouldNotBeNull());
        await component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']")
            .ChangeAsync(new() { Value = "Submit title" });
        await component.Find($"[data-testid='{WorkOrderManage.Elements.Description}']")
            .ChangeAsync(new() { Value = "Submit description" });
    }

    private static Employee Creator()
    {
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        return user;
    }

    private static WorkOrder Draft(Employee user, string number, DateOnly dueDate) =>
        new()
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = "Existing",
            Description = "Desc",
            Status = WorkOrderStatus.Draft,
            Creator = user,
            DueDate = dueDate
        };

    private static NavigationManager Register(BunitContext ctx, IBus bus, Employee user)
    {
        ctx.Services.AddSingleton(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubDueDateWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubDueDateUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubDueDateTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();
        return ctx.Services.GetRequiredService<NavigationManager>();
    }

    private sealed class StubDueDateBus(WorkOrder? existing = null) : Bus(null!)
    {
        public object? LastCommand { get; private set; }

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
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

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)(existing ?? throw new InvalidOperationException("missing")));
            }

            if (request is EmployeeByUserNameQuery byUser)
            {
                var employee = new Employee(byUser.Username, "A", "B", "a@b.com");
                return Task.FromResult((TResponse)(object)employee);
            }

            if (request is IStateCommand)
            {
                LastCommand = request;
                var wo = existing ?? new WorkOrder { Number = "WO-TEST", Status = WorkOrderStatus.Draft };
                return Task.FromResult((TResponse)(object)new StateCommandResult(wo));
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }

    private sealed class StubDueDateWorkOrderBuilder(Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee current) =>
            new()
            {
                Id = Guid.NewGuid(),
                Number = "WO-NEW",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "",
                Description = ""
            };
    }

    private sealed class StubDueDateUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private sealed class StubDueDateTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode) => Task.FromResult(text);
    }
}
