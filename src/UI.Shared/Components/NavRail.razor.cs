using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Application navigation rail: sidebar, viewport sync, toggle and overlay backdrop.
/// </summary>
public partial class NavRail : IAsyncDisposable
{
    /// <summary>
    /// Must stay aligned with <c>@media (max-width: 768px)</c> in <c>MainLayout.razor.css</c> and the
    /// <c>matchMedia</c> argument in <c>mainLayoutNav.js</c>.
    /// </summary>
    public const string BreakpointMediaQuery = "(max-width: 768px)";

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private ElementReference _navToggleButtonRef;
    private DotNetObjectReference<NavRail>? _dotNetRef;
    private bool _disposed;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _navToggleHelper;
    private bool _isNarrowViewport;
    private bool _viewportSynced;
    private bool _navVisible = true;

    private string AppContainerClass => NavRailCss.AppContainerClass(_isNarrowViewport, _navVisible);

    private string SidebarClass => NavRailCss.SidebarClass(_isNarrowViewport, _navVisible);

    internal string NavToggleTitle =>
        _navVisible ? "Hide navigation panel" : "Show navigation panel";

    internal string NavToggleAriaExpanded => _navVisible ? "true" : "false";

    internal bool IsNavVisible => _navVisible;

    /// <summary>
    /// Raised when rail visibility or viewport state changes so cascaded chrome can re-render.
    /// </summary>
    // ReSharper disable once EventNeverSubscribedTo.Global -- Toggle subscribes at runtime
    internal event Action? Changed;

    internal void BindToggleButton(ElementReference button) => _navToggleButtonRef = button;

    [JSInvokable]
    public Task OnViewportChanged(bool isNarrow)
    {
        if (!_viewportSynced)
        {
            _viewportSynced = true;
            if (isNarrow)
                _navVisible = false;
        }

        _isNarrowViewport = isNarrow;
        NotifyChanged();
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            _jsModule = await Js.InvokeAsync<IJSObjectReference>("import",
                "./_content/ClearMeasure.Bootcamp.UI.Shared/js/mainLayoutNav.js");
            if (_disposed)
                return;
            _navToggleHelper = await _jsModule.InvokeAsync<IJSObjectReference>("initNavToggle", _dotNetRef,
                BreakpointMediaQuery);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    internal async Task ToggleAsync()
    {
        var wasVisible = _navVisible;
        _navVisible = !wasVisible;
        await InvokeAsync(NotifyChanged);

        if (_isNarrowViewport && wasVisible)
        {
            await Task.Yield();
            try
            {
                await _navToggleButtonRef.FocusAsync();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        if (_navToggleHelper is not null)
        {
            try
            {
                await _navToggleHelper.InvokeVoidAsync("dispose");
            }
            catch (JSDisconnectedException)
            {
            }

            await _navToggleHelper.DisposeAsync();
        }

        if (_jsModule is not null)
            await _jsModule.DisposeAsync();

        _dotNetRef?.Dispose();
    }
}
