using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

/// <summary>
/// Characterization and design tests for #1824 in-process create validation: Failed results via
/// stored-data <c>FindRuleViolations</c> plus handler-injected concrete FluentValidators — not
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
    public void FindRuleViolations_ShouldIgnoreBlankTitle_WhenOnlyStoredBatchRulesRun()
    {
        var command = new CreateDatedWorkOrdersCommand(
            "creator",
            "assignee",
            "",
            "Desc",
            [new DateOnly(2026, 8, 29)]);

        command.FindRuleViolations().ShouldBeEmpty();
    }

    [Test]
    public void FindRuleViolations_ShouldReportDuplicateVendor_WhenNameMatchesIgnoringCase()
    {
        var command = new CreateVendorCommand("acme", null, null, null, null);

        var violations = command.FindRuleViolations(["Bolt", "Acme"]);

        violations.ShouldBe(["A vendor named 'acme' already exists."]);
    }

    [Test]
    public void FindRuleViolations_ShouldIgnoreBlankVendorName_WhenOnlyStoredDataRulesRun()
    {
        var command = new CreateVendorCommand("  ", null, null, null, null);

        command.FindRuleViolations([]).ShouldBeEmpty();
    }

    [Test]
    public void FindRuleViolations_ShouldReportUnknownTeamMember_WhenUserNameIsMissing()
    {
        var command = new CreateTeamCommand("Grounds", null, ["ghost"]);

        var violations = command.FindRuleViolations(["Kitchen"], ["u1"]);

        violations.ShouldBe(["No employee with user name 'ghost' exists."]);
    }

    [Test]
    public void FindRuleViolations_ShouldIgnoreBlankTeamName_WhenOnlyStoredDataRulesRun()
    {
        var command = new CreateTeamCommand("", null, []);

        command.FindRuleViolations([], []).ShouldBeEmpty();
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
}
