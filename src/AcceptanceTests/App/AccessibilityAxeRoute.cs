using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using HomeIndex = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>How <see cref="AccessibilityAxeTests"/> waits for a route before scanning.</summary>
public enum AccessibilityAxeReadyKind
{
    /// <summary>Wait for a <c>data-testid</c>.</summary>
    TestId,

    /// <summary>Wait until present selector exists and absent selector is gone.</summary>
    PresentAbsent
}

/// <summary>One axe scan target.</summary>
public sealed class AccessibilityAxeRoute
{
    /// <summary>Stable case key.</summary>
    public required string Key { get; init; }

    /// <summary>URL or template with <c>{number}</c>.</summary>
    public required string UrlTemplate { get; init; }

    /// <summary>When true, sign in before scanning.</summary>
    public bool RequiresSignIn { get; init; }

    /// <summary>When true, seed a work order and substitute <c>{number}</c>.</summary>
    public bool NeedsWorkOrderNumber { get; init; }

    /// <summary>Ready-wait strategy.</summary>
    public AccessibilityAxeReadyKind ReadyKind { get; init; }

    /// <summary>Test id, or <c>present|absent</c> for PresentAbsent.</summary>
    public required string ReadyToken { get; init; }
}

/// <summary>Route catalog for per-route axe cases (~12–14 routes).</summary>
public static class AccessibilityAxeRoutes
{
    /// <summary>Every route scanned for serious/critical axe violations.</summary>
    public static IReadOnlyList<AccessibilityAxeRoute> All { get; } =
    [
        new()
        {
            Key = "login", UrlTemplate = "/login", ReadyKind = AccessibilityAxeReadyKind.TestId,
            ReadyToken = nameof(Login.Elements.User)
        },
        new()
        {
            Key = "home", UrlTemplate = "/", RequiresSignIn = true, ReadyKind = AccessibilityAxeReadyKind.TestId,
            ReadyToken = nameof(HomeIndex.Elements.StatusDashboard)
        },
        new()
        {
            Key = "not-found", UrlTemplate = "/not-found", RequiresSignIn = true,
            ReadyKind = AccessibilityAxeReadyKind.TestId, ReadyToken = nameof(NotFound.Elements.NotFoundHomeLink)
        },
        new()
        {
            Key = "client-health", UrlTemplate = "/_clienthealthcheck", RequiresSignIn = true,
            ReadyKind = AccessibilityAxeReadyKind.TestId, ReadyToken = nameof(ClientHealthCheck.Elements.Status)
        },
        new()
        {
            Key = "client-health-detailed", UrlTemplate = "/_clienthealthcheck/detailed", RequiresSignIn = true,
            ReadyKind = AccessibilityAxeReadyKind.TestId, ReadyToken = nameof(DetailedClientHealthCheck.Elements.Status)
        },
        new()
        {
            Key = "settings", UrlTemplate = "/settings", RequiresSignIn = true,
            ReadyKind = AccessibilityAxeReadyKind.TestId, ReadyToken = nameof(Settings.Elements.DarkModeSwitch)
        },
        new()
        {
            Key = "search", UrlTemplate = "/workorder/search", RequiresSignIn = true,
            ReadyKind = AccessibilityAxeReadyKind.TestId, ReadyToken = nameof(WorkOrderSearch.Elements.ResultsCountPill)
        },
        new()
        {
            Key = "manage-new", UrlTemplate = "/workorder/manage?mode=New", RequiresSignIn = true,
            ReadyKind = AccessibilityAxeReadyKind.TestId, ReadyToken = nameof(WorkOrderManage.Elements.WorkOrderNumber)
        },
        new()
        {
            Key = "manage-edit", UrlTemplate = "/workorder/manage/{number}?mode=Edit", RequiresSignIn = true,
            NeedsWorkOrderNumber = true, ReadyKind = AccessibilityAxeReadyKind.TestId,
            ReadyToken = nameof(WorkOrderManage.Elements.WorkOrderNumber)
        },
        new()
        {
            Key = "print", UrlTemplate = "/workorder/print/{number}", RequiresSignIn = true,
            NeedsWorkOrderNumber = true, ReadyKind = AccessibilityAxeReadyKind.TestId,
            ReadyToken = nameof(WorkOrderPrint.Elements.Number)
        },
        new()
        {
            Key = "buildings", UrlTemplate = "/admin/buildings", RequiresSignIn = true,
            ReadyKind = AccessibilityAxeReadyKind.PresentAbsent, ReadyToken = ".buildings-header|.buildings-loading"
        },
        new()
        {
            Key = "teams", UrlTemplate = "/admin/teams", RequiresSignIn = true,
            ReadyKind = AccessibilityAxeReadyKind.PresentAbsent, ReadyToken = ".teams-header|.teams-loading"
        },
        new()
        {
            Key = "reports", UrlTemplate = "/reports", RequiresSignIn = true,
            ReadyKind = AccessibilityAxeReadyKind.TestId, ReadyToken = nameof(Reports.Elements.SummaryCardStatus)
        },
        new()
        {
            Key = "ai-agent", UrlTemplate = "/ai-agent", RequiresSignIn = true,
            ReadyKind = AccessibilityAxeReadyKind.TestId, ReadyToken = nameof(ApplicationChat.Elements.ChatShell)
        }
    ];

    /// <summary>Resolves <paramref name="key"/> or throws.</summary>
    public static AccessibilityAxeRoute ByKey(string key) =>
        All.FirstOrDefault(route => route.Key == key)
        ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown axe route key.");
}
