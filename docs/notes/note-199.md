# Note 199

`MainLayout.NavRailBreakpointMediaQuery` in `src/UI.Shared/MainLayout.razor.cs` is the string `(max-width: 768px)`. `OnAfterRenderAsync` passes that constant to `initNavToggle` (`_jsModule.InvokeAsync<IJSObjectReference>("initNavToggle", _dotNetRef, NavRailBreakpointMediaQuery)`), and `initNavToggle` in `src/UI.Shared/wwwroot/js/mainLayoutNav.js` calls `window.matchMedia` with that string. `MainLayout.razor.css` uses the same query (`@media (max-width: 768px)`) for the narrow sidebar.
