using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using HomePage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>How <see cref="PhoneWidthNoDocumentScrollTests"/> waits for a route.</summary>
public enum PhoneWidthReadyKind
{
    /// <summary>Wait for a <c>data-testid</c>.</summary>
    TestId,

    /// <summary>Wait for a CSS selector.</summary>
    Css,

    /// <summary>Wait for an accessible heading name.</summary>
    Heading
}

/// <summary>One 390px width assertion target.</summary>
public sealed class PhoneWidthRoute
{
    /// <summary>Stable case key.</summary>
    public required string Key { get; init; }

    /// <summary>URL or template with <c>{number}</c>.</summary>
    public required string UrlTemplate { get; init; }

    /// <summary>When true, sign in before asserting.</summary>
    public bool RequiresSignIn { get; init; }

    /// <summary>When true, seed a work order and substitute <c>{number}</c>.</summary>
    public bool NeedsWorkOrderNumber { get; init; }

    /// <summary>When true, switch to dark theme before asserting.</summary>
    public bool UseDarkTheme { get; init; }

    /// <summary>Ready-wait strategy.</summary>
    public PhoneWidthReadyKind ReadyKind { get; init; }

    /// <summary>Test id, CSS selector, or heading name.</summary>
    public required string ReadyToken { get; init; }

    /// <summary>Optional region that must scroll inside itself.</summary>
    public string? InnerScrollSelector { get; init; }

    /// <summary>When true, assert the top bar wraps at 390px.</summary>
    public bool AssertTopBarWraps { get; init; }
}

/// <summary>Route catalog for per-route 390px width cases.</summary>
public static class PhoneWidthRoutes
{
    /// <summary>Every route checked at 390px (light, plus dark re-checks where layout differs).</summary>
    public static IReadOnlyList<PhoneWidthRoute> All { get; } =
    [
        new()
        {
            Key = "login", UrlTemplate = "/login", ReadyKind = PhoneWidthReadyKind.TestId,
            ReadyToken = nameof(Login.Elements.LoginHint)
        },
        new()
        {
            Key = "home", UrlTemplate = "/", RequiresSignIn = true, ReadyKind = PhoneWidthReadyKind.TestId,
            ReadyToken = nameof(HomePage.Elements.HomeTagline)
        },
        new()
        {
            Key = "search", UrlTemplate = "/workorder/search", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.Css, ReadyToken = ".table-scroll",
            InnerScrollSelector = ".table-scroll", AssertTopBarWraps = true
        },
        new()
        {
            Key = "search-board", UrlTemplate = "/workorder/search?View=Board", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.Css, ReadyToken = ".board-scroll",
            InnerScrollSelector = ".board-scroll"
        },
        new()
        {
            Key = "manage-new", UrlTemplate = "/workorder/manage?mode=New", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.TestId, ReadyToken = nameof(WorkOrderManage.Elements.Title)
        },
        new()
        {
            Key = "print", UrlTemplate = "/workorder/print/{number}", RequiresSignIn = true,
            NeedsWorkOrderNumber = true, ReadyKind = PhoneWidthReadyKind.TestId,
            ReadyToken = nameof(WorkOrderPrint.Elements.Number)
        },
        new()
        {
            Key = "reports", UrlTemplate = "/reports", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.TestId, ReadyToken = nameof(Reports.Elements.ChartCompletedPerWeek),
            InnerScrollSelector = ".chart-container"
        },
        new()
        {
            Key = "buildings", UrlTemplate = "/admin/buildings", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.Heading, ReadyToken = "Buildings & Rooms"
        },
        new()
        {
            Key = "teams", UrlTemplate = "/admin/teams", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.Heading, ReadyToken = "Teams & Vendors",
            InnerScrollSelector = ".teams-page .table-scroll"
        },
        new()
        {
            Key = "ai-agent", UrlTemplate = "/ai-agent", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.TestId, ReadyToken = nameof(ApplicationChat.Elements.ChatShell)
        },
        new()
        {
            Key = "settings", UrlTemplate = "/settings", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.TestId, ReadyToken = nameof(Settings.Elements.SettingsSubtitle)
        },
        new()
        {
            Key = "counter", UrlTemplate = "/counter", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.TestId, ReadyToken = nameof(NotFound.Elements.NotFoundHomeLink)
        },
        new()
        {
            Key = "fetchdata", UrlTemplate = "/fetchdata", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.TestId, ReadyToken = nameof(NotFound.Elements.NotFoundHomeLink)
        },
        new()
        {
            Key = "not-found", UrlTemplate = "/not-found", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.TestId, ReadyToken = nameof(NotFound.Elements.NotFoundHomeLink)
        },
        new()
        {
            Key = "client-health", UrlTemplate = "/_clienthealthcheck", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.TestId, ReadyToken = nameof(ClientHealthCheck.Elements.Status)
        },
        new()
        {
            Key = "client-health-detailed", UrlTemplate = "/_clienthealthcheck/detailed", RequiresSignIn = true,
            ReadyKind = PhoneWidthReadyKind.TestId, ReadyToken = nameof(DetailedClientHealthCheck.Elements.Status)
        },
        new()
        {
            Key = "search-dark", UrlTemplate = "/workorder/search", RequiresSignIn = true, UseDarkTheme = true,
            ReadyKind = PhoneWidthReadyKind.Css, ReadyToken = ".table-scroll"
        },
        new()
        {
            Key = "search-board-dark", UrlTemplate = "/workorder/search?View=Board", RequiresSignIn = true,
            UseDarkTheme = true, ReadyKind = PhoneWidthReadyKind.Css, ReadyToken = ".board-scroll"
        },
        new()
        {
            Key = "reports-dark", UrlTemplate = "/reports", RequiresSignIn = true, UseDarkTheme = true,
            ReadyKind = PhoneWidthReadyKind.TestId, ReadyToken = nameof(Reports.Elements.ChartCompletedPerWeek)
        }
    ];

    /// <summary>Resolves <paramref name="key"/> or throws.</summary>
    public static PhoneWidthRoute ByKey(string key) =>
        All.FirstOrDefault(route => route.Key == key)
        ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown phone-width route key.");
}
