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
public class WorkOrderManageSubmitTests
{
    [Test]
    public async Task ShouldNavigateToSearch_WhenSaveCommandSubmittedForNewWorkOrder()
    {
        await using var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var bus = new StubSubmitBus();

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").ShouldNotBeNull();
        });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").ChangeAsync(new() { Value = "Submit title" });
        await component.Find($"[data-testid='{WorkOrderManage.Elements.Description}']").ChangeAsync(new() { Value = "Submit description" });

        var saveButton = component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']");
        await saveButton.ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            bus.LastCommand.ShouldNotBeNull();
            bus.LastCommand.ShouldBeOfType<SaveDraftCommand>().PreviousRoomNumber.ShouldBeNull();
            navigationManager.Uri.ShouldContain("/workorder/search");
        });
    }

    [Test]
    public async Task ShouldLoadExistingWorkOrder_WhenEditModeSubmitted()
    {
        await using var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-EDIT",
            Title = "Existing",
            Description = "Desc",
            Status = WorkOrderStatus.Draft,
            Creator = user
        };
        var bus = new StubSubmitBus(existing);

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/?Mode=Edit&Id=WO-EDIT");

        var component = ctx.Render<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, "WO-EDIT"));

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").GetAttribute("value")
                .ShouldBe("Existing");
        });

        var saveButton = component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']");
        await saveButton.ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            bus.WorkOrderByNumberHits.ShouldBeGreaterThan(0);
            bus.LastCommand.ShouldNotBeNull();
            navigationManager.Uri.ShouldContain("/workorder/search");
        });
    }

    [Test]
    public async Task ShouldSendPersistedRoomNumberAsPrevious_WhenEditModeRoomChanged()
    {
        await using var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-ROOM",
            Title = "Existing",
            Description = "Desc",
            RoomNumber = "Room A",
            Status = WorkOrderStatus.Draft,
            Creator = user
        };
        var bus = new StubSubmitBus(existing);
        RegisterServices(ctx, bus, user);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/?Mode=Edit&Id=WO-ROOM");

        var component = ctx.Render<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, "WO-ROOM"));

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").GetAttribute("value")
                .ShouldBe("Existing");
        });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.RoomNumber}']").ChangeAsync(new() { Value = "Room B" });
        await component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            var command = bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            command.PreviousRoomNumber.ShouldBe("Room A");
            command.WorkOrder.RoomNumber.ShouldBe("Room B");
        });
    }

    [Test]
    public async Task HandleSubmit_SubmittedTwiceWhileInFlight_SendsOneCommand()
    {
        await using var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var gate = new TaskCompletionSource();
        var bus = new StubSubmitBus(commandGate: gate.Task);
        RegisterServices(ctx, bus, user);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();
        await component.WaitForAssertionAsync(() =>
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").ShouldNotBeNull());
        await component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").ChangeAsync(new() { Value = "Once" });
        await component.Find($"[data-testid='{WorkOrderManage.Elements.Description}']").ChangeAsync(new() { Value = "Only once" });

        var saveSelector = $"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']";
        _ = component.Find(saveSelector).ClickAsync(new());
        await component.WaitForAssertionAsync(() => bus.CommandCount.ShouldBe(1));
        var pendingSave = component.Find(saveSelector);
        pendingSave.HasAttribute("disabled").ShouldBeTrue();
        await component.Find("form").SubmitAsync();
        gate.SetResult();

        await component.WaitForAssertionAsync(() => navigationManager.Uri.ShouldContain("/workorder/search"));
        bus.CommandCount.ShouldBe(1);
    }

    [Test]
    public async Task ShouldSendPriorityAndBuilding_WhenSaved()
    {
        await using var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var bus = new StubSubmitBus();
        RegisterServices(ctx, bus, user);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();
        await component.WaitForAssertionAsync(() =>
            component.Find($"[data-testid='{WorkOrderManage.Elements.Priority}']").ShouldNotBeNull());
        component.Find($"[data-testid='{WorkOrderManage.Elements.Priority}']").GetAttribute("value")
            .ShouldBe(nameof(WorkOrderPriority.Medium));
        await component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").ChangeAsync(new() { Value = "Organ" });
        await component.Find($"[data-testid='{WorkOrderManage.Elements.Description}']").ChangeAsync(new() { Value = "Tune it" });
        await component.Find($"[data-testid='{WorkOrderManage.Elements.Priority}']").ChangeAsync(new() { Value = nameof(WorkOrderPriority.High) });
        await component.Find($"[data-testid='{WorkOrderManage.Elements.Building}']").ChangeAsync(new() { Value = "  Sanctuary " });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            var command = bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            command.WorkOrder.Priority.ShouldBe(WorkOrderPriority.High);
            command.WorkOrder.Building.ShouldBe("Sanctuary");
        });
    }

    private static void RegisterServices(BunitContext ctx, IBus bus, Employee user)
    {
        ctx.Services.AddSingleton(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();
    }

    private sealed class StubSubmitBus(WorkOrder? existing = null, Task? commandGate = null) : Bus(null!)
    {
        public object? LastCommand { get; private set; }
        public int CommandCount { get; private set; }
        public int WorkOrderByNumberHits { get; private set; }

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

            if (request is WorkOrderByNumberQuery)
            {
                WorkOrderByNumberHits++;
                return Task.FromResult((TResponse)(object)(existing ?? throw new InvalidOperationException("missing")));
            }

            if (request is EmployeeByUserNameQuery byUser)
            {
                var employee = new Employee(byUser.Username, "A", "B", "a@b.com");
                return Task.FromResult((TResponse)(object)employee);
            }

            if (request is WorkOrderStatusHistoryQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderStatusChange>());
            }

            if (request is IStateCommand)
            {
                LastCommand = request;
                CommandCount++;
                var wo = existing ?? new WorkOrder { Number = "WO-TEST", Status = WorkOrderStatus.Draft };
                var result = new StateCommandResult(wo);
                return RespondAfterGate((TResponse)(object)result);
            }

            if (request is WorkOrderStatusHistoryQuery)

            {

                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderStatusChange>());

            }


            throw new NotImplementedException(request.GetType().Name);
        }

        private async Task<TResponse> RespondAfterGate<TResponse>(TResponse response)
        {
            if (commandGate != null)
            {
                await commandGate;
            }

            return response;
        }
    }

    private sealed class StubWorkOrderBuilder(Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee current)
        {
            return new WorkOrder
            {
                Id = Guid.NewGuid(),
                Number = "WO-NEW",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "",
                Description = ""
            };
        }
    }

    private sealed class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private sealed class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode) => Task.FromResult(text);
    }
}
