# Note 174

ThemePreferenceService.ThemeJsModulePath is the public constant "./_content/ClearMeasure.Bootcamp.UI.Shared/js/theme.js" (`public const string ThemeJsModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/theme.js";`) in `src/UI.Shared/Services/ThemePreferenceService.cs`. `InitializeAsync` passes that constant to `IJSRuntime.InvokeAsync` as the module path to import (`_module = await _js.InvokeAsync<IJSObjectReference>("import", ThemeJsModulePath);`).
