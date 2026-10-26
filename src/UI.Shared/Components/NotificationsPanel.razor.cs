using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

public partial class NotificationsPanel : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel? Data { get; set; }

    [Parameter]
    public ElementReference BellButtonRef { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    private DotNetObjectReference<NotificationsPanel>? _dotNetRef;
    private IJSObjectReference? _jsModule;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen && _dotNetRef == null)
        {
            try
            {
                _jsModule ??= await Js.InvokeAsync<IJSObjectReference>("import",
                    "./_content/ClearMeasure.Bootcamp.UI.Shared/js/notificationsPanel.js");
                _dotNetRef = DotNetObjectReference.Create(this);
                await _jsModule.InvokeVoidAsync("setupEscapeHandler", _dotNetRef);
            }
            catch (JSDisconnectedException)
            {
            }
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
        try
        {
            await BellButtonRef.FocusAsync();
        }
        catch (InvalidOperationException)
        {
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        try
        {
            if (_jsModule is not null)
            {
                await _jsModule.InvokeVoidAsync("cleanupEscapeHandler");
                await _jsModule.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        _dotNetRef?.Dispose();
    }

    private static string GetDueUrgency(WorkOrder order)
    {
        if (order.DueDate == null)
            return "none";

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (order.DueDate < today)
            return "overdue";
        if (order.DueDate == today)
            return "due-today";
        return "upcoming";
    }

    private static string GetDueDateText(DateOnly? dueDate)
    {
        if (dueDate == null)
            return "No due date";

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dueDate == today)
            return "Due today";
        if (dueDate < today)
            return "Overdue";

        var daysUntil = (dueDate.Value.DayNumber - today.DayNumber);
        if (daysUntil == 1)
            return "Due tomorrow";

        return dueDate.Value.ToString("M/d");
    }
}
