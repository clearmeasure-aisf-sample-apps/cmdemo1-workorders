using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

public partial class WorkOrderPrint : AppComponentBase
{
    private WorkOrder? _workOrder;
    private WorkOrderAttachment[] _attachments = [];
    private bool _loaded;

    [Parameter] public string? Number { get; set; }

    [Inject] private TimeProvider Clock { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        _workOrder = string.IsNullOrWhiteSpace(Number)
            ? null
            : await Bus.Send(new WorkOrderByNumberQuery(Number));
        _attachments = _workOrder == null
            ? []
            : await Bus.Send(new WorkOrderAttachmentsQuery(_workOrder.Id)) ?? [];
        _loaded = true;
    }

    private async Task PrintAsync() => await Js.InvokeVoidAsync("window.print");

    private static string Or(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value;

    private static string ChurchTime(DateTime? utc)
    {
        if (utc is null) return "None";
        var value = DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(value, ChurchTimeZone.Chicago).ToString("yyyy-MM-dd h:mm tt");
    }

    private string DueDateText
    {
        get
        {
            if (_workOrder?.DueDate is null) return "None";
            var text = _workOrder.DueDate.Value.ToString("yyyy-MM-dd");
            var urgency = DueDateUrgencyCalculator.ScreenReaderText(
                DueDateUrgencyCalculator.Calculate(_workOrder, Clock));
            return string.IsNullOrEmpty(urgency) ? text : $"{text} ({urgency})";
        }
    }

    public enum Elements
    {
        NotFound,
        PrintButton,
        Number,
        Title,
        Status,
        Priority,
        Building,
        RoomNumber,
        DueDate,
        Creator,
        Assignee,
        CreatedDate,
        AssignedDate,
        CompletedDate,
        Description,
        Instructions,
        Attachments,
        AttachmentsNone,
        AttachmentFileName
    }
}
