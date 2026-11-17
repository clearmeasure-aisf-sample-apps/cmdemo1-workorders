using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class CreateTeamCommandShapeRulesTests
{
    [Test]
    public void ErrorMessages_ShouldListFluentValidationMessages_WhenTeamDescriptionExceedsMaxLength()
    {
        var command = new CreateTeamCommand("Grounds", new string('d', 501), []);

        new CreateTeamCommandShapeRules(new CreateTeamCommandValidator()).ErrorMessages(command).ShouldContain(
            "The length of 'Description' must be 500 characters or fewer. You entered 501 characters.");
    }
}
