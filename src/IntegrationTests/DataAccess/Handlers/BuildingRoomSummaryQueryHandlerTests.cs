using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>
/// The building and room summary must equal Work Order Search for the same building and room on
/// every database. Expected values come from the search handler, never from per-provider lists.
/// </summary>
[TestFixture]
public class BuildingRoomSummaryQueryHandlerTests
{
    // 03:00 UTC on 14 October is still 13 October at the church, so overdue uses 13 October.
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly ChurchToday = new(2026, 10, 13);

    [Test]
    public async Task ShouldMatchWorkOrderSearch_ForEveryBuildingAndRoom()
    {
        var seed = await SeedVariantsAsync("brmatch");

        var buildings = Own(await Handle(), seed.Tag);

        buildings.Sum(b => b.Rooms.Sum(r => r.TotalCount)).ShouldBe(seed.InOwnBuildings);
        AssertEachRoomMatchesSearch(buildings);
    }

    [Test]
    public async Task ShouldReturnEachBuildingAndRoomOnce_UnderDatabaseComparison()
    {
        var seed = await SeedVariantsAsync("bronce");

        var all = await Handle();

        AssertUniqueBuildingsAndRooms(all);
        var buildings = Own(all, seed.Tag);
        AssertBuildingTotalsMatchSearch(buildings);

        buildings.Sum(b => b.Rooms.Sum(r => r.TotalCount)).ShouldBe(seed.InOwnBuildings);
        var noBuilding = all.Single(b => b.Building == "");
        noBuilding.Rooms.Count(r => r.Room == "").ShouldBe(1);
        all.Single(b => b.Building == seed.Gym).Rooms.Count(r => r.Room == "").ShouldBe(1);
    }

    [Test]
    public async Task ShouldCountBoundaries_WithFixedClock()
    {
        var tag = TestData.Tag();
        var boundary = $"Boundary {tag}";
        var annex = $"Annex {tag}";
        await GivenBoundaryOrdersAsync(boundary, annex);

        var buildings = Own(await Handle(), tag);

        buildings.Select(b => b.Building).ShouldBe([annex, boundary]);
        AssertCounts(buildings[1].Rooms.Single(), new ExpectedRoom("1", new RoomCounts(2, 1, 1, 4)));
        AssertCounts(buildings[0].Rooms.Single(), new ExpectedRoom("9", new RoomCounts(0, 0, 0, 2)));
    }

    [Test]
    public async Task ShouldNotReturnUnknownBuilding_AndListEachBuildingAndRoomOnce()
    {
        var unknown = $"empty {TestData.Tag()}";

        var buildings = await Handle();

        buildings.ShouldNotContain(b => b.Building == unknown);
        buildings.Select(b => b.Building).ShouldBeUnique();
        AssertUniqueRoomsPerBuilding(buildings);
    }

    [Test]
    public void ShouldTranslateToSql_OnSqlServerAndSqlite()
    {
        AssertSqlContainsGroupByJoinSum(
            "Server=localhost;Database=TranslationOnly;Integrated Security=false;TrustServerCertificate=true");
        AssertSqlContainsGroupByJoinSum("Data Source=:memory:");
    }

    private static void AssertSqlContainsGroupByJoinSum(string connectionString)
    {
        using var context = new DataContext(new StubDatabaseConfiguration(connectionString));
        var clock = TestData.FixedClock(Now);
        var sql = new BuildingRoomSummaryQueryHandler(context, clock, new ChurchCalendar(clock)).CountsQuery()
            .ToQueryString();

        TestContext.Out.WriteLine($"-- {context.Database.ProviderName}{Environment.NewLine}{sql}");
        sql.ShouldContain("GROUP BY");
        sql.ShouldContain("JOIN");
        sql.ShouldContain("SUM(");
    }

    private static void AssertCounts(RoomSummary room, ExpectedRoom expected)
    {
        room.Room.ShouldBe(expected.Name);
        room.OpenCount.ShouldBe(expected.Counts.Open);
        room.OverdueCount.ShouldBe(expected.Counts.Overdue);
        room.CompletedLast30Days.ShouldBe(expected.Counts.Completed);
        room.TotalCount.ShouldBe(expected.Counts.Total);
    }

    private sealed record RoomCounts(int Open, int Overdue, int Completed, int Total);

    private sealed record ExpectedRoom(string Name, RoomCounts Counts);

    private static void AssertEachRoomMatchesSearch(BuildingSummary[] buildings)
    {
        foreach (var building in buildings)
        {
            foreach (var room in building.Rooms)
            {
                AssertRoomMatchesSearch(building.Building, room);
            }
        }
    }

    private static void AssertRoomMatchesSearch(string building, RoomSummary room)
    {
        var where = $"building '{building}', room '{room.Room}'";
        room.TotalCount.ShouldBe(SearchCountAsync(building, room.Room).GetAwaiter().GetResult(), where);
        room.OpenCount.ShouldBe(
            SearchCountAsync(building, room.Room, q => q.MatchOpenOnly(true)).GetAwaiter().GetResult(), where);
        room.OverdueCount.ShouldBe(
            SearchCountAsync(building, room.Room, q => q.MatchOverdueOnly(true)).GetAwaiter().GetResult(), where);
        room.CompletedLast30Days.ShouldBe(
            SearchCountAsync(building, room.Room, q => q.MatchCompletedWithinDays(30)).GetAwaiter().GetResult(), where);
    }

    private static void AssertUniqueBuildingsAndRooms(BuildingSummary[] all)
    {
        all.Select(b => b.Building).ShouldBeUnique();
        foreach (var building in all)
        {
            building.Rooms.ShouldNotBeEmpty();
            building.Rooms.Select(r => r.Room).ShouldBeUnique();
        }
    }

    private static void AssertUniqueRoomsPerBuilding(BuildingSummary[] buildings)
    {
        foreach (var building in buildings)
        {
            building.Rooms.Select(r => r.Room).ShouldBeUnique();
        }
    }

    private static void AssertBuildingTotalsMatchSearch(BuildingSummary[] buildings)
    {
        foreach (var building in buildings)
        {
            // A building the database also counts under another returned entry would make this search larger.
            SearchCountAsync(building.Building, null).GetAwaiter().GetResult()
                .ShouldBe(building.Rooms.Sum(r => r.TotalCount), $"building '{building.Building}'");
            foreach (var room in building.Rooms)
            {
                SearchCountAsync(building.Building, room.Room).GetAwaiter().GetResult()
                    .ShouldBe(room.TotalCount, $"building '{building.Building}', room '{room.Room}'");
            }
        }
    }

    /// <summary>
    /// Case and whitespace variants of one building across two rooms, null/empty/blank buildings and
    /// rooms, and every status relevant to the counts. The named buildings carry a unique tag; blank
    /// buildings cannot, so they share the "" entry with other tests' rows.
    /// </summary>
    private static async Task<SeededVariants> SeedVariantsAsync(string prefix)
    {
        var tag = TestData.Tag();
        var hall = $"Hall {tag}";
        var gym = $"Gym {tag}";
        var userName = TestData.UserName(prefix);
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Room").WithLastName("Summary").WithEmail(TestData.Email(userName)).Build();
        var orders = BuildingRoomSummarySteps.BuildHallOrders(creator, hall, Now, ChurchToday)
            .Concat(BuildingRoomSummarySteps.BuildBlankAndGymOrders(creator, gym, Now, ChurchToday))
            .ToArray();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator, orders);
        return new SeededVariants(tag, gym, orders.Count(o => !string.IsNullOrWhiteSpace(o.Building)));
    }

    private static async Task GivenBoundaryOrdersAsync(string boundary, string annex)
    {
        var userName = TestData.UserName("brbound");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Room").WithLastName("Summary").WithEmail(TestData.Email(userName)).Build();
        var now = Now.DateTime;
        var created = Now.DateTime.AddDays(-90);
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).InBuilding(boundary).InRoom("1")
                .CreatedAt(created).DueOn(ChurchToday).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).InBuilding(boundary).InRoom("1")
                .CreatedAt(created).DueOn(ChurchToday.AddDays(-1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).InBuilding(boundary).InRoom("1")
                .CreatedAt(created).CompletedAt(now.AddDays(-30)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).InBuilding(boundary).InRoom("1")
                .CreatedAt(created).CompletedAt(now.AddDays(-30).AddSeconds(-1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled).InBuilding(annex).InRoom("9")
                .CreatedAt(created).DueOn(ChurchToday.AddDays(-5)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).InBuilding(annex).InRoom("9")
                .CreatedAt(created).CompletedAt(now.AddDays(-60)).Build());
    }

    private sealed record SeededVariants(string Tag, string Gym, int InOwnBuildings);

    private static BuildingSummary[] Own(IEnumerable<BuildingSummary> buildings, string tag) =>
        buildings.Where(b => b.Building.Contains(tag, StringComparison.OrdinalIgnoreCase)).ToArray();

    private static async Task<int> SearchCountAsync(string building, string? room,
        Action<WorkOrderSpecificationQuery>? flag = null)
    {
        var query = new WorkOrderSpecificationQuery();
        ApplyBuildingRoom(query, building, room);
        flag?.Invoke(query);
        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TestData.FixedClock(Now), new ChurchCalendar(TestData.FixedClock(Now)));
        return (await handler.Handle(query)).Length;
    }

    private static void ApplyBuildingRoom(WorkOrderSpecificationQuery query, string building, string? room)
    {
        if (building == "")
        {
            query.MatchNoBuilding(true);
        }
        else
        {
            query.MatchBuilding(building);
        }

        if (room == "")
        {
            query.MatchNoRoom(true);
        }
        else if (room != null)
        {
            query.MatchRoom(room);
        }
    }

    private static async Task<BuildingSummary[]> Handle()
    {
        var clock = TestData.FixedClock(Now);
        var handler = new BuildingRoomSummaryQueryHandler(
            TestHost.GetRequiredService<DataContext>(), clock, new ChurchCalendar(clock));
        return await handler.Handle(new BuildingRoomSummaryQuery());
    }

    private sealed class StubDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool()
        {
        }
    }
}
