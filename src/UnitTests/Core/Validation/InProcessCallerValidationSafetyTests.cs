using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

/// <summary>
/// Before in-process validation covers every rule-bearing validator, prove the requests
/// current Blazor / MCP callers construct for happy-path work pass those rules (#1804).
/// </summary>
[TestFixture]
public class InProcessCallerValidationSafetyTests
{
    [Test]
    public void ShouldPass_WhenBlazorDashboardSendsRecentWorkOrders()
    {
        var validator = new RecentWorkOrdersQueryValidator();
        var request = new RecentWorkOrdersQuery(10);

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorLoadsTheEmployeeDirectory()
    {
        var validator = new EmployeeGetAllQueryValidator();
        var request = new EmployeeGetAllQuery();

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorLooksUpAnEmployeeByUserName()
    {
        var validator = new EmployeeByUserNameQueryValidator();
        var request = new EmployeeByUserNameQuery("hsimpson");

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorLoadsAWorkOrderByNumber()
    {
        var validator = new WorkOrderByNumberQueryValidator();
        var request = new WorkOrderByNumberQuery("WO-100");

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorLoadsAttachments()
    {
        var validator = new WorkOrderAttachmentsQueryValidator();
        var request = new WorkOrderAttachmentsQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorLoadsStatusHistory()
    {
        var validator = new WorkOrderStatusHistoryQueryValidator();
        var request = new WorkOrderStatusHistoryQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorRunsWorkOrderSearch()
    {
        var validator = new WorkOrderSpecificationQueryValidator();
        var request = new WorkOrderSpecificationQuery();

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorLoadsSearchSummary()
    {
        var validator = new WorkOrderSearchSummaryQueryValidator();
        var request = new WorkOrderSearchSummaryQuery("hsimpson");

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorLoadsSavedSearchViews()
    {
        var validator = new SavedSearchViewsQueryValidator();
        var request = new SavedSearchViewsQuery("hsimpson");

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorSavesASearchView()
    {
        var validator = new SaveSearchViewCommandValidator();
        var request = new SaveSearchViewCommand("hsimpson", "Mine", "status=Draft");

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorDeletesASearchView()
    {
        var validator = new DeleteSearchViewCommandValidator();
        var request = new DeleteSearchViewCommand("hsimpson", Guid.Parse("11111111-2222-3333-4444-555555555555"));

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorBulkLoadsWorkOrdersByNumber()
    {
        var validator = new WorkOrdersByNumbersQueryValidator();
        var request = new WorkOrdersByNumbersQuery(["WO-1", "WO-2"]);

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenBlazorRemovesAnAttachment()
    {
        var validator = new RemoveAttachmentCommandValidator();
        var request = new RemoveAttachmentCommand(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Guid.Parse("ffffffff-0000-1111-2222-333333333333"),
            "hsimpson");

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenLoginPublishesUserLoggedIn()
    {
        var validator = new UserLoggedInEventValidator();
        var request = new UserLoggedInEvent("hsimpson");

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenMcpCreatesAVendorWithValidFields()
    {
        var validator = new CreateVendorCommandValidator();
        var request = new CreateVendorCommand("Acme HVAC", "HVAC", "Pat", "pat@example.com", "555-0100");

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenMcpCreatesATeamWithValidFields()
    {
        var validator = new CreateTeamCommandValidator();
        var request = new CreateTeamCommand("Crew", "Day shift", ["hsimpson"]);

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenMcpCreatesDatedWorkOrdersWithRequiredFields()
    {
        var validator = new CreateDatedWorkOrdersCommandValidator();
        var request = new CreateDatedWorkOrdersCommand(
            "hsimpson",
            "gwillie",
            "Mow lawn",
            "Weekly mowing",
            [new DateOnly(2026, 11, 21)]);

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenHealthCheckUsesTheDefaultStatus()
    {
        var validator = new HealthCheckRemotableRequestValidator();
        var request = new HealthCheckRemotableRequest();

        var result = validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }
}
