using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class CreateDatedWorkOrdersCommandShapeRulesTests
{
    [Test]
    public void ErrorMessages_ShouldListFluentValidationMessages_WhenTitleIsBlank()
    {
        var command = new CreateDatedWorkOrdersCommand(
            "creator", "assignee", "", "Desc", [new DateOnly(2026, 8, 29)]);

        new CreateDatedWorkOrdersCommandShapeRules(new CreateDatedWorkOrdersCommandValidator()).ErrorMessages(command)
            .ShouldContain("'Title' must not be empty.");
    }
}
