using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using HomePage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>Route catalog for per-route 390px width cases.</summary>
public static class PhoneWidthRoutes
{
    /// <summary>Every route checked at 390px (light, plus dark re-checks where layout differs).</summary>
    private static IReadOnlyList<PhoneWidthRoute> All { get; } =
    [
        new()
        {
            Key = "login", UrlTemplate = "/login", WaitKind = PhoneWidthRoute.ReadyKind.TestId,
            ReadyToken = nameof(Login.Elements.LoginHint)
        },
        new()
        {
            Key = "home", UrlTemplate = "/", RequiresSignIn = true, WaitKind = PhoneWidthRoute.ReadyKind.TestId,
            ReadyToken = nameof(HomePage.Elements.HomeTagline)
        },
        new()
        {
            Key = "search", UrlTemplate = "/workorder/search", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.Css, ReadyToken = ".table-scroll",
            InnerScrollSelector = ".table-scroll", AssertTopBarWraps = true
        },
        new()
        {
            Key = "search-board", UrlTemplate = "/workorder/search?View=Board", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.Css, ReadyToken = ".board-scroll",
            InnerScrollSelector = ".board-scroll"
        },
        new()
        {
            Key = "manage-new", UrlTemplate = "/workorder/manage?mode=New", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.TestId, ReadyToken = nameof(WorkOrderManage.Elements.Title)
        },
        new()
        {
            Key = "print", UrlTemplate = "/workorder/print/{number}", RequiresSignIn = true,
            NeedsWorkOrderNumber = true, WaitKind = PhoneWidthRoute.ReadyKind.TestId,
            ReadyToken = nameof(WorkOrderPrint.Elements.Number)
        },
        new()
        {
            Key = "reports", UrlTemplate = "/reports", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.TestId, ReadyToken = nameof(Reports.Elements.ChartCompletedPerWeek),
            InnerScrollSelector = ".chart-container"
        },
        new()
        {
            Key = "buildings", UrlTemplate = "/admin/buildings", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.Heading, ReadyToken = "Buildings & Rooms"
        },
        new()
        {
            Key = "teams", UrlTemplate = "/admin/teams", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.Heading, ReadyToken = "Teams & Vendors",
            InnerScrollSelector = ".teams-page .table-scroll"
        },
        new()
        {
            Key = "ai-agent", UrlTemplate = "/ai-agent", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.TestId, ReadyToken = nameof(ApplicationChat.Elements.ChatShell)
        },
        new()
        {
            Key = "settings", UrlTemplate = "/settings", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.TestId, ReadyToken = nameof(Settings.Elements.SettingsSubtitle)
        },
        new()
        {
            Key = "counter", UrlTemplate = "/counter", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.TestId, ReadyToken = nameof(NotFound.Elements.NotFoundHomeLink)
        },
        new()
        {
            Key = "fetchdata", UrlTemplate = "/fetchdata", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.TestId, ReadyToken = nameof(NotFound.Elements.NotFoundHomeLink)
        },
        new()
        {
            Key = "not-found", UrlTemplate = "/not-found", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.TestId, ReadyToken = nameof(NotFound.Elements.NotFoundHomeLink)
        },
        new()
        {
            Key = "client-health", UrlTemplate = "/_clienthealthcheck", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.TestId, ReadyToken = nameof(ClientHealthCheck.Elements.Status)
        },
        new()
        {
            Key = "client-health-detailed", UrlTemplate = "/_clienthealthcheck/detailed", RequiresSignIn = true,
            WaitKind = PhoneWidthRoute.ReadyKind.TestId, ReadyToken = nameof(DetailedClientHealthCheck.Elements.Status)
        },
        new()
        {
            Key = "search-dark", UrlTemplate = "/workorder/search", RequiresSignIn = true, UseDarkTheme = true,
            WaitKind = PhoneWidthRoute.ReadyKind.Css, ReadyToken = ".table-scroll"
        },
        new()
        {
            Key = "search-board-dark", UrlTemplate = "/workorder/search?View=Board", RequiresSignIn = true,
            UseDarkTheme = true, WaitKind = PhoneWidthRoute.ReadyKind.Css, ReadyToken = ".board-scroll"
        },
        new()
        {
            Key = "reports-dark", UrlTemplate = "/reports", RequiresSignIn = true, UseDarkTheme = true,
            WaitKind = PhoneWidthRoute.ReadyKind.TestId, ReadyToken = nameof(Reports.Elements.ChartCompletedPerWeek)
        }
    ];

    /// <summary>Resolves <paramref name="key"/> or throws.</summary>
    public static PhoneWidthRoute ByKey(string key) =>
        All.FirstOrDefault(route => route.Key == key)
        ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown phone-width route key.");
}
