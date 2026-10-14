using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

public partial class Reports : AppComponentBase
{
    [Inject] private TimeProvider Clock { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    public WorkOrderReportsData? Data { get; private set; }

    public enum Elements
    {
        ReportsTitle,
        ReportsSubtitle,
        ExportButton,
        SummaryCardStatus,
        SummaryCardPriority,
        SummaryCardBuilding,
        SummaryCardCompletedLast30,
        ChartCompletedPerWeek,
        KpiOverdue
    }

    protected override async Task OnInitializedAsync()
    {
        Data = await Bus.Send(new WorkOrderReportsQuery());
    }

    private async Task ExportAsync()
    {
        if (Data == null)
        {
            return;
        }

        var csv = WorkOrderReportsExporter.ToCsv(Data);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        var fileName = $"reports-{DateOnly.FromDateTime(Clock.GetUtcNow().DateTime):yyyyMMdd}.csv";

        await JS.InvokeVoidAsync("searchExport.download", fileName, bytes);
    }
}
