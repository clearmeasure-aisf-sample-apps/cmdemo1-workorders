using Bunit;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.SignIn;

[TestFixture]
public class LoginUserSelectAutocompleteTests
{
    [Test]
    public async Task ShouldTurnOffAutofillOnTheMemberDropdown()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        LoginPageSteps.UserSelect(component).GetAttribute("autocomplete").ShouldBe("off");
    }
}
