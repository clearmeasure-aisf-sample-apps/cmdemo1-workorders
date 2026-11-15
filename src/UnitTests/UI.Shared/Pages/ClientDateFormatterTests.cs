using System.Globalization;
using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class ClientDateFormatterTests
{
    private static readonly DateOnly Due = new(2026, 10, 23);
    private static readonly DateTime Created = new(2026, 10, 23, 15, 4, 5, DateTimeKind.Utc);
    private static readonly Employee Creator = new("jpalermo", "Jeffrey", "Palermo", "jp@example.com")
    {
        Id = Guid.NewGuid()
    };

    [Test]
    public async Task ShouldShowDayFirstDueDate_WhenCultureIsEnGb_OnPageAndList()
    {
        using (new CultureScope("en-GB"))
        {
            await AssertDueDateAsync("23/10/2026");
        }
    }

    [Test]
    public async Task ShouldShowMonthFirstDueDate_WhenCultureIsEnUs_OnPageAndList()
    {
        using (new CultureScope("en-US"))
        {
            await AssertDueDateAsync("10/23/2026");
        }
    }

    [Test]
    public void ShouldReturnNull_WhenDateIsMissing()
    {
        ClientDateFormatter.Format((DateOnly?)null).ShouldBeNull();
        ClientDateFormatter.Format((DateTime?)null).ShouldBeNull();
    }

    [Test]
    public void ShouldLeaveCulture_WhenBrowserLanguageIsBlank()
    {
        var before = CultureInfo.CurrentCulture;
        ClientDateFormatter.ApplyBrowserCulture("  ");
        CultureInfo.CurrentCulture.ShouldBe(before);
    }

    [Test]
    public void ShouldLeaveCulture_WhenBrowserLanguageIsUnknown()
    {
        var before = CultureInfo.CurrentCulture;
        ClientDateFormatter.ApplyBrowserCulture("!!!");
        CultureInfo.CurrentCulture.ShouldBe(before);
    }

    [Test]
    public void ShouldFormatEnGbShortDate_WhenBrowserLanguageIsEnGb()
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        var previousDefault = CultureInfo.DefaultThreadCurrentCulture;
        var previousDefaultUi = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            ClientDateFormatter.ApplyBrowserCulture("en-GB");
            ClientDateFormatter.Format(Due).ShouldBe("23/10/2026");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.DefaultThreadCurrentCulture = previousDefault;
            CultureInfo.DefaultThreadCurrentUICulture = previousDefaultUi;
        }
    }

    [Test]
    public void ShouldFormatEnGbShortDate_WhenBrowserLanguageHasExtraSubtag()
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        var previousDefault = CultureInfo.DefaultThreadCurrentCulture;
        var previousDefaultUi = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            ClientDateFormatter.ApplyBrowserCulture("en-GB-x-private");
            ClientDateFormatter.Format(Due).ShouldBe("23/10/2026");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.DefaultThreadCurrentCulture = previousDefault;
            CultureInfo.DefaultThreadCurrentUICulture = previousDefaultUi;
        }
    }

    private static async Task AssertDueDateAsync(string expectedDue)
    {
        var culture = CultureInfo.CurrentCulture;
        var order = SampleWorkOrder();

        await using var search = CreateSearchContext(order);
        var list = search.Render<WorkOrderSearch>();
        var cells = list.FindAll($"[data-testid='{WorkOrderSearch.Elements.DueDateCell}{order.Number}']");
        cells.Count.ShouldBeGreaterThan(0);
        foreach (var cell in cells)
        {
            cell.TextContent.ShouldContain(expectedDue);
        }

        await using var manage = CreateManageContext(order);
        var page = manage.Render<WorkOrderManage>();
        var due = await page.WaitForElementAsync($"[data-testid='{WorkOrderManage.Elements.DueDateDisplay}']");
        due.TextContent.ShouldContain(expectedDue);
        var created = page.Find($"[data-testid='{WorkOrderManage.Elements.CreatedDate}']");
        created.TextContent.Trim().ShouldBe(Created.ToString("G", culture));
    }

    private static WorkOrder SampleWorkOrder() => new()
    {
        Id = Guid.NewGuid(),
        Number = "WO-603",
        Title = "Culture dates",
        Description = "Dates follow the browser culture",
        Status = WorkOrderStatus.Complete,
        Creator = Creator,
        CreatedDate = Created,
        DueDate = Due
    };

    private static BunitContext CreateSearchContext(WorkOrder order)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var bus = new StubBus([order]);
        var uiBus = new StubUiBus();
        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(null));
        ctx.Services.AddSingleton<IUiBus>(uiBus);
        ctx.Services.AddSingleton(new WorkOrderBulkActionService(bus, uiBus));
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton(new WorkOrderSearchState());
        ctx.Services.AddSingleton<IFeatureFlags>(new StubFeatureFlags());

        var store = new StubUserSessionStore { Username = "jpalermo" };
        var authProvider = new CustomAuthenticationStateProvider(store);
        authProvider.Login("jpalermo").GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);
        return ctx;
    }

    private static BunitContext CreateManageContext(WorkOrder order)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new ManageBus(order));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(Creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "edit"));
        return ctx;
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture;
        private readonly CultureInfo _ui;
        private readonly CultureInfo? _defaultCulture;
        private readonly CultureInfo? _defaultUi;

        public CultureScope(string name)
        {
            _culture = CultureInfo.CurrentCulture;
            _ui = CultureInfo.CurrentUICulture;
            _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
            _defaultUi = CultureInfo.DefaultThreadCurrentUICulture;
            var culture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _ui;
            CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _defaultUi;
        }
    }

    private sealed class ManageBus(WorkOrder workOrder) : Bus(null!)
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

    private sealed class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string languageCode) => Task.FromResult(text);
    }
}
