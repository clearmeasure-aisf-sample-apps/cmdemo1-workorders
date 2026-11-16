using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Palermo.BlazorMvc;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

/// <summary>
/// Fluent bUnit host for <see cref="ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage"/> page tests.
/// </summary>
public sealed class WorkOrderManagePageContext
{
    private Employee? _user;
    private StubUserSession? _session;
    private WorkOrder? _editing;
    private string? _navigateUri;
    private TimeProvider _clock = TimeProvider.System;
    private IUiBus? _uiBus;
    private IJSRuntime? _jsRuntime;
    private bool _looseJs;
    private bool _captureStateCommands;
    private Task? _commandGate;
    private bool _captureRemoveAttachment;
    private WorkOrderAttachment[] _attachments = [];
    private WorkOrderStatusChange[] _history = [];
    private RemoveAttachmentResult _removeResult = RemoveAttachmentResult.Success;
    private readonly WorkOrderManageNewOrderSource _newOrder = new();
    private readonly WorkOrderManageTranslation _translation = new();
    private WorkOrderManageBus? _bus;

    /// <summary>Starts a page context with default DI wiring.</summary>
    public static WorkOrderManagePageContext Create() => new();

    /// <summary>Configurable bus used after <see cref="Build"/>.</summary>
    public WorkOrderManageBus Bus => _bus ?? throw new InvalidOperationException("Call Build() first.");

    /// <summary>Translation fake registered with the host.</summary>
    public WorkOrderManageTranslation Translation => _translation;

    /// <summary>Session fake registered with the host.</summary>
    public StubUserSession Session =>
        _session ?? throw new InvalidOperationException("Call SignedInAs before Build().");

    /// <summary>New-order seed used when the page is not in edit mode.</summary>
    public WorkOrderManageNewOrderSource NewOrder => _newOrder;

    /// <summary>Sets the signed-in employee and session.</summary>
    public WorkOrderManagePageContext SignedInAs(Employee employee, StubUserSession? session = null)
    {
        _user = employee;
        _session = session ?? new StubUserSession(employee);
        return this;
    }

    /// <summary>Loads an existing work order in edit mode.</summary>
    public WorkOrderManagePageContext Editing(WorkOrder workOrder)
    {
        _editing = workOrder;
        return this;
    }

    /// <summary>Captures bus requests of type <typeparamref name="TRequest"/> (state commands or remove attachment).</summary>
    public WorkOrderManagePageContext WhenBusReceives<TRequest>(Task? gate = null)
    {
        if (typeof(TRequest) == typeof(RemoveAttachmentCommand))
        {
            _captureRemoveAttachment = true;
            return this;
        }

        _captureStateCommands = true;
        _commandGate = gate;
        return this;
    }

    /// <summary>Uses the recording translation fake (echo + last-call capture).</summary>
    public WorkOrderManagePageContext TranslationReturns() => this;

    /// <summary>Configures the New-mode work order returned by <see cref="IWorkOrderBuilder"/>.</summary>
    public WorkOrderManagePageContext WithNewOrder(Action<WorkOrderManageNewOrderSource> configure)
    {
        configure(_newOrder);
        return this;
    }

    /// <summary>Seeds attachment rows on the bus.</summary>
    public WorkOrderManagePageContext WithAttachments(params WorkOrderAttachment[] attachments)
    {
        _attachments = attachments;
        return this;
    }

    /// <summary>Sets the remove-attachment result returned by the bus.</summary>
    public WorkOrderManagePageContext WithRemoveResult(RemoveAttachmentResult result)
    {
        _removeResult = result;
        return this;
    }

    /// <summary>Seeds status-history rows on the bus.</summary>
    public WorkOrderManagePageContext WithHistory(params WorkOrderStatusChange[] history)
    {
        _history = history;
        return this;
    }

    /// <summary>Overrides <see cref="TimeProvider"/>.</summary>
    public WorkOrderManagePageContext WithClock(TimeProvider clock)
    {
        _clock = clock;
        return this;
    }

    /// <summary>Overrides the UI event bus (for example a spy).</summary>
    public WorkOrderManagePageContext WithUiBus(IUiBus uiBus)
    {
        _uiBus = uiBus;
        return this;
    }

    /// <summary>Registers a custom <see cref="IJSRuntime"/>.</summary>
    public WorkOrderManagePageContext WithJsRuntime(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        return this;
    }

    /// <summary>Enables loose JS interop (dictation tests).</summary>
    public WorkOrderManagePageContext WithLooseJsInterop()
    {
        _looseJs = true;
        return this;
    }

    /// <summary>Navigates to an explicit URI instead of the default New/Edit query.</summary>
    public WorkOrderManagePageContext NavigateTo(string uri)
    {
        _navigateUri = uri;
        return this;
    }

    /// <summary>Builds a disposable bUnit context with manage-page dependencies.</summary>
    public BunitContext Build()
    {
        var user = _user ?? throw new InvalidOperationException("Call SignedInAs before Build().");
        _session ??= new StubUserSession(user);
        _bus = CreateBus();

        var ctx = new BunitContext();
        if (_looseJs)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        }

        RegisterServices(ctx);
        Navigate(ctx);
        return ctx;
    }

    /// <summary>Renders the manage page, using the edited work-order number when in edit mode.</summary>
    public IRenderedComponent<ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage> Render(BunitContext ctx) =>
        Render(ctx, _editing?.Number);

    /// <summary>Renders the manage page, optionally supplying the route <c>Id</c> parameter.</summary>
    public IRenderedComponent<ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage> Render(
        BunitContext ctx,
        string? id)
    {
        if (id == null)
        {
            return ctx.Render<ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage>();
        }

        return ctx.Render<ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage>(parameters =>
            parameters.Add(p => p.Id, id));
    }

    private WorkOrderManageBus CreateBus()
    {
        var bus = new WorkOrderManageBus();
        if (_editing != null)
        {
            bus.WithExisting(_editing);
        }

        bus.WithAttachments(_attachments).WithHistory(_history);
        bus.RemoveResult = _removeResult;
        if (_captureStateCommands)
        {
            bus.CaptureStateCommands(_commandGate);
        }

        if (_captureRemoveAttachment)
        {
            bus.CaptureRemoveAttachment();
        }

        return bus;
    }

    private void RegisterServices(BunitContext ctx)
    {
        ctx.Services.AddSingleton<IBus>(Bus);
        ctx.Services.AddSingleton(_uiBus ?? new StubUiBus());
        ctx.Services.AddSingleton(_clock);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(_newOrder);
        ctx.Services.AddSingleton<IUserSession>(Session);
        ctx.Services.AddSingleton<ITranslationService>(_translation);
        if (_jsRuntime != null)
        {
            ctx.Services.AddSingleton(_jsRuntime);
        }

        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();
    }

    private void Navigate(BunitContext ctx)
    {
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        if (_navigateUri != null)
        {
            navigation.NavigateTo(_navigateUri);
            return;
        }

        if (_editing != null)
        {
            navigation.NavigateTo($"http://localhost/?Mode=Edit&Id={_editing.Number}");
            return;
        }

        navigation.NavigateTo(navigation.GetUriWithQueryParameter("Mode", "New"));
    }
}
