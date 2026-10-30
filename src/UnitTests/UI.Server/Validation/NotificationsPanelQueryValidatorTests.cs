using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Validation;

[TestFixture]
public class NotificationsPanelQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenQueryHasUserName()
    {
        var validator = new NotificationsPanelQueryValidator();

        var result = validator.TestValidate(new NotificationsPanelQuery("hsimpson"));

        result.IsValid.ShouldBeTrue();
    }
}
