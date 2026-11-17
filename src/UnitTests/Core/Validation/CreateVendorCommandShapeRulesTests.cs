using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class CreateVendorCommandShapeRulesTests
{
    [Test]
    public void ErrorMessages_ShouldBeEmpty_WhenVendorPassesShapeRules()
    {
        var command = new CreateVendorCommand("Acme", "HVAC", null, null, null);

        new CreateVendorCommandShapeRules(new CreateVendorCommandValidator()).ErrorMessages(command).ShouldBeEmpty();
    }

    [Test]
    public void ErrorMessages_ShouldListFluentValidationMessages_WhenVendorNameExceedsMaxLength()
    {
        var command = new CreateVendorCommand(new string('a', 101), null, null, null, null);

        new CreateVendorCommandShapeRules(new CreateVendorCommandValidator()).ErrorMessages(command).ShouldContain(
            "The length of 'Name' must be 100 characters or fewer. You entered 101 characters.");
    }
}
