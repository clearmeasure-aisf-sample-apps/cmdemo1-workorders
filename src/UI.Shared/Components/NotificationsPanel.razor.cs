using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

public partial class NotificationsPanel : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public NotificationsSummary? Data { get; set; }

    [Parameter]
    public ElementReference BellButtonRef { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private TimeProvider Clock { get; set; } = null!;

    private DotNetObjectReference<NotificationsPanel>? _dotNetRef;
    private NotificationsPanelScript? _script;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen && _dotNetRef == null)
        {
            _script ??= new NotificationsPanelScript(Js);
            _dotNetRef = DotNetObjectReference.Create(this);
            await _script.SetupEscapeHandlerAsync(_dotNetRef);
        }
    }

    [JSInvokable]
    public async Task HandleEscapeKey()
    {
        await CloseAsync();
    }

    private async Task CloseAsync()
    {
        await OnClose.InvokeAsync();
        await (_script ??= new NotificationsPanelScript(Js)).FocusAsync(BellButtonRef);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_script is not null)
            await _script.DisposeAsync();

        _dotNetRef?.Dispose();
    }

    private string GetDueUrgency(WorkOrder order)
    {
        if (order.DueDate == null)
            return "none";

        var today = ChurchTimeZone.Today(Clock);
        if (order.DueDate < today)
            return "overdue";
        if (order.DueDate == today)
            return "due-today";
        return "upcoming";
    }

    private string GetDueDateText(DateOnly? dueDate)
    {
        if (dueDate == null)
            return "No due date";

        var today = ChurchTimeZone.Today(Clock);
        if (dueDate == today)
            return "Due today";
        if (dueDate < today)
            return "Overdue";

        var daysUntil = dueDate.Value.DayNumber - today.DayNumber;
        if (daysUntil == 1)
            return "Due tomorrow";

        return dueDate.Value.ToString("M/d");
    }
}
