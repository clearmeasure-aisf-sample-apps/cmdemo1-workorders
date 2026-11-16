using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class InProcessCreateCommandShapeValidationTests
{
    [Test]
    public void ErrorMessages_ShouldBeEmpty_WhenVendorPassesShapeRules()
    {
        var command = new CreateVendorCommand("Acme", "HVAC", null, null, null);

        InProcessCreateCommandShapeValidation.ErrorMessages(command).ShouldBeEmpty();
    }

    [Test]
    public void ErrorMessages_ShouldListFluentValidationMessages_WhenVendorNameExceedsMaxLength()
    {
        var command = new CreateVendorCommand(new string('a', 101), null, null, null, null);

        var messages = InProcessCreateCommandShapeValidation.ErrorMessages(command);

        messages.ShouldContain(
            "The length of 'Name' must be 100 characters or fewer. You entered 101 characters.");
    }

    [Test]
    public void ErrorMessages_ShouldListFluentValidationMessages_WhenTeamDescriptionExceedsMaxLength()
    {
        var command = new CreateTeamCommand("Grounds", new string('d', 501), []);

        var messages = InProcessCreateCommandShapeValidation.ErrorMessages(command);

        messages.ShouldContain(
            "The length of 'Description' must be 500 characters or fewer. You entered 501 characters.");
    }
}
