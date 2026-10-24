using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

public class RemotableRequestTests
{
    [Test]
    public void ShouldSerialize()
    {
        AssertRemotable(new ForecastQuery());
        AssertRemotable(new[] { ObjectMother.Faker<WeatherForecast>() });
        AssertRemotable(new HealthCheckRemotableRequest());
        AssertRemotable(HealthStatus.Degraded);
        AssertRemotable(ObjectMother.Faker<WorkOrderSpecificationQuery>());
        AssertRemotable(WorkOrderStatus.Draft);
        AssertRemotable(new RecentWorkOrdersQuery(10));
    }

    [Test]
    public void ShouldBeRemotableCompatible()
    {
        var order = ObjectMother.Faker<WorkOrder>();
        AssertRemotable(order);
        AssertRemotable(new ServerHealthCheckQuery());
        AssertRemotable(ObjectMother.Faker<Role>());

        var employee = ObjectMother.Faker<Employee>();
        var role = ObjectMother.Faker<Role>();
        employee.AddRole(role);
        var rehydratedRole = ((Employee)AssertRemotable(employee)).Roles.Single(role1 => role1 == role);
        ObjectMother.AssertAllProperties(role, rehydratedRole);

        AssertRemotable(new SaveDraftCommand(order, employee));
        AssertRemotable(new StateCommandResult(order, "Save", "message"));
    }

    [Test]
    public void ShouldSerializeStateCommand()
    {
        var order = ObjectMother.Faker<WorkOrder>();
        var employee = ObjectMother.Faker<Employee>();
        IStateCommand command = new SaveDraftCommand(order, employee);
        AssertRemotable(command);
    }

    [Test]
    public void ShouldRoundTripSpecificationQueryWithNewFilters()
    {
        var query = new WorkOrderSpecificationQuery();
        query.MatchPriority(WorkOrderPriority.High);
        query.MatchBuilding("Sanctuary");
        query.MatchOpenOnly(true);
        query.MatchDueThisWeekOnly(true);
        query.MatchCompletedWithinDays(30);
        query.MatchSearchText("organ");

        var rehydrated = SimulateRemoteObject(query);

        rehydrated.Priority.ShouldBe(WorkOrderPriority.High);
        rehydrated.Building.ShouldBe("Sanctuary");
        rehydrated.OpenOnly.ShouldBeTrue();
        rehydrated.DueThisWeekOnly.ShouldBeTrue();
        rehydrated.CompletedWithinDays.ShouldBe(30);
        rehydrated.SearchText.ShouldBe("organ");
        rehydrated.ShouldBe(query);
    }

    [Test]
    public void ShouldRoundTripSearchSummaryQuery()
    {
        AssertRemotable(new WorkOrderSearchSummaryQuery("tlovejoy"));
        AssertRemotable(new WorkOrderSearchSummaryQuery(null));
        AssertRemotable(new WorkOrderBuildingsQuery());
        AssertRemotable(new SavedSearchViewsQuery("tlovejoy"));
        AssertRemotable(new SaveSearchViewCommand("tlovejoy", "Mine", "status=Draft"));
        AssertRemotable(new DeleteSearchViewCommand("tlovejoy", Guid.NewGuid()));
        var summary = new WorkOrderSearchSummary
        {
            OpenCount = 24, OpenedLast7Days = 3, AssignedToMeCount = 2, AssignedToMeTotal = 4,
            AssignedToMeDueToday = 1, OverdueCount = 3, DueThisWeekCount = 6, CompletedLast30Days = 41, AverageDaysToClose = 2.4,
            CreatedByMeCount = 7, InProgressCount = 5
        };

        var rehydrated = SimulateRemoteObject(summary);

        ObjectMother.AssertAllProperties(summary, rehydrated);
    }

    [Test]
    public void ShouldRoundTripBuildingRoomSummaryQueryAndResult()
    {
        AssertRemotable(new BuildingRoomSummaryQuery());
        BuildingSummary[] summary =
        [
            new()
            {
                Building = "Sanctuary",
                Rooms =
                [
                    new RoomSummary { Room = "101", OpenCount = 3, OverdueCount = 1, CompletedLast30Days = 2, TotalCount = 7 },
                    new RoomSummary { Room = "", OpenCount = 1, TotalCount = 1 }
                ]
            },
            new() { Building = "", Rooms = [new RoomSummary { Room = "", CompletedLast30Days = 4, TotalCount = 5 }] }
        ];

        var rehydrated = SimulateRemoteObject(summary);

        rehydrated.Length.ShouldBe(2);
        rehydrated[0].Building.ShouldBe("Sanctuary");
        rehydrated[0].Rooms.Length.ShouldBe(2);
        ObjectMother.AssertAllProperties(summary[0].Rooms[0], rehydrated[0].Rooms[0]);
        ObjectMother.AssertAllProperties(summary[0].Rooms[1], rehydrated[0].Rooms[1]);
        rehydrated[1].Building.ShouldBe("");
        ObjectMother.AssertAllProperties(summary[1].Rooms[0], rehydrated[1].Rooms.Single());
    }

    [Test]
    public void ShouldRoundTripReportsQueryAndData()
    {
        AssertRemotable(new WorkOrderReportsQuery());
        var data = new WorkOrderReportsData
        {
            CountByStatus = { ["Draft"] = 2, ["Assigned"] = 1 },
            CountByPriority = { ["High"] = 3 },
            CountByBuilding = { ["Sanctuary"] = 3 },
            CompletedPerWeek = { new WorkOrderReportsData.WeeklyCount { WeekEndingDate = new DateOnly(2026, 10, 11), Count = 4 } },
            AverageDaysToClose = 2.5, OverdueCount = 1, CompletedLast30Days = 9
        };

        var rehydrated = SimulateRemoteObject(data);

        rehydrated.CountByStatus["Draft"].ShouldBe(2);
        rehydrated.CountByBuilding["Sanctuary"].ShouldBe(3);
        rehydrated.CompletedPerWeek.Single().WeekEndingDate.ShouldBe(new DateOnly(2026, 10, 11));
        rehydrated.CompletedPerWeek.Single().Count.ShouldBe(4);
        rehydrated.AverageDaysToClose.ShouldBe(2.5);
        rehydrated.OverdueCount.ShouldBe(1);
        rehydrated.CompletedLast30Days.ShouldBe(9);
    }

    [Test]
    public void ShouldRoundTripTeamAndVendorRequests()
    {
        AssertRemotable(new TeamsQuery());
        AssertRemotable(new VendorsQuery());
        AssertRemotable(new CreateVendorCommand("Acme", "HVAC", "Pat", "pat@acme.test", "555", false));
        AssertRemotable(new CreateTeamResult(true, "Team 'Grounds' created.", Guid.NewGuid()));
        AssertRemotable(new CreateVendorResult(false, "A vendor named 'Acme' already exists.", null));

        var createTeam = new CreateTeamCommand("Grounds", "Lawn and snow", ["u1", "u2"]);
        var rehydratedCommand = SimulateRemoteObject(createTeam);

        rehydratedCommand.Name.ShouldBe("Grounds");
        rehydratedCommand.Description.ShouldBe("Lawn and snow");
        rehydratedCommand.MemberUserNames.ShouldBe(["u1", "u2"]);
    }

    [Test]
    public void ShouldRoundTripTeamWithMembersAndVendor()
    {
        var team = new Team { Id = Guid.NewGuid(), Name = "Grounds", Description = "Lawn and snow" };
        var first = ObjectMother.Faker<Employee>();
        var second = ObjectMother.Faker<Employee>();
        team.AddMember(first);
        team.AddMember(second);

        var rehydrated = (Team)AssertRemotable(team);

        rehydrated.MemberCount.ShouldBe(2);
        rehydrated.Members.Select(m => m.Id).ShouldBe([first.Id, second.Id], ignoreOrder: true);

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(), Name = "Acme", Trade = "HVAC", ContactName = "Pat",
            EmailAddress = "pat@acme.test", PhoneNumber = "555", IsActive = false
        };
        AssertRemotable(vendor);
    }

    [Test]
    public void ShouldRoundTripShelveReason()
    {
        var order = ObjectMother.Faker<WorkOrder>();
        var employee = ObjectMother.Faker<Employee>();
        var command = new InProgressToAssignedCommand(order, employee) { Reason = "Waiting on parts" };

        var rehydrated = SimulateRemoteObject(command);

        rehydrated.Reason.ShouldBe("Waiting on parts");

        var change = new WorkOrderStatusChange
        {
            Id = Guid.NewGuid(),
            WorkOrderId = order.Id,
            FromStatus = WorkOrderStatus.InProgress,
            ToStatus = WorkOrderStatus.Assigned,
            ChangedById = employee.Id,
            ChangedDate = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc),
            Reason = "Waiting on parts"
        };
        var rehydratedChange = SimulateRemoteObject(change);
        rehydratedChange.Reason.ShouldBe("Waiting on parts");
    }

    public static object AssertRemotable(object theObject)
    {
        var rehydratedQuery = SimulateRemoteObject(theObject);

        ObjectMother.AssertAllProperties(theObject, rehydratedQuery);
        rehydratedQuery.ShouldBe(theObject);
        return rehydratedQuery;
    }

    // ReSharper disable once MemberCanBePrivate.Global -- called from IntegrationTests and UnitTests.LlmGateway across assemblies
    public static object SimulateRemoteObject(object theObject)
    {
        var json = new WebServiceMessage(theObject).GetJson();
        var message = JsonSerializer.Deserialize<WebServiceMessage>(json);
        var rehydratedQuery = message!.GetBodyObject();
        return rehydratedQuery;
    }

    public static T SimulateRemoteObject<T>(T theObject)
    {
        return (T)SimulateRemoteObject((object)theObject! ?? throw new InvalidOperationException());
    }
}