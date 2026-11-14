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
public class WorkOrderManagePrintLinkTests
{
    private static readonly Employee Creator = new("jpalermo", "Jeffrey", "Palermo", "jp@example.com")
    {
        Id = Guid.NewGuid()
    };

    [Test]
    public async Task Should_RenderPrintLink_WithRelNoopenerNoreferrer()
    {
        await using var ctx = CreateEditContext(CreatePrintWorkOrder());
        var component = ctx.Render<WorkOrderManage>();
        var link = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.PrintLink}']");

        link.GetAttribute("href").ShouldBe("/workorder/print/WO-PRINT");
        link.GetAttribute("target").ShouldBe("_blank");
        link.GetAttribute("rel").ShouldBe("noopener noreferrer");
    }

    [Test]
    public async Task Should_RenderPrintLink_WithVisuallyHiddenNewTabText()
    {
        await using var ctx = CreateEditContext(CreatePrintWorkOrder());
        var component = ctx.Render<WorkOrderManage>();
        var link = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.PrintLink}']");

        link.TextContent.ShouldContain("Print");
        var printIndex = link.TextContent.IndexOf("Print", StringComparison.Ordinal);
        var hintIndex = link.TextContent.IndexOf("(opens in a new tab)", StringComparison.Ordinal);
        hintIndex.ShouldBeGreaterThan(printIndex);
        var hidden = link.QuerySelector(".visually-hidden");
        hidden.ShouldNotBeNull();
        hidden.TextContent.ShouldBe("(opens in a new tab)");
    }

    private static WorkOrder CreatePrintWorkOrder() => new()
    {
        Id = Guid.NewGuid(),
        Number = "WO-PRINT",
        Title = "Print link",
        Description = "Print link",
        Status = WorkOrderStatus.Draft,
        Creator = Creator,
        CreatedDate = DateTime.UtcNow
    };

    private static BunitContext CreateEditContext(WorkOrder order)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(order));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubManageWorkOrderCreator(order.Id, Creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(Creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "edit"));
        return ctx;
    }

    private sealed class StubWorkOrderManageBus(WorkOrder order) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)order);
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

    private sealed class StubManageWorkOrderCreator(Guid workOrderId, Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser) => new()
        {
            Id = workOrderId,
            Number = "WO-PRINT",
            Status = WorkOrderStatus.Draft,
            Creator = creator,
            Title = "Print link"
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
