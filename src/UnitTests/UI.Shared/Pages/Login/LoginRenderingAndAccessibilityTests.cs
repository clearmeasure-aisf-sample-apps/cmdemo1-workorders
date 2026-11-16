using Bunit;
using Shouldly;
using LoginPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.Login;

[TestFixture]
public class LoginRenderingAndAccessibilityTests
{
    [Test]
    public async Task ShouldDisplayDropdownWithEmployees()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var employeeSelect = LoginPageSteps.UserSelect(component);
        employeeSelect.ShouldNotBeNull();
        employeeSelect.GetAttribute("id").ShouldBe(nameof(LoginPage.Elements.User));
        component.Find($"label[for='{LoginPage.Elements.User}']").ShouldNotBeNull();
        var options = component.FindAll("option");
        options.Count.ShouldBe(6);
        options[0].GetAttribute("value").ShouldBe(string.Empty);
        options[0].TextContent.ShouldBe("-- Select a parishioner or staff member --");
    }

    [Test]
    public async Task ShouldDisplayUppercaseLabelsInLoginDropdown_ForMixedAndAllCapsNames()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        var options = component.FindAll("option");

        options.Single(o => o.GetAttribute("value") == "hsimpson")
            .TextContent.ShouldBe("HOMER SIMPSON");
        options.Single(o => o.GetAttribute("value") == "jdoe")
            .TextContent.ShouldBe("MARY JANE SIMPSON");
        var mburnsOption = options.Single(o => o.GetAttribute("value") == "mburns");
        mburnsOption.TextContent.ShouldBe("MONTGOMERY BURNS");
        mburnsOption.GetAttribute("value").ShouldBe("mburns");
    }

    [Test]
    public async Task ShouldDisplayWelcomeToTheChurchPortalHeading()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("h3").TextContent.ShouldBe("Welcome to the Church Workers Portal");
    }

    [Test]
    public async Task ShouldDisplayKalahariChurchSubtitle()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find(".login-subtitle").TextContent.ShouldBe("Kalahari Church");
    }

    [Test]
    public async Task ShouldHaveAutofocusOnMemberDropdown()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        LoginPageSteps.UserSelect(component).HasAttribute("autofocus").ShouldBeTrue();
    }

    [Test]
    public async Task ShouldDisplaySignInHeading()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("h2").TextContent.Trim().ShouldBe("Sign in");
    }

    [Test]
    public async Task ShouldDisplayLockIconInSignInHeading()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var icon = component.Find("h2 > i.bi-lock");
        icon.ShouldNotBeNull();
        icon.GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Test]
    public async Task ShouldDisplayLoginHint_WithExactText()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var hint = component.Find(LoginPageSteps.TestId(LoginPage.Elements.LoginHint));
        hint.TagName.ShouldBe("P");
        hint.TextContent.ShouldBe("Choose your user to sign in.");
        hint.PreviousElementSibling!.TagName.ShouldBe("H2");
        hint.PreviousElementSibling.TextContent.Trim().ShouldBe("Sign in");
    }

    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnLoginHeading()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find(LoginPageSteps.TestId(LoginPage.Elements.LoginHeadingTooltip))
            .GetAttribute("title").ShouldBe("Sign in page");
    }

    [Test]
    public async Task Should_ShowHelperTextUnderMemberDropdown()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var helperText = component.Find("small.form-text.text-muted");
        helperText.ShouldNotBeNull();
        helperText.TextContent.ShouldBe("Not listed? Ask the church office to add you.");
    }

    [Test]
    public async Task Should_DescribeUserSelect_WithNotListedHint()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        LoginPageSteps.UserSelect(component).GetAttribute("aria-describedby")
            .ShouldBe("login-user-hint");
        var hint = component.Find("#login-user-hint");
        hint.TagName.ShouldBe("SMALL");
        hint.TextContent.ShouldBe("Not listed? Ask the church office to add you.");
    }

    [Test]
    public async Task Should_RenderForgotLoginLink_WithCorrectText()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("a[href='mailto:office@kalaharichurch.example']")
            .TextContent.ShouldBe("Forgot your login?");
    }

    [Test]
    public async Task Should_RenderForgotLoginLink_WithCorrectHref()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("a[href='mailto:office@kalaharichurch.example']")
            .GetAttribute("href").ShouldBe("mailto:office@kalaharichurch.example");
    }

    [Test]
    public async Task Should_RenderForgotLoginLink_InsideSmallTag()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("small > a[href='mailto:office@kalaharichurch.example']")
            .ShouldNotBeNull();
    }

    [Test]
    public async Task Should_RenderOfficePhoneLink_WithCorrectText()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("a[href='tel:+15550102468']")
            .TextContent.ShouldBe("(555) 010-2468");
    }

    [Test]
    public async Task Should_RenderOfficePhoneLink_WithCorrectHref()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("a[href='tel:+15550102468']")
            .GetAttribute("href").ShouldBe("tel:+15550102468");
    }

    [Test]
    public async Task Should_RenderOfficePhoneLink_InsideSmallTag()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("small > a[href='tel:+15550102468']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldDisplayCurrentYearFooter()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var footerDiv = component.FindAll("div.text-center")
            .First(d => d.QuerySelector("small.text-muted")?.TextContent
                .Contains("Kalahari Church") == true);

        footerDiv.QuerySelector("small.text-muted")!.TextContent
            .ShouldBe("Kalahari Church · " + TimeProvider.System.GetLocalNow().Year);
    }

    [Test]
    public async Task EnterThePortalButton_ShouldHaveFullWidthClass()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        LoginPageSteps.LoginButton(component).GetAttribute("class")!.ShouldContain("w-100");
    }

    [Test]
    public async Task EnterThePortalButton_ShouldPreserveBtnPrimaryClass()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        LoginPageSteps.LoginButton(component).GetAttribute("class")!.ShouldContain("btn-primary");
    }

    [Test]
    public async Task EnterThePortalButton_ShouldPreserveSubmitType()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        LoginPageSteps.LoginButton(component).GetAttribute("type").ShouldBe("submit");
    }

    [Test]
    public async Task EnterThePortalButton_ShouldPreserveButtonText()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        LoginPageSteps.LoginButton(component).TextContent.Trim().ShouldBe("Enter the Portal");
    }

    [Test]
    public async Task Should_RenderRememberMySelectionCheckbox()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("input[type='checkbox'][id='rememberMySelection']").ShouldNotBeNull();
    }

    [Test]
    public async Task Should_RenderRememberMySelectionCheckbox_UncheckedByDefault()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("input[type='checkbox'][id='rememberMySelection']")
            .HasAttribute("checked").ShouldBeFalse();
    }

    [Test]
    public async Task Should_AssociateRememberMySelectionLabel_ViaForId()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var label = component.Find("label[for='rememberMySelection']");
        label.ShouldNotBeNull();
        label.TextContent.ShouldBe("Remember my selection");
    }

    [Test]
    public async Task Should_RenderNeedHelpLink_WithCorrectText()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var link = component.Find("a[href='https://www.clear-measure.com']");
        var caption = link.QuerySelector("small");
        caption.ShouldNotBeNull();
        caption.TextContent.ShouldBe("Need help?");
        var hidden = link.QuerySelector(".visually-hidden");
        hidden.ShouldNotBeNull();
        hidden.TextContent.ShouldBe("(opens in a new tab)");
    }

    [Test]
    public async Task Should_RenderNeedHelpLink_WithTargetBlank()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("a[href='https://www.clear-measure.com']")
            .GetAttribute("target").ShouldBe("_blank");
    }

    [Test]
    public async Task Should_RenderNeedHelpLink_WithRelNoopener()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("a[href='https://www.clear-measure.com']")
            .GetAttribute("rel").ShouldBe("noopener noreferrer");
    }

    [Test]
    public async Task Should_AnnounceErrorMessage_WithRoleAlert()
    {
        var page = LoginPageContext.Create().WithBus(new LoginFailingEmployeeBus());
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        component.Find("div.alert.alert-danger").GetAttribute("role").ShouldBe("alert");
    }
}
