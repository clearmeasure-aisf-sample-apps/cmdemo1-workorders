namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Holds the current light/dark theme and persists the user's choice in browser local storage.
/// </summary>
public sealed class ThemePreferenceService : IAsyncDisposable
{
    public const string ThemeJsModulePath = ThemeScript.ModulePath;

    private readonly ThemeScript _script;
    private bool _isDarkMode;
    private bool _initialized;

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public ThemePreferenceService(Microsoft.JSInterop.IJSRuntime js) => _script = new ThemeScript(js);

    // ReSharper disable once EventNeverSubscribedTo.Global -- public Blazor state-change event; consumers subscribe at runtime
    public event Action? OnChange;

    public bool IsDarkMode => _isDarkMode;

    /// <summary>
    /// Loads stored or system-default theme and applies it to the document (without persisting system default).
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        var theme = await _script.GetThemeAsync();
        if (theme is not null)
        {
            _isDarkMode = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase);
            await _script.SyncDomFromThemeAsync(theme);
        }

        _initialized = true;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Sets dark mode on or off, persists to local storage, and updates the document root.
    /// </summary>
    public async Task SetDarkModeAsync(bool enabled)
    {
        if (_isDarkMode == enabled && _initialized)
            return;

        _isDarkMode = enabled;
        await _script.SetThemeAsync(enabled);
        _initialized = true;
        OnChange?.Invoke();
    }

    public ValueTask DisposeAsync() => _script.DisposeAsync();
}
