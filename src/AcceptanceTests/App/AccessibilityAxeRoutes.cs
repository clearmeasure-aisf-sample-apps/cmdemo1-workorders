using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using HomeIndex = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>Route catalog for per-route axe cases (~12–14 routes).</summary>
public static class AccessibilityAxeRoutes
{
    /// <summary>Every route scanned for serious/critical axe violations.</summary>
    private static IReadOnlyList<AccessibilityAxeRoute> All { get; } =
    [
        new()
        {
            Key = "login", UrlTemplate = "/login", WaitKind = AccessibilityAxeRoute.ReadyKind.TestId,
            ReadyToken = nameof(Login.Elements.User)
        },
        new()
        {
            Key = "home", UrlTemplate = "/", RequiresSignIn = true, WaitKind = AccessibilityAxeRoute.ReadyKind.TestId,
            ReadyToken = nameof(HomeIndex.Elements.StatusDashboard)
        },
        new()
        {
            Key = "not-found", UrlTemplate = "/not-found", RequiresSignIn = true,
            WaitKind = AccessibilityAxeRoute.ReadyKind.TestId, ReadyToken = nameof(NotFound.Elements.NotFoundHomeLink)
        },
        new()
        {
            Key = "client-health", UrlTemplate = "/_clienthealthcheck", RequiresSignIn = true,
            WaitKind = AccessibilityAxeRoute.ReadyKind.TestId, ReadyToken = nameof(ClientHealthCheck.Elements.Status)
        },
        new()
        {
            Key = "client-health-detailed", UrlTemplate = "/_clienthealthcheck/detailed", RequiresSignIn = true,
            WaitKind = AccessibilityAxeRoute.ReadyKind.TestId, ReadyToken = nameof(DetailedClientHealthCheck.Elements.Status)
        },
        new()
        {
            Key = "settings", UrlTemplate = "/settings", RequiresSignIn = true,
            WaitKind = AccessibilityAxeRoute.ReadyKind.TestId, ReadyToken = nameof(Settings.Elements.DarkModeSwitch)
        },
        new()
        {
            Key = "search", UrlTemplate = "/workorder/search", RequiresSignIn = true,
            WaitKind = AccessibilityAxeRoute.ReadyKind.TestId, ReadyToken = nameof(WorkOrderSearch.Elements.ResultsCountPill)
        },
        new()
        {
            Key = "manage-new", UrlTemplate = "/workorder/manage?mode=New", RequiresSignIn = true,
            WaitKind = AccessibilityAxeRoute.ReadyKind.TestId, ReadyToken = nameof(WorkOrderManage.Elements.WorkOrderNumber)
        },
        new()
        {
            Key = "manage-edit", UrlTemplate = "/workorder/manage/{number}?mode=Edit", RequiresSignIn = true,
            NeedsWorkOrderNumber = true, WaitKind = AccessibilityAxeRoute.ReadyKind.TestId,
            ReadyToken = nameof(WorkOrderManage.Elements.WorkOrderNumber)
        },
        new()
        {
            Key = "print", UrlTemplate = "/workorder/print/{number}", RequiresSignIn = true,
            NeedsWorkOrderNumber = true, WaitKind = AccessibilityAxeRoute.ReadyKind.TestId,
            ReadyToken = nameof(WorkOrderPrint.Elements.Number)
        },
        new()
        {
            Key = "buildings", UrlTemplate = "/admin/buildings", RequiresSignIn = true,
            WaitKind = AccessibilityAxeRoute.ReadyKind.PresentAbsent, ReadyToken = ".buildings-header|.buildings-loading"
        },
        new()
        {
            Key = "teams", UrlTemplate = "/admin/teams", RequiresSignIn = true,
            WaitKind = AccessibilityAxeRoute.ReadyKind.PresentAbsent, ReadyToken = ".teams-header|.teams-loading"
        },
        new()
        {
            Key = "reports", UrlTemplate = "/reports", RequiresSignIn = true,
            WaitKind = AccessibilityAxeRoute.ReadyKind.TestId, ReadyToken = nameof(Reports.Elements.SummaryCardStatus)
        },
        new()
        {
            Key = "ai-agent", UrlTemplate = "/ai-agent", RequiresSignIn = true,
            WaitKind = AccessibilityAxeRoute.ReadyKind.TestId, ReadyToken = nameof(ApplicationChat.Elements.ChatShell)
        }
    ];

    /// <summary>Resolves <paramref name="key"/> or throws.</summary>
    public static AccessibilityAxeRoute ByKey(string key) =>
        All.FirstOrDefault(route => route.Key == key)
        ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown axe route key.");
}
