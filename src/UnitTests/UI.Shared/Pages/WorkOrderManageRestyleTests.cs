using System.Text.RegularExpressions;
using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageRestyleTests
{
    private static readonly Employee Creator = new("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };

    [Test]
    public async Task ShouldRenderPageHeaderIconsAndExistingTestIds()
    {
        await using var ctx = CreateNewContext();

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            component.FindComponent<PageHeader>().ShouldNotBeNull();
            var heading = component.Find($"[data-testid='{WorkOrderManage.Elements.ManageHeadingTooltip}']");
            heading.TagName.ShouldBe("H1", StringCompareShould.IgnoreCase);
            heading.GetAttribute("title").ShouldBe("Create or edit a work order");
            component.Find("h1 [data-testid='" + WorkOrderManage.Elements.WorkOrderNumber + "']").TextContent
                .ShouldBe("WO-TIP");
            component.Find($"[data-testid='{WorkOrderManage.Elements.RequiredFieldsNote}']").TextContent.Trim()
                .ShouldBe("Fields marked with * are required.");

            var speak = component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakTitle}']");
            speak.GetAttribute("aria-label").ShouldBe("Speak title");
            component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakTitle}'] .bi-volume-up").ShouldNotBeNull();
            var dictate = component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}']");
            dictate.GetAttribute("aria-label").ShouldBe("Dictate title");
            component.Find($"[data-testid='{WorkOrderManage.Elements.DictateTitle}'] .bi-mic").ShouldNotBeNull();
            component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakDescription}']").GetAttribute("aria-label")
                .ShouldBe("Speak description");
            component.Find($"[data-testid='{WorkOrderManage.Elements.DictateDescription}']").GetAttribute("aria-label")
                .ShouldBe("Dictate description");
        });
    }

    [Test]
    public async Task ShouldRenderSearchStatusPill_WhenAssigned()
    {
        var order = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-AS",
            Title = "Assigned",
            Description = "Assigned",
            Status = WorkOrderStatus.Assigned,
            Creator = Creator,
            CreatedDate = DateTime.UtcNow
        };
        await using var ctx = CreateEditContext(order, TimeProvider.System);

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var expected = StatusPillPresenter.Present(order, false);
            var badge = component.Find($"[data-testid='{WorkOrderManage.Elements.Status}']");
            badge.GetAttribute("class").ShouldBe(expected.CssClass);
            badge.TextContent.ShouldBe(expected.Text);
        });
    }

    [Test]
    public async Task ShouldRenderOverdueStatusPill_WhenDueDateIsPast()
    {
        var clock = new StubClock(new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero));
        var order = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-OD",
            Title = "Overdue",
            Description = "Overdue",
            Status = WorkOrderStatus.Assigned,
            Creator = Creator,
            CreatedDate = DateTime.UtcNow,
            DueDate = new DateOnly(2026, 6, 14)
        };
        await using var ctx = CreateEditContext(order, clock);

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var expected = StatusPillPresenter.Present(order, true);
            var badge = component.Find($"[data-testid='{WorkOrderManage.Elements.Status}']");
            badge.GetAttribute("class").ShouldBe(expected.CssClass);
            badge.TextContent.ShouldBe("Overdue");
            component.FindAll(".visually-hidden")
                .Select(element => element.TextContent.Trim())
                .ShouldContain(expected.HiddenText!);
        });
    }

    [Test]
    public void ShouldKeepFormStylesheetFreeOfHexColours()
    {
        var path = Path.GetFullPath(Path.Combine(
            NUnit.Framework.TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI.Shared", "Pages", "WorkOrderManage.razor.css"));
        File.Exists(path).ShouldBeTrue($"Expected scoped stylesheet at {path}");
        var css = File.ReadAllText(path);

        Regex.IsMatch(css, "#[0-9a-fA-F]{3,8}").ShouldBeFalse();
        css.ShouldNotContain("rgba(");
        css.ShouldNotContain("hsl(");
        css.ShouldContain("rgb(254, 240, 138)");
        css.ShouldContain("rgb(254, 202, 202)");
        css.ShouldContain("var(--control-height)");
        css.ShouldContain(".token-card");
        css.ShouldContain(".btn-app--primary");
        css.ShouldContain(".status-badge--overdue");
        css.ShouldContain(".status-badge--waiting");
        css.ShouldContain(".due-date-field ::deep .due-date-today");
        css.ShouldContain(".due-date-field ::deep .due-date-overdue");
    }

    private static BunitContext CreateNewContext()
    {
        var ctx = new BunitContext();
        var workOrderId = Guid.NewGuid();
        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(null));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId, Creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(Creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));
        return ctx;
    }

    private static BunitContext CreateEditContext(WorkOrder order, TimeProvider clock)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(order));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(clock);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(order.Id, Creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(Creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "edit"));
        return ctx;
    }

    private sealed class StubClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubWorkOrderManageBus(WorkOrder? order) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)order!);
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

    private sealed class StubWorkOrderBuilder(Guid workOrderId, Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser) => new()
        {
            Id = workOrderId,
            Number = "WO-TIP",
            Status = WorkOrderStatus.Draft,
            Creator = creator,
            Title = "Heading tooltip"
        };
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
