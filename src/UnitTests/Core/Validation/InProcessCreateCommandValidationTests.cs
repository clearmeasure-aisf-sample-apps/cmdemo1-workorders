using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

/// <summary>
/// Characterization and design tests for #1824 in-process create validation: Failed results via
/// <c>FindRuleViolations</c> (shape from deferred FluentValidators + stored-data rules), not
/// <see cref="FluentValidation.ValidationException"/>. Real handlers are covered by integration tests.
/// </summary>
[TestFixture]
public class InProcessCreateCommandValidationTests
{
    [Test]
    public void FindRuleViolations_ShouldReportEmptyDueDates_WhenDatedBatchHasNone()
    {
        var command = new CreateDatedWorkOrdersCommand("creator", "assignee", "Title", "Desc", []);

        var violations = command.FindRuleViolations();

        violations.ShouldBe([CreateDatedWorkOrdersCommand.AtLeastOneDueDateRequired]);
    }

    [Test]
    public void FindRuleViolations_ShouldReportBatchLimit_WhenDatedBatchExceedsTen()
    {
        var dueDates = Enumerable.Range(0, CreateDatedWorkOrdersCommand.MaximumBatchSize + 1)
            .Select(i => new DateOnly(2026, 8, 29).AddDays(i))
            .ToList();
        var command = new CreateDatedWorkOrdersCommand("creator", "assignee", "Title", "Desc", dueDates);

        var violations = command.FindRuleViolations();

        violations.ShouldBe([CreateDatedWorkOrdersCommand.BatchCannotExceed]);
    }

    [Test]
    public void FindRuleViolations_ShouldBeEmpty_WhenDatedBatchSizeIsAllowed()
    {
        var command = new CreateDatedWorkOrdersCommand(
            "creator",
            "assignee",
            "Title",
            "Desc",
            [new DateOnly(2026, 8, 29)]);

        command.FindRuleViolations().ShouldBeEmpty();
    }

    [Test]
    public void FindRuleViolations_ShouldReportDatedTitleShape_WhenTitleIsBlankAndDueDatesOk()
    {
        var command = new CreateDatedWorkOrdersCommand(
            "creator",
            "assignee",
            "",
            "Desc",
            [new DateOnly(2026, 8, 29)]);

        command.FindRuleViolations().ShouldContain("'Title' must not be empty.");
    }

    [Test]
    public void FindRuleViolations_ShouldReportDuplicateVendor_WhenNameMatchesIgnoringCase()
    {
        var command = new CreateVendorCommand("acme", null, null, null, null);

        var violations = command.FindRuleViolations(["Bolt", "Acme"]);

        violations.ShouldBe(["A vendor named 'acme' already exists."]);
    }

    [Test]
    public void FindRuleViolations_ShouldReportVendorNameShape_WhenNameIsBlank()
    {
        var command = new CreateVendorCommand("  ", null, null, null, null);

        command.FindRuleViolations([]).ShouldContain("'Name' must not be empty.");
    }

    [Test]
    public void FindRuleViolations_ShouldReportUnknownTeamMember_WhenUserNameIsMissing()
    {
        var command = new CreateTeamCommand("Grounds", null, ["ghost"]);

        var violations = command.FindRuleViolations(["Kitchen"], ["u1"]);

        violations.ShouldBe(["No employee with user name 'ghost' exists."]);
    }

    [Test]
    public void FindRuleViolations_ShouldReportTeamNameShape_WhenNameIsBlank()
    {
        var command = new CreateTeamCommand("", null, []);

        command.FindRuleViolations([], []).ShouldContain("'Name' must not be empty.");
    }

    [Test]
    public void HttpEnvelopeCatalog_ShouldKeepThreeCreateValidatorsOffMediatR()
    {
        HttpEnvelopeValidationDeferral.Entries.Select(e => e.RequestType).ShouldBe(
        [
            typeof(CreateVendorCommand),
            typeof(CreateTeamCommand),
            typeof(CreateDatedWorkOrdersCommand)
        ]);
        HttpEnvelopeValidationDeferral.IsDeferredValidator(typeof(CreateVendorCommandValidator))
            .ShouldBeTrue();
        HttpEnvelopeValidationDeferral.IsDeferredValidator(typeof(CreateTeamCommandValidator))
            .ShouldBeTrue();
        HttpEnvelopeValidationDeferral.IsDeferredValidator(typeof(CreateDatedWorkOrdersCommandValidator))
            .ShouldBeTrue();
    }

    [Test]
    public void ErrorMessages_ShouldMatchHttpFluentValidation_WhenVendorNameIsBlank()
    {
        var command = new CreateVendorCommand("", "HVAC", null, null, null);

        InProcessCreateCommandShapeValidation.ErrorMessages(command)
            .ShouldBe(new CreateVendorCommandValidator().Validate(command).Errors.Select(e => e.ErrorMessage));
    }
}
