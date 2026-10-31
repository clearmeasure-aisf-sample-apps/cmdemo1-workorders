using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderSearchUrlStateTests
{
    [Test]
    public void FromQuery_Empty_ReturnsDefaults()
    {
        var state = WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?>());

        state.ShouldBe(WorkOrderSearchUrlState.Default);
        state.Page.ShouldBe(1);
        state.HasAnyFilter.ShouldBeFalse();
    }

    [Test]
    public void ToRelativeUri_Defaults_ReturnsBareSearchPath()
    {
        WorkOrderSearchUrlState.Default.ToRelativeUri().ShouldBe("workorder/search");
    }

    [Test]
    public void RoundTrip_AllValues_Preserved()
    {
        var state = new WorkOrderSearchUrlState
        {
            Creator = "tlovejoy", Assignee = "gwillie", Status = WorkOrderStatus.InProgress.Key, OverdueOnly = true, DueThisWeekOnly = true, WaitingOnVendorOnly = true,
            Priority = WorkOrderPriority.High, Building = "Fellowship Hall", Room = "101", OpenOnly = true, CompletedWithinDays = 30,
            Q = "organ & pipes", Sort = "Priority", Descending = true, Page = 3, View = SearchView.Board
        };

        var uri = state.ToRelativeUri();
        var parsed = WorkOrderSearchUrlState.FromQuery(ParseQuery(uri));

        parsed.ShouldBe(state);
        uri.ShouldContain("Building=Fellowship%20Hall");
        uri.ShouldContain("Room=101");
        uri.ShouldContain("Q=organ%20%26%20pipes");
        uri.ShouldContain("Dir=desc");
        uri.ShouldEndWith("View=Board");
    }

    [Test]
    public void View_Board_RoundTripsAsViewEqualsBoard()
    {
        var state = new WorkOrderSearchUrlState { View = SearchView.Board };

        var uri = state.ToRelativeUri();

        uri.ShouldBe("workorder/search?View=Board");
        WorkOrderSearchUrlState.FromQuery(ParseQuery(uri)).ShouldBe(state);
    }

    [Test]
    public void View_Table_WritesNoViewParameter()
    {
        new WorkOrderSearchUrlState { View = SearchView.Table }.ToRelativeUri().ShouldBe("workorder/search");
        WorkOrderSearchUrlState.Default.View.ShouldBe(SearchView.Table);
        WorkOrderSearchUrlState.Default.ToRelativeUri().ShouldBe("workorder/search");
    }

    [Test]
    public void View_ParsesCaseInsensitively()
    {
        var state = WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?> { ["view"] = "board" });

        state.View.ShouldBe(SearchView.Board);
    }

    [TestCase("Kanban")]
    [TestCase("1")]
    [TestCase("0")]
    [TestCase("7")]
    public void View_InvalidValue_IsTableAndNotWritten(string value)
    {
        var state = WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?> { ["View"] = value });

        state.View.ShouldBe(SearchView.Table);
        state.ToRelativeUri().ShouldBe("workorder/search");
    }

    [Test]
    public void HasAnyFilter_OnlyViewSet_IsFalse()
    {
        new WorkOrderSearchUrlState { View = SearchView.Board }.HasAnyFilter.ShouldBeFalse();
    }

    [Test]
    public void ToRelativeUri_OmitsDefaults()
    {
        var state = new WorkOrderSearchUrlState { Creator = "tlovejoy", Descending = true, Page = 1 };

        state.ToRelativeUri().ShouldBe("workorder/search?Creator=tlovejoy");
    }

    [Test]
    public void FromQuery_InvalidPriorityOrNumbers_FallsBackToDefaults()
    {
        var state = WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?>
        {
            ["Priority"] = "Urgent", ["CompletedWithinDays"] = "-4", ["Page"] = "zero", ["Status"] = "Bogus",
            ["OverdueOnly"] = "yes", ["Dir"] = "sideways"
        });

        state.ShouldBe(WorkOrderSearchUrlState.Default);
        WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?> { ["Priority"] = "3" }).Priority.ShouldBeNull();
        WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?> { ["CompletedWithinDays"] = "4000" })
            .CompletedWithinDays.ShouldBeNull();
    }

    [Test]
    public void FromQuery_IsCaseInsensitiveForKeysAndEnumValues()
    {
        var state = WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?>
        {
            ["priority"] = "high", ["status"] = "inprogress", ["overdueonly"] = "TRUE", ["q"] = "  lights  "
        });

        state.Priority.ShouldBe(WorkOrderPriority.High);
        state.Status.ShouldBe(WorkOrderStatus.InProgress.Key);
        state.OverdueOnly.ShouldBeTrue();
        state.Q.ShouldBe("lights");
    }

    [Test]
    public void ToRelativeUri_AssignedToMe_OmitsAssignee()
    {
        var state = new WorkOrderSearchUrlState { Assignee = "jpalermo", AssignedToMe = true };

        state.ToRelativeUri().ShouldBe("workorder/search?AssignedToMe=true");
    }

    [Test]
    public void ToRelativeUri_WaitingOnVendorOnly_WritesFlag()
    {
        var state = new WorkOrderSearchUrlState { WaitingOnVendorOnly = true };

        state.ToRelativeUri().ShouldBe("workorder/search?WaitingOnVendorOnly=true");
        WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?> { ["WaitingOnVendorOnly"] = "true" })
            .WaitingOnVendorOnly.ShouldBeTrue();
    }

    [Test]
    public void ToRelativeUri_DirOnlyWithSort()
    {
        new WorkOrderSearchUrlState { Sort = "Title", Descending = true }.ToRelativeUri()
            .ShouldBe("workorder/search?Sort=Title&Dir=desc");
        new WorkOrderSearchUrlState { Sort = "Title" }.ToRelativeUri().ShouldBe("workorder/search?Sort=Title");
    }

    [Test]
    public void DueDateSort_RoundTripsThroughUrl_AndUnknownSortIsDropped()
    {
        var ascending = new WorkOrderSearchUrlState { Sort = "DueDate" };
        ascending.ToRelativeUri().ShouldBe("workorder/search?Sort=DueDate");
        WorkOrderSearchUrlState.FromQuery(ParseQuery(ascending.ToRelativeUri())).ShouldBe(ascending);

        var descending = new WorkOrderSearchUrlState { Sort = "DueDate", Descending = true };
        descending.ToRelativeUri().ShouldBe("workorder/search?Sort=DueDate&Dir=desc");
        WorkOrderSearchUrlState.FromQuery(ParseQuery(descending.ToRelativeUri())).ShouldBe(descending);

        WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?> { ["Sort"] = "NotAColumn" }).Sort.ShouldBeNull();
    }

    [TestCase("Creator")]
    [TestCase("Assignee")]
    [TestCase("Status")]
    [TestCase("Priority")]
    [TestCase("Building")]
    [TestCase("Room")]
    [TestCase("NoBuilding")]
    [TestCase("NoRoom")]
    [TestCase("Q")]
    [TestCase("AssignedToMe")]
    [TestCase("OpenOnly")]
    [TestCase("OverdueOnly")]
    [TestCase("DueThisWeekOnly")]
    [TestCase("WaitingOnVendorOnly")]
    [TestCase("CompletedWithinDays")]
    public void HasAnyFilter_TrueForEachFilter(string key)
    {
        var value = key switch
        {
            "Status" => WorkOrderStatus.Draft.Key,
            "Priority" => "Low",
            "CompletedWithinDays" => "7",
            "AssignedToMe" or "OpenOnly" or "OverdueOnly" or "DueThisWeekOnly" or "WaitingOnVendorOnly" or "NoBuilding" or "NoRoom" => "true",
            _ => "x"
        };

        WorkOrderSearchUrlState.FromQuery(new Dictionary<string, string?> { [key] = value }).HasAnyFilter.ShouldBeTrue();
    }

    [Test]
    public void HasAnyFilter_FalseForSortAndPageOnly()
    {
        new WorkOrderSearchUrlState { Sort = "Title", Page = 2 }.HasAnyFilter.ShouldBeFalse();
    }

    [Test]
    public void ForCard_ReplacesFiltersWithTheCardFilter()
    {
        WorkOrderSearchUrlState.ForCard(SummaryCard.Open).ToRelativeUri().ShouldBe("workorder/search?OpenOnly=true");
        WorkOrderSearchUrlState.ForCard(SummaryCard.AssignedToMe).ToRelativeUri()
            .ShouldBe("workorder/search?AssignedToMe=true&OpenOnly=true");
        WorkOrderSearchUrlState.ForCard(SummaryCard.Overdue).ToRelativeUri().ShouldBe("workorder/search?OverdueOnly=true");
        WorkOrderSearchUrlState.ForCard(SummaryCard.DueThisWeek).ToRelativeUri()
            .ShouldBe("workorder/search?DueThisWeekOnly=true");
        WorkOrderSearchUrlState.ForCard(SummaryCard.Completed).ToRelativeUri()
            .ShouldBe("workorder/search?Status=Complete&CompletedWithinDays=30");
    }

    [Test]
    public void ToRelativeUri_Building_OmitsNoBuilding()
    {
        var state = new WorkOrderSearchUrlState { Building = "Kitchen", NoBuilding = true };

        state.ToRelativeUri().ShouldBe("workorder/search?Building=Kitchen");
    }

    [Test]
    public void ToRelativeUri_NoBuilding_WritesWhenNoBuildingText()
    {
        var state = new WorkOrderSearchUrlState { NoBuilding = true };

        state.ToRelativeUri().ShouldBe("workorder/search?NoBuilding=true");
    }

    [Test]
    public void ToRelativeUri_Room_OmitsNoRoom()
    {
        var state = new WorkOrderSearchUrlState { Room = "101", NoRoom = true };

        state.ToRelativeUri().ShouldBe("workorder/search?Room=101");
    }

    [Test]
    public void ToRelativeUri_NoRoom_WritesWhenNoRoomText()
    {
        var state = new WorkOrderSearchUrlState { NoRoom = true };

        state.ToRelativeUri().ShouldBe("workorder/search?NoRoom=true");
    }

    private static Dictionary<string, string?> ParseQuery(string uri) =>
        uri.Split('?', 2)[1].Split('&')
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => p[0], p => (string?)Uri.UnescapeDataString(p[1]));

    [Test]
    public void ToQueryString_Defaults_ReturnsEmpty()
    {
        WorkOrderSearchUrlState.Default.ToQueryString().ShouldBe(string.Empty);
    }

    [Test]
    public void ToQueryString_RoundTripsThroughRelativeUriForQueryAndFromQuery()
    {
        var state = new WorkOrderSearchUrlState
        {
            Creator = "hsimpson",
            Priority = WorkOrderPriority.High,
            OverdueOnly = true,
            Q = "leaky roof & more"
        };

        var query = state.ToQueryString();

        query.ShouldNotStartWith("?");
        WorkOrderSearchUrlState.RelativeUriForQuery(query).ShouldBe(state.ToRelativeUri());
        var parsed = WorkOrderSearchUrlState.FromQuery(query.Split('&')
            .Select(part => part.Split('=', 2))
            .ToDictionary(kv => kv[0], kv => (string?)Uri.UnescapeDataString(kv[1])));
        parsed.ShouldBe(state);
    }

    [Test]
    public void RelativeUriForQuery_Empty_ReturnsBareSearchPath()
    {
        WorkOrderSearchUrlState.RelativeUriForQuery(string.Empty).ShouldBe("workorder/search");
    }
}
