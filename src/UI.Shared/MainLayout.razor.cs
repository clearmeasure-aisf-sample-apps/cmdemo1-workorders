using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared;

public partial class MainLayout
{
    /// <summary>
    /// Must stay aligned with <c>@media (max-width: 768px)</c> in <c>MainLayout.razor.css</c> and the
    /// <c>matchMedia</c> argument in <c>mainLayoutNav.js</c>.
    /// </summary>
    public const string NavRailBreakpointMediaQuery = Components.NavRail.BreakpointMediaQuery;

    public enum Elements
    {
        NavRailToggle,
        CopyrightFooter,
        FooterNote,
        SoftwareVersion,
        DarkModeToggle,
        GitSha,
        EnvironmentName,
        BrandSubtitle,
        HelpLink,
        NotificationsButton,
        NotificationsBadge,
        BackToTop
    }

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private ThemePreferenceService Theme { get; set; } = null!;

    private string DarkModeToggleTitle => Theme.IsDarkMode ? "Switch to light mode" : "Switch to dark mode";

    protected override async Task OnInitializedAsync()
    {
        await ApplyBrowserCultureAsync();
    }

    private async Task ApplyBrowserCultureAsync()
    {
        try
        {
            var language = await Js.InvokeAsync<string>("churchBulletinBrowserCulture");
            ClientDateFormatter.ApplyBrowserCulture(language);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            Logger.LogWarning(ex, "Browser culture could not be read; shell continues with the default culture.");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await Theme.InitializeAsync();
        Theme.OnChange += OnThemeChanged;
        await InvokeAsync(StateHasChanged);
    }

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);

    private async Task ToggleDarkModeAsync() => await Theme.SetDarkModeAsync(!Theme.IsDarkMode);

    public override void Dispose()
    {
        Theme.OnChange -= OnThemeChanged;
        base.Dispose();
    }
}
