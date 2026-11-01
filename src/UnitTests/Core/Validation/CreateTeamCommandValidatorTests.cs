using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class CreateTeamCommandValidatorTests
{
    private CreateTeamCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateTeamCommandValidator();
    }

    [Test]
    public void ShouldPass_WhenAllFieldsValid()
    {
        var command = new CreateTeamCommand("Grounds", "Lawn and snow", ["u1", "u2"]);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenDescriptionIsNullAndNoMembers()
    {
        var command = new CreateTeamCommand("Grounds", null, []);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ShouldFail_WhenNameIsEmpty(string name)
    {
        var command = new CreateTeamCommand(name, null, []);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldFail_WhenNameIs101Chars()
    {
        var command = new CreateTeamCommand(new string('a', 101), null, []);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldPass_WhenNameIsExactly100Chars()
    {
        var command = new CreateTeamCommand(new string('a', 100), null, []);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void ShouldPass_WhenDescriptionIsExactly500Chars()
    {
        var command = new CreateTeamCommand("Grounds", new string('d', 500), []);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void ShouldFail_WhenDescriptionIs501Chars()
    {
        var command = new CreateTeamCommand("Grounds", new string('d', 501), []);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [TestCase("")]
    [TestCase("  ")]
    public void ShouldFail_WhenMemberUserNameIsBlank(string userName)
    {
        var command = new CreateTeamCommand("Grounds", null, ["u1", userName]);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.StartsWith(nameof(CreateTeamCommand.MemberUserNames)));
    }

    [Test]
    public void ShouldReportDuplicate_WhenNameMatchesExistingIgnoringCase()
    {
        var command = new CreateTeamCommand("Grounds", null, []);

        var violations = command.FindRuleViolations(["grounds"], []);

        violations.ShouldBe(["A team named 'Grounds' already exists."]);
    }

    [Test]
    public void ShouldReportDuplicate_WhenNameMatchesExistingAfterTrimming()
    {
        var command = new CreateTeamCommand("  Grounds ", null, []);

        var violations = command.FindRuleViolations(["GROUNDS"], []);

        violations.ShouldHaveSingleItem();
    }

    [Test]
    public void ShouldReportUnknownMember_WhenUserNameNotKnown()
    {
        var command = new CreateTeamCommand("Grounds", null, ["U1", "ghost"]);

        var violations = command.FindRuleViolations(["Kitchen"], ["u1", "u2"]);

        violations.ShouldBe(["No employee with user name 'ghost' exists."]);
    }

    [Test]
    public void ShouldReportEveryViolation_WhenDuplicateAndUnknownMembers()
    {
        var command = new CreateTeamCommand("Kitchen", null, ["ghost", "phantom"]);

        var violations = command.FindRuleViolations(["Kitchen"], ["u1"]);

        violations.Count.ShouldBe(3);
    }

    [Test]
    public void ShouldReturnNoViolations_WhenUniqueAndMembersKnown()
    {
        var command = new CreateTeamCommand("Grounds", null, ["u1", "U2"]);

        var violations = command.FindRuleViolations(["Kitchen"], ["u1", "u2", "u3"]);

        violations.ShouldBeEmpty();
    }
}
