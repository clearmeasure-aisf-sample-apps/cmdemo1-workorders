using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

public partial class NotificationsPanel : ComponentBase
{
    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public global::ClearMeasure.Bootcamp.Core.Queries.NotificationsPanel? Data { get; set; }

    [Parameter]
    public ElementReference BellButtonRef { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    private ElementReference PanelRef { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && IsOpen && PanelRef.Context != null)
        {
            try
            {
                await PanelRef.FocusAsync();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            await CloseAsync();
        }
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
