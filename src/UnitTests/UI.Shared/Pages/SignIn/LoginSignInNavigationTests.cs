using Bunit;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.SignIn;

[TestFixture]
public class LoginSignInNavigationTests
{
    [Test]
    public async Task ShouldLoginWithSelectedEmployee()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await LoginPageSteps.SelectUserAsync(component, "hsimpson");
        await LoginPageSteps.ClickLoginAsync(component);

        page.Auth.IsAuthenticated().ShouldBeTrue();
        page.Auth.GetUsername().ShouldBe("hsimpson");
    }

    [Test]
    public async Task Should_ShowLovejoyShortcut_WithoutDropdownSelection()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var shortcut = LoginPageSteps.LovejoyShortcut(component);

        shortcut.ShouldNotBeNull();
        shortcut.TextContent.Trim().ShouldBe("Log in as Timothy Lovejoy");
        page.Auth.IsAuthenticated().ShouldBeFalse();
    }

    [Test]
    public async Task Should_LoginAsTlovejoy_WhenLovejoyShortcutClicked()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await LoginPageSteps.ClickLovejoyShortcutAsync(component);

        page.Auth.IsAuthenticated().ShouldBeTrue();
        page.Auth.GetUsername().ShouldBe("tlovejoy");
    }

    [Test]
    public async Task Should_LoginAsTlovejoy_WhenLovejoyClickedBeforeEmployeesLoaded()
    {
        var gatedBus = new LoginGatedEmployeeBus();
        var page = LoginPageContext.Create().WithBus(gatedBus);
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        component.FindAll("option")
            .Count(o => !string.IsNullOrEmpty(o.GetAttribute("value")))
            .ShouldBe(0);
        var clickTask = LoginPageSteps.ClickLovejoyShortcutAsync(component);
        page.Auth.IsAuthenticated().ShouldBeFalse();

        gatedBus.ReleaseEmployees();
        await clickTask;
        await component.WaitForAssertionAsync(() =>
        {
            page.Auth.IsAuthenticated().ShouldBeTrue();
            page.Auth.GetUsername().ShouldBe("tlovejoy");
        });
    }

    [Test]
    public async Task Should_KeepDropdownLoginUnchanged_WhenLovejoyShortcutPresent()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        LoginPageSteps.LovejoyShortcut(component).ShouldNotBeNull();
        await LoginPageSteps.SelectUserAsync(component, "hsimpson");
        await LoginPageSteps.ClickLoginAsync(component);

        page.Auth.IsAuthenticated().ShouldBeTrue();
        page.Auth.GetUsername().ShouldBe("hsimpson");
    }

    [Test]
    public async Task EnterThePortalButton_ShouldBeDisabled_OnInitialRender()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        LoginPageSteps.LoginButton(component).HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task EnterThePortalButton_ShouldBeEnabled_AfterMemberSelected()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await LoginPageSteps.SelectUserAsync(component, "hsimpson");

        LoginPageSteps.LoginButton(component).HasAttribute("disabled").ShouldBeFalse();
    }

    [Test]
    public async Task WelcomeMessage_ShouldBeAbsent_OnInitialRender()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.FindAll("small.text-success").ShouldBeEmpty();
    }

    [Test]
    public async Task WelcomeMessage_ShouldShowFullName_WhenMemberSelected()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await LoginPageSteps.SelectUserAsync(component, "hsimpson");

        var welcomeMessage = component.Find("small.text-success");
        welcomeMessage.ShouldNotBeNull();
        welcomeMessage.TextContent.ShouldBe("Welcome back, HOMER SIMPSON!");
    }

    [Test]
    public async Task WelcomeMessage_ShouldIncludeMiddleName_WhenSelectedEmployeeHasOne()
    {
        var page = LoginPageContext.Create().WithBus(new LoginMiddleNameEmployeeBus());
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await LoginPageSteps.SelectUserAsync(component, "mjsimpson");

        component.Find("small.text-success").TextContent
            .ShouldBe("Welcome back, Mary Jo Simpson!");
    }

    [Test]
    public async Task Should_LoginAsTlovejoy_WhenTlovejoyIsNotInLoadedEmployees()
    {
        var page = LoginPageContext.Create().WithBus(new LoginOffPageEmployeeBus());
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        // Prove the bug condition: tlovejoy is absent from the loaded dropdown options.
        component.FindAll("option")
            .ShouldNotContain(o => o.GetAttribute("value") == "tlovejoy");

        await LoginPageSteps.ClickLovejoyShortcutAsync(component);

        page.Auth.IsAuthenticated().ShouldBeTrue();
        page.Auth.GetUsername().ShouldBe("tlovejoy");
    }

    [Test]
    public async Task Should_ShowInvalidEmployeeSelection_WhenUserNameDoesNotExist()
    {
        var page = LoginPageContext.Create().WithBus(new LoginOffPageEmployeeBus { FailAllLookups = true });
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await LoginPageSteps.ClickLovejoyShortcutAsync(component);

        component.Find("div.alert.alert-danger").TextContent
            .ShouldBe("Invalid employee selection");
        page.Auth.IsAuthenticated().ShouldBeFalse();
    }
}
