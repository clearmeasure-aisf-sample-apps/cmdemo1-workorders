using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
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
        var seeded = await SeedVariantsAsync("brmatch");

        var buildings = await Handle();

        buildings.Sum(b => b.Rooms.Sum(r => r.TotalCount)).ShouldBe(seeded);
        foreach (var building in buildings)
        {
            foreach (var room in building.Rooms)
            {
                var where = $"building '{building.Building}', room '{room.Room}'";
                room.TotalCount.ShouldBe(await SearchCountAsync(building.Building, room.Room), where);
                room.OpenCount.ShouldBe(await SearchCountAsync(building.Building, room.Room, q => q.MatchOpenOnly(true)), where);
                room.OverdueCount.ShouldBe(await SearchCountAsync(building.Building, room.Room, q => q.MatchOverdueOnly(true)), where);
                room.CompletedLast30Days.ShouldBe(
                    await SearchCountAsync(building.Building, room.Room, q => q.MatchCompletedWithinDays(30)), where);
            }
        }
    }

    [Test]
    public async Task ShouldReturnEachBuildingAndRoomOnce_UnderDatabaseComparison()
    {
        var seeded = await SeedVariantsAsync("bronce");

        var buildings = await Handle();

        buildings.Select(b => b.Building).ShouldBeUnique();
        foreach (var building in buildings)
        {
            building.Rooms.ShouldNotBeEmpty();
            building.Rooms.Select(r => r.Room).ShouldBeUnique();
            // A building the database also counts under another returned entry would make this search larger.
            (await SearchCountAsync(building.Building, null)).ShouldBe(building.Rooms.Sum(r => r.TotalCount),
                $"building '{building.Building}'");
            foreach (var room in building.Rooms)
            {
                (await SearchCountAsync(building.Building, room.Room)).ShouldBe(room.TotalCount,
                    $"building '{building.Building}', room '{room.Room}'");
            }
        }

        buildings.Sum(b => b.Rooms.Sum(r => r.TotalCount)).ShouldBe(seeded);
        var noBuilding = buildings.Single(b => b.Building == "");
        noBuilding.Rooms.Count(r => r.Room == "").ShouldBe(1);
        buildings.Single(b => b.Building == "Gym").Rooms.Count(r => r.Room == "").ShouldBe(1);
    }

    [Test]
    public async Task ShouldCountBoundaries_WithFixedClock()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("brbound", "Room", "Bounds", "rb@example.com");
        var now = Now.DateTime;
        await SaveAsync(creator,
            Order(creator, "D000001", WorkOrderStatus.InProgress, "Boundary", "1", due: ChurchToday),
            Order(creator, "D000002", WorkOrderStatus.Assigned, "Boundary", "1", due: ChurchToday.AddDays(-1)),
            Order(creator, "D000003", WorkOrderStatus.Complete, "Boundary", "1", completed: now.AddDays(-30)),
            Order(creator, "D000004", WorkOrderStatus.Complete, "Boundary", "1", completed: now.AddDays(-30).AddSeconds(-1)),
            Order(creator, "D000005", WorkOrderStatus.Cancelled, "Annex", "9", due: ChurchToday.AddDays(-5)),
            Order(creator, "D000006", WorkOrderStatus.Complete, "Annex", "9", completed: now.AddDays(-60)));

        var buildings = await Handle();

        buildings.Select(b => b.Building).ShouldBe(["Annex", "Boundary"]);
        AssertCounts(buildings[1].Rooms.Single(), "1", open: 2, overdue: 1, completed: 1, total: 4);
        AssertCounts(buildings[0].Rooms.Single(), "9", open: 0, overdue: 0, completed: 0, total: 2);
    }

    [Test]
    public async Task ShouldReturnEmpty_WhenNoWorkOrders()
    {
        new DatabaseTests().Clean();

        (await Handle()).ShouldBeEmpty();
    }

    [Test]
    public void ShouldTranslateToSql_OnSqlServerAndSqlite()
    {
        string[] connectionStrings =
        [
            "Server=localhost;Database=TranslationOnly;Integrated Security=false;TrustServerCertificate=true",
            "Data Source=:memory:"
        ];

        foreach (var connectionString in connectionStrings)
        {
            using var context = new DataContext(new StubDatabaseConfiguration(connectionString));
            var sql = new BuildingRoomSummaryQueryHandler(context, new FixedClock(Now)).CountsQuery().ToQueryString();

            TestContext.Out.WriteLine($"-- {context.Database.ProviderName}{Environment.NewLine}{sql}");
            sql.ShouldContain("GROUP BY");
            sql.ShouldContain("JOIN");
            sql.ShouldContain("SUM(");
        }
    }

    private static void AssertCounts(RoomSummary room, string name, int open, int overdue, int completed, int total)
    {
        room.Room.ShouldBe(name);
        room.OpenCount.ShouldBe(open);
        room.OverdueCount.ShouldBe(overdue);
        room.CompletedLast30Days.ShouldBe(completed);
        room.TotalCount.ShouldBe(total);
    }

    /// <summary>
    /// Case and whitespace variants of one building across two rooms, null/empty/blank buildings and
    /// rooms, and every status relevant to the counts. Returns the number of work orders seeded.
    /// </summary>
    private static async Task<int> SeedVariantsAsync(string userName)
    {
        new DatabaseTests().Clean();
        var creator = new Employee(userName, "Room", "Summary", $"{userName}@example.com");
        var now = Now.DateTime;
        WorkOrder[] orders =
        [
            Order(creator, "V000001", WorkOrderStatus.Assigned, "Hall", "101", due: ChurchToday.AddDays(-1)),
            Order(creator, "V000002", WorkOrderStatus.Draft, " HALL ", "101 ", due: ChurchToday),
            Order(creator, "V000003", WorkOrderStatus.Complete, "hall", "102", completed: now.AddDays(-30)),
            Order(creator, "V000004", WorkOrderStatus.Complete, "hall", "102", completed: now.AddDays(-31)),
            Order(creator, "V000005", WorkOrderStatus.InProgress, "Hall", "101"),
            Order(creator, "V000006", WorkOrderStatus.Cancelled, null, null, due: ChurchToday.AddDays(-12)),
            Order(creator, "V000007", WorkOrderStatus.Complete, "", "", completed: now.AddDays(-60)),
            Order(creator, "V000008", WorkOrderStatus.Assigned, "   ", "  ", due: ChurchToday.AddDays(-12)),
            Order(creator, "V000009", WorkOrderStatus.Draft, null, "201"),
            Order(creator, "V000010", WorkOrderStatus.Complete, "", "201", completed: now.AddDays(-2)),
            Order(creator, "V000011", WorkOrderStatus.InProgress, "   ", " 201"),
            Order(creator, "V000012", WorkOrderStatus.InProgress, "Gym", null, due: ChurchToday.AddDays(-3)),
            Order(creator, "V000013", WorkOrderStatus.Cancelled, "Gym", "  "),
            Order(creator, "V000014", WorkOrderStatus.Complete, " Gym", "", completed: now.AddDays(-1))
        ];
        await SaveAsync(creator, orders);
        return orders.Length;
    }

    private static async Task<int> SearchCountAsync(string building, string? room,
        Action<WorkOrderSpecificationQuery>? flag = null)
    {
        var query = new WorkOrderSpecificationQuery();
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

        flag?.Invoke(query);
        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), new FixedClock(Now));
        return (await handler.Handle(query)).Length;
    }

    private static async Task<BuildingSummary[]> Handle()
    {
        var handler = new BuildingRoomSummaryQueryHandler(
            TestHost.GetRequiredService<DataContext>(), new FixedClock(Now));
        return await handler.Handle(new BuildingRoomSummaryQuery());
    }

    private static WorkOrder Order(Employee creator, string number, WorkOrderStatus status, string? building,
        string? room, DateTime? completed = null, DateOnly? due = null) =>
        new()
        {
            Creator = creator, Number = number, Title = number, Status = status, Building = building,
            RoomNumber = room, CreatedDate = Now.DateTime.AddDays(-90), CompletedDate = completed, DueDate = due
        };

    private static async Task SaveAsync(Employee creator, params WorkOrder[] orders)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.AddRange(orders.Cast<object>());
        await context.SaveChangesAsync();
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool()
        {
        }
    }
}
