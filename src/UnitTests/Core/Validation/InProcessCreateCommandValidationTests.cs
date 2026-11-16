using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Shared;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

/// <summary>
/// Characterization of master in-process create validation (#1824): callers get Failed
/// results from command/handler seams, not <see cref="ValidationException"/>. HTTP shape
/// rules stay on the envelope FluentValidators in <see cref="HttpEnvelopeValidationDeferral"/>.
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
    public void FindRuleViolations_ShouldReportDuplicateVendor_WhenNameMatchesIgnoringCase()
    {
        var command = new CreateVendorCommand("acme", null, null, null, null);

        var violations = command.FindRuleViolations(["Bolt", "Acme"]);

        violations.ShouldBe(["A vendor named 'acme' already exists."]);
    }

    [Test]
    public void FindRuleViolations_ShouldIgnoreVendorFieldShape_WhenNameIsBlank()
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
    public void FindRuleViolations_ShouldIgnoreTeamFieldShape_WhenNameIsBlank()
    {
        var command = new CreateTeamCommand("", null, []);

        command.FindRuleViolations([], []).ShouldBeEmpty();
    }

    [Test]
    public async Task Bus_ShouldReturnFailedDatedResult_WhenDueDatesEmptyWithoutValidationException()
    {
        var bus = BusWithoutDeferredValidators();
        var command = new CreateDatedWorkOrdersCommand("a", "b", "t", "d", []);

        var result = await bus.Send(command);

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe(CreateDatedWorkOrdersCommand.AtLeastOneDueDateRequired);
        result.WorkOrders.ShouldBeEmpty();
    }

    [Test]
    public async Task Bus_ShouldReturnFailedDatedResult_WhenDueDatesExceedTenWithoutValidationException()
    {
        var dueDates = Enumerable.Range(0, 11).Select(i => new DateOnly(2026, 1, 1).AddDays(i)).ToList();
        var bus = BusWithoutDeferredValidators();
        var command = new CreateDatedWorkOrdersCommand("a", "b", "t", "d", dueDates);

        var result = await bus.Send(command);

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe(CreateDatedWorkOrdersCommand.BatchCannotExceed);
    }

    [Test]
    public async Task Bus_ShouldReturnFailedVendorResult_WhenFindRuleViolationsReportsDuplicate()
    {
        var bus = BusWithoutDeferredValidators(existingVendorNames: ["Acme"]);
        var command = new CreateVendorCommand("acme", "HVAC", null, null, null);

        var result = await bus.Send(command);

        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldBe("A vendor named 'acme' already exists.");
        result.Id.ShouldBeNull();
    }

    [Test]
    public async Task Bus_ShouldReturnFailedTeamResult_WhenFindRuleViolationsReportsUnknownMember()
    {
        var bus = BusWithoutDeferredValidators();
        var command = new CreateTeamCommand("Grounds", null, ["ghost"]);

        var result = await bus.Send(command);

        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldBe("No employee with user name 'ghost' exists.");
        result.Id.ShouldBeNull();
    }

    [Test]
    public async Task Bus_ShouldReachVendorHandler_WhenBlankNameWouldFailHttpFluentValidation()
    {
        var bus = BusWithoutDeferredValidators();
        var command = new CreateVendorCommand("", "HVAC", null, null, null);

        var result = await bus.Send(command);

        result.Succeeded.ShouldBeTrue();
        result.Message.ShouldBe("handler-accepted-blank-name");
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

    private static Bus BusWithoutDeferredValidators(IReadOnlyList<string>? existingVendorNames = null)
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<InProcessCreateCommandValidationTests>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddTransient<IRequestHandler<CreateDatedWorkOrdersCommand, CreateDatedWorkOrdersResult>,
            CharacterizingDatedHandler>();
        services.AddTransient<IRequestHandler<CreateVendorCommand, CreateVendorResult>>(
            _ => new CharacterizingVendorHandler(existingVendorNames ?? []));
        services.AddTransient<IRequestHandler<CreateTeamCommand, CreateTeamResult>, CharacterizingTeamHandler>();
        return new Bus(services.BuildServiceProvider().GetRequiredService<IMediator>());
    }

    private sealed class CharacterizingDatedHandler
        : IRequestHandler<CreateDatedWorkOrdersCommand, CreateDatedWorkOrdersResult>
    {
        public Task<CreateDatedWorkOrdersResult> Handle(
            CreateDatedWorkOrdersCommand request,
            CancellationToken cancellationToken)
        {
            var violations = request.FindRuleViolations();
            return Task.FromResult(violations.Count > 0
                ? CreateDatedWorkOrdersResult.Failed(string.Join(" ", violations))
                : CreateDatedWorkOrdersResult.Created([]));
        }
    }

    private sealed class CharacterizingVendorHandler(IReadOnlyList<string> existingNames)
        : IRequestHandler<CreateVendorCommand, CreateVendorResult>
    {
        public Task<CreateVendorResult> Handle(CreateVendorCommand request, CancellationToken cancellationToken)
        {
            var violations = request.FindRuleViolations(existingNames);
            if (violations.Count > 0)
            {
                return Task.FromResult(new CreateVendorResult(false, string.Join(" ", violations), null));
            }

            return Task.FromResult(new CreateVendorResult(true, "handler-accepted-blank-name", Guid.Empty));
        }
    }

    private sealed class CharacterizingTeamHandler : IRequestHandler<CreateTeamCommand, CreateTeamResult>
    {
        public Task<CreateTeamResult> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
        {
            var violations = request.FindRuleViolations([], []);
            return Task.FromResult(violations.Count > 0
                ? new CreateTeamResult(false, string.Join(" ", violations), null)
                : new CreateTeamResult(true, "created", Guid.Empty));
        }
    }
}
