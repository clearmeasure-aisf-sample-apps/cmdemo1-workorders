using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class SearchFiltersTests
{
    [Test]
    public void ShouldShowTheSignedInUserAsAssignee_WhenAssignedToMeIsOn()
    {
        var filters = new WorkOrderSearchModel.SearchFilters();

        filters.Show(WorkOrderSearchUrlState.Default with { AssignedToMe = true, Assignee = "ned" }, "ada");

        filters.Assignee.ShouldBe("ada");
    }

    [Test]
    public void ShouldShowThePriorityByName_WhenTheStateHasOne()
    {
        var filters = new WorkOrderSearchModel.SearchFilters();

        filters.Show(WorkOrderSearchUrlState.Default with { Priority = WorkOrderPriority.High }, null);

        filters.Priority.ShouldBe("High");
    }

    [Test]
    public void ShouldDropBlankValuesAndGoBackToPageOne_WhenTurnedIntoState()
    {
        var filters = new WorkOrderSearchModel.SearchFilters { Creator = " ", Status = "Assigned", Priority = "Low" };

        var state = filters.ToState(WorkOrderSearchUrlState.Default with { Page = 3 }, assignedToMe: false);

        state.ShouldBe(WorkOrderSearchUrlState.Default with { Status = "Assigned", Priority = WorkOrderPriority.Low, Page = 1 });
    }

    [Test]
    public void ShouldIgnoreTheAssigneeControl_WhenAssignedToMeIsOn()
    {
        var filters = new WorkOrderSearchModel.SearchFilters { Assignee = "ada" };

        var state = filters.ToState(WorkOrderSearchUrlState.Default, assignedToMe: true);

        state.Assignee.ShouldBeNull();
        state.AssignedToMe.ShouldBeTrue();
    }
}
