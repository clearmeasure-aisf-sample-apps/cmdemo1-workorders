using System.ComponentModel.DataAnnotations;
using Shouldly;
using LoginPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.Login;

[TestFixture]
public class LoginCredentialValidationTests
{
    [Test]
    public void ShouldOnlyRequireUsername()
    {
        var loginModel = new LoginPage.LoginModel { Username = "hsimpson" };
        var validationContext = new ValidationContext(loginModel);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(loginModel, validationContext, validationResults, true);

        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRequireUsername()
    {
        var loginModel = new LoginPage.LoginModel { Username = "" };
        var validationContext = new ValidationContext(loginModel);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(loginModel, validationContext, validationResults, true);

        isValid.ShouldBeFalse();
        validationResults.ShouldContain(r => r.MemberNames.Contains("Username"));
    }
}
