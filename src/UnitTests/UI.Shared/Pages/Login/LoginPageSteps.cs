using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using LoginPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.Login;

/// <summary>
/// DOM selectors and shared arrange helpers for login-page tests. No assertions.
/// </summary>
public static class LoginPageSteps
{
    /// <summary>CSS selector for a login-page data-testid.</summary>
    public static string TestId(LoginPage.Elements element) => $"[data-testid='{element}']";

    /// <summary>Finds the member dropdown by test id.</summary>
    public static AngleSharp.Dom.IElement UserSelect(IRenderedComponent<LoginPage> component) =>
        component.Find(TestId(LoginPage.Elements.User));

    /// <summary>Finds the Enter the Portal submit button by test id.</summary>
    public static AngleSharp.Dom.IElement LoginButton(IRenderedComponent<LoginPage> component) =>
        component.Find(TestId(LoginPage.Elements.LoginButton));

    /// <summary>Finds the Lovejoy shortcut button by test id.</summary>
    public static AngleSharp.Dom.IElement LovejoyShortcut(IRenderedComponent<LoginPage> component) =>
        component.Find(TestId(LoginPage.Elements.LovejoyShortcut));

    /// <summary>Selects a member username in the dropdown.</summary>
    public static Task SelectUserAsync(IRenderedComponent<LoginPage> component, string userName) =>
        UserSelect(component).ChangeAsync(new ChangeEventArgs { Value = userName });

    /// <summary>Clicks the Enter the Portal submit button.</summary>
    public static Task ClickLoginAsync(IRenderedComponent<LoginPage> component) =>
        LoginButton(component).ClickAsync(new MouseEventArgs());

    /// <summary>Clicks the Timothy Lovejoy shortcut.</summary>
    public static Task ClickLovejoyShortcutAsync(IRenderedComponent<LoginPage> component) =>
        LovejoyShortcut(component).ClickAsync(new MouseEventArgs());
}
