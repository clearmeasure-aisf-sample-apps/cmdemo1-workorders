using AngleSharp.Dom;
using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

/// <summary>
/// DOM queries and page actions for work-order search bUnit tests. No assertions.
/// </summary>
public static class WorkOrderSearchSteps
{
    /// <summary>Default signed-in user name (same as <see cref="WorkOrderSearchPageBuilder.DefaultUserName"/>).</summary>
    private const string DefaultUserName = WorkOrderSearchPageBuilder.DefaultUserName;

    /// <summary>CSS selector for a data-testid built from an Elements enum value.</summary>
    public static string TestId(WorkOrderSearchPage.Elements element, string suffix = "") =>
        $"[data-testid='{element}{suffix}']";

    /// <summary>Work-order numbers from table link test ids.</summary>
    public static string[] LinkNumbers(IRenderedComponent<WorkOrderSearchPage> component) =>
        component.FindAll($"[data-testid^='{WorkOrderSearchPage.Elements.WorkOrderLink}']")
            .Select(a => a.GetAttribute("data-testid")![nameof(WorkOrderSearchPage.Elements.WorkOrderLink).Length..])
            .ToArray();

    /// <summary>Active filter chip keys from data-testid suffixes.</summary>
    public static string[] ChipKeys(IRenderedComponent<WorkOrderSearchPage> component) =>
        component.FindAll($"[data-testid^='{WorkOrderSearchPage.Elements.ActiveFilterChip}']")
            .Select(c => c.GetAttribute("data-testid")![nameof(WorkOrderSearchPage.Elements.ActiveFilterChip).Length..])
            .ToArray();

    /// <summary>Board card numbers under the board view root.</summary>
    public static string[] BoardCardNumbers(IRenderedComponent<WorkOrderSearchPage> component) =>
        CardNumbers(component.Find($"[data-testid='{WorkOrderSearchPage.Elements.BoardView}']"));

    /// <summary>Board card numbers in the Draft column.</summary>
    public static string[] DraftColumnNumbers(IRenderedComponent<WorkOrderSearchPage> component) =>
        CardNumbers(component.Find($"[data-testid='{WorkOrderSearchPage.Elements.BoardColumn}Draft']"));

    /// <summary>Board card numbers under a column or board container.</summary>
    public static string[] CardNumbers(IElement container) =>
        container.QuerySelectorAll($"[data-testid^='{WorkOrderSearchPage.Elements.BoardCard}']")
            .Select(a => a.GetAttribute("data-testid")![nameof(WorkOrderSearchPage.Elements.BoardCard).Length..])
            .ToArray();

    /// <summary>Finds a filter control by Elements id.</summary>
    public static IElement FindById(IRenderedComponent<WorkOrderSearchPage> component, WorkOrderSearchPage.Elements element) =>
        component.Find($"#{element}");

    /// <summary>Changes a select or input identified by Elements.</summary>
    public static Task ChangeAsync(
        IRenderedComponent<WorkOrderSearchPage> component,
        WorkOrderSearchPage.Elements element,
        object value) =>
        FindById(component, element).ChangeAsync(new ChangeEventArgs { Value = value });

    /// <summary>Clicks an element by CSS selector.</summary>
    public static Task ClickAsync(IRenderedComponent<WorkOrderSearchPage> component, string cssSelector) =>
        component.Find(cssSelector).ClickAsync(new MouseEventArgs());

    /// <summary>Clicks a data-testid Elements value (optional suffix).</summary>
    public static Task ClickTestIdAsync(
        IRenderedComponent<WorkOrderSearchPage> component,
        WorkOrderSearchPage.Elements element,
        string suffix = "") =>
        ClickAsync(component, TestId(element, suffix));

    /// <summary>Opens the Saved Views menu.</summary>
    public static Task OpenSavedViewsMenuAsync(IRenderedComponent<WorkOrderSearchPage> component) =>
        ClickTestIdAsync(component, WorkOrderSearchPage.Elements.SavedViewsButton);

    /// <summary>Clicks Save current view inside the open menu.</summary>
    public static Task ClickSaveCurrentViewAsync(IRenderedComponent<WorkOrderSearchPage> component) =>
        ClickTestIdAsync(component, WorkOrderSearchPage.Elements.SaveCurrentViewButton);

    /// <summary>Types a name into the saved-view name input.</summary>
    public static Task InputSavedViewNameAsync(IRenderedComponent<WorkOrderSearchPage> component, string name) =>
        component.Find(TestId(WorkOrderSearchPage.Elements.SavedViewNameInput))
            .InputAsync(new ChangeEventArgs { Value = name });

    /// <summary>Submits the save-view form.</summary>
    public static Task SubmitSaveViewFormAsync(IRenderedComponent<WorkOrderSearchPage> component) =>
        component.Find("form.save-view-form").SubmitAsync();

    /// <summary>Presses Escape on the nearest menu anchor.</summary>
    public static Task PressEscapeOnMenuAsync(IRenderedComponent<WorkOrderSearchPage> component, string menuAnchorCss) =>
        component.Find(menuAnchorCss).KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

    /// <summary>Sets creator, assignee, and status filters used by clear-filters tests.</summary>
    public static async Task SetCreatorAssigneeStatusFiltersAsync(IRenderedComponent<WorkOrderSearchPage> component)
    {
        await ChangeAsync(component, WorkOrderSearchPage.Elements.CreatorSelect, DefaultUserName);
        await ChangeAsync(component, WorkOrderSearchPage.Elements.AssigneeSelect, "hsimpson");
        await ChangeAsync(component, WorkOrderSearchPage.Elements.StatusSelect, WorkOrderStatus.InProgress.Key);
    }

    /// <summary>Navigates the bUnit host to a relative URI.</summary>
    public static void NavigateTo(BunitContext ctx, string uri) =>
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

    /// <summary>Registers the searchExport.js module stubs used by export tests.</summary>
    public static BunitJSModuleInterop SetupSearchExportModule(BunitContext ctx)
    {
        var module = ctx.JSInterop.SetupModule("./_content/ClearMeasure.Bootcamp.UI.Shared/js/searchExport.js");
        module.SetupVoid("downloadCsv", _ => true).SetVoidResult();
        module.SetupVoid("setIndeterminate", _ => true).SetVoidResult();
        return module;
    }

    /// <summary>Completes a held specification search with a single work order.</summary>
    public static void CompleteHeldSearch(StubHeldSearchBus bus, int index, WorkOrder order) =>
        bus.Held[index].SetResult([order]);

    /// <summary>Builds a creator employee who can create work orders for the default user.</summary>
    public static Employee CreatorWhoCanCreate() =>
        EmployeeBuilder.Create(DefaultUserName)
            .WithFirstName("Jeffrey")
            .WithLastName("Palermo")
            .WithEmail("j@example.com")
            .WithRole("creator", canCreate: true, canFulfill: false)
            .Build();

    /// <summary>Builds three draft work orders titled A/B/C for sort tests.</summary>
    public static WorkOrder[] DraftsTitledAbc() =>
    [
        WorkOrderBuilder.Create().WithNumber("WO-003").WithTitle("C").WithStatus(WorkOrderStatus.Draft).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-001").WithTitle("A").WithStatus(WorkOrderStatus.Draft).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-002").WithTitle("B").WithStatus(WorkOrderStatus.Draft).Build()
    ];

    /// <summary>Builds three work orders spanning Assigned/Draft/InProgress for status sort.</summary>
    public static WorkOrder[] OrdersForStatusSort() =>
    [
        WorkOrderBuilder.Create().WithNumber("WO-003").WithTitle("C").WithStatus(WorkOrderStatus.InProgress).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-001").WithTitle("A").WithStatus(WorkOrderStatus.Assigned).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-002").WithTitle("B").WithStatus(WorkOrderStatus.Draft).Build()
    ];

    /// <summary>Builds three drafts with due dates for due-date sort (nulls last).</summary>
    public static WorkOrder[] OrdersForDueDateSort() =>
    [
        WorkOrderBuilder.Create().WithNumber("WO-003").WithTitle("C").WithStatus(WorkOrderStatus.Draft).DueOn(null).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-001").WithTitle("A").WithStatus(WorkOrderStatus.Draft)
            .DueOn(new DateOnly(2025, 6, 1)).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-002").WithTitle("B").WithStatus(WorkOrderStatus.Draft)
            .DueOn(new DateOnly(2025, 3, 1)).Build()
    ];

    /// <summary>Builds three drafts with rooms A/B/C for room sort.</summary>
    public static WorkOrder[] OrdersForRoomSort() =>
    [
        WorkOrderBuilder.Create().WithNumber("WO-003").WithTitle("C").WithStatus(WorkOrderStatus.Draft).InRoom("C").Build(),
        WorkOrderBuilder.Create().WithNumber("WO-001").WithTitle("A").WithStatus(WorkOrderStatus.Draft).InRoom("A").Build(),
        WorkOrderBuilder.Create().WithNumber("WO-002").WithTitle("B").WithStatus(WorkOrderStatus.Draft).InRoom("B").Build()
    ];

    /// <summary>Builds three drafts spanning Low/High/Medium priority for sort-select tests.</summary>
    public static WorkOrder[] OrdersForPrioritySort() =>
    [
        WorkOrderBuilder.Create().WithNumber("WO-L").WithTitle("L").WithStatus(WorkOrderStatus.Draft)
            .WithPriority(WorkOrderPriority.Low).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-H").WithTitle("H").WithStatus(WorkOrderStatus.Draft)
            .WithPriority(WorkOrderPriority.High).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-M").WithTitle("M").WithStatus(WorkOrderStatus.Draft)
            .WithPriority(WorkOrderPriority.Medium).Build()
    ];

    /// <summary>Builds orders used when switching sort column resets direction.</summary>
    public static WorkOrder[] OrdersForColumnSwitchSort() =>
    [
        WorkOrderBuilder.Create().WithNumber("WO-003").WithTitle("C").WithStatus(WorkOrderStatus.InProgress)
            .DueOn(new DateOnly(2025, 6, 1)).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-001").WithTitle("A").WithStatus(WorkOrderStatus.Assigned)
            .DueOn(new DateOnly(2025, 3, 1)).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-002").WithTitle("B").WithStatus(WorkOrderStatus.Draft)
            .DueOn(new DateOnly(2025, 9, 1)).Build()
    ];

    /// <summary>Builds the newest race-winner row for held-search tests.</summary>
    public static WorkOrder NewestAssignedOrder() =>
        WorkOrderBuilder.Create().WithNumber("NEWEST1").WithTitle("Newest").WithStatus(WorkOrderStatus.Assigned).Build();

    /// <summary>Builds the stale race-loser row for held-search tests.</summary>
    public static WorkOrder StaleDraftOrder() =>
        WorkOrderBuilder.Create().WithNumber("STALE01").WithTitle("Stale").WithStatus(WorkOrderStatus.Draft).Build();

    /// <summary>Builds matching and non-matching rows for CSV export filter tests.</summary>
    public static (WorkOrder Matching, WorkOrder Other) ExportFilterPair() =>
    (
        WorkOrderBuilder.Create().WithNumber("WO-KEEP").WithTitle("Say \"hi\", now").WithStatus(WorkOrderStatus.Assigned).Build(),
        WorkOrderBuilder.Create().WithNumber("WO-DROP").WithTitle("Leave this out").WithStatus(WorkOrderStatus.Draft).Build()
    );

    /// <summary>Builds a draft owned by <paramref name="creator"/> for bulk assign.</summary>
    public static WorkOrder DraftOwnedBy(Employee creator) =>
        WorkOrderBuilder.Create().WithNumber("WO-D1").WithTitle("Draft").WithStatus(WorkOrderStatus.Draft)
            .CreatedBy(creator).Build();

    /// <summary>Builds InProgress rows with and without WaitingOnVendor for board tests.</summary>
    public static WorkOrder[] WaitingAndWorkingInProgress() =>
    [
        WorkOrderBuilder.Create().WithNumber("WV001").WithTitle("Waiting").WithStatus(WorkOrderStatus.InProgress)
            .WaitingOnVendorSince(new DateTime(2026, 10, 14, 9, 0, 0)).Build(),
        WorkOrderBuilder.Create().WithNumber("WV002").WithTitle("Working").WithStatus(WorkOrderStatus.InProgress).Build()
    ];
}
