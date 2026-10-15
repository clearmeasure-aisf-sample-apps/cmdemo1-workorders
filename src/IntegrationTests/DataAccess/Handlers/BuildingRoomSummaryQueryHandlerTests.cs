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
        var seed = await SeedVariantsAsync("brmatch");

        var buildings = Own(await Handle(), seed.Tag);

        buildings.Sum(b => b.Rooms.Sum(r => r.TotalCount)).ShouldBe(seed.InOwnBuildings);
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
        var seed = await SeedVariantsAsync("bronce");

        var all = await Handle();

        all.Select(b => b.Building).ShouldBeUnique();
        foreach (var building in all)
        {
            building.Rooms.ShouldNotBeEmpty();
            building.Rooms.Select(r => r.Room).ShouldBeUnique();
        }

        var buildings = Own(all, seed.Tag);
        foreach (var building in buildings)
        {
            // A building the database also counts under another returned entry would make this search larger.
            (await SearchCountAsync(building.Building, null)).ShouldBe(building.Rooms.Sum(r => r.TotalCount),
                $"building '{building.Building}'");
            foreach (var room in building.Rooms)
            {
                (await SearchCountAsync(building.Building, room.Room)).ShouldBe(room.TotalCount,
                    $"building '{building.Building}', room '{room.Room}'");
            }
        }

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
        var creator = NewEmployee("brbound");
        var now = Now.DateTime;
        await SaveAsync(creator,
            Order(creator, WorkOrderStatus.InProgress, boundary, "1", due: ChurchToday),
            Order(creator, WorkOrderStatus.Assigned, boundary, "1", due: ChurchToday.AddDays(-1)),
            Order(creator, WorkOrderStatus.Complete, boundary, "1", completed: now.AddDays(-30)),
            Order(creator, WorkOrderStatus.Complete, boundary, "1", completed: now.AddDays(-30).AddSeconds(-1)),
            Order(creator, WorkOrderStatus.Cancelled, annex, "9", due: ChurchToday.AddDays(-5)),
            Order(creator, WorkOrderStatus.Complete, annex, "9", completed: now.AddDays(-60)));

        var buildings = Own(await Handle(), tag);

        buildings.Select(b => b.Building).ShouldBe([annex, boundary]);
        AssertCounts(buildings[1].Rooms.Single(), "1", open: 2, overdue: 1, completed: 1, total: 4);
        AssertCounts(buildings[0].Rooms.Single(), "9", open: 0, overdue: 0, completed: 0, total: 2);
    }

    [Test]
    public async Task ShouldNotReturnUnknownBuilding_AndListEachBuildingAndRoomOnce()
    {
        var unknown = $"empty {TestData.Tag()}";

        var buildings = await Handle();

        buildings.ShouldNotContain(b => b.Building == unknown);
        buildings.Select(b => b.Building).ShouldBeUnique();
        foreach (var building in buildings)
        {
            building.Rooms.Select(r => r.Room).ShouldBeUnique();
        }
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
            var sql = new BuildingRoomSummaryQueryHandler(context, TestData.FixedClock(Now)).CountsQuery().ToQueryString();

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
    /// rooms, and every status relevant to the counts. The named buildings carry a unique tag; blank
    /// buildings cannot, so they share the "" entry with other tests' rows.
    /// </summary>
    private static async Task<SeededVariants> SeedVariantsAsync(string prefix)
    {
        var tag = TestData.Tag();
        var hall = $"Hall {tag}";
        var gym = $"Gym {tag}";
        var creator = NewEmployee(prefix);
        var now = Now.DateTime;
        WorkOrder[] orders =
        [
            Order(creator, WorkOrderStatus.Assigned, hall, "101", due: ChurchToday.AddDays(-1)),
            Order(creator, WorkOrderStatus.Draft, $" {hall.ToUpperInvariant()} ", "101 ", due: ChurchToday),
            Order(creator, WorkOrderStatus.Complete, hall.ToLowerInvariant(), "102", completed: now.AddDays(-30)),
            Order(creator, WorkOrderStatus.Complete, hall.ToLowerInvariant(), "102", completed: now.AddDays(-31)),
            Order(creator, WorkOrderStatus.InProgress, hall, "101"),
            Order(creator, WorkOrderStatus.Cancelled, null, null, due: ChurchToday.AddDays(-12)),
            Order(creator, WorkOrderStatus.Complete, "", "", completed: now.AddDays(-60)),
            Order(creator, WorkOrderStatus.Assigned, "   ", "  ", due: ChurchToday.AddDays(-12)),
            Order(creator, WorkOrderStatus.Draft, null, "201"),
            Order(creator, WorkOrderStatus.Complete, "", "201", completed: now.AddDays(-2)),
            Order(creator, WorkOrderStatus.InProgress, "   ", " 201"),
            Order(creator, WorkOrderStatus.InProgress, gym, null, due: ChurchToday.AddDays(-3)),
            Order(creator, WorkOrderStatus.Cancelled, gym, "  "),
            Order(creator, WorkOrderStatus.Complete, $" {gym}", "", completed: now.AddDays(-1))
        ];
        await SaveAsync(creator, orders);
        return new SeededVariants(tag, gym, orders.Count(o => !string.IsNullOrWhiteSpace(o.Building)));
    }

    private sealed record SeededVariants(string Tag, string Gym, int InOwnBuildings);

    private static BuildingSummary[] Own(IEnumerable<BuildingSummary> buildings, string tag) =>
        buildings.Where(b => b.Building.Contains(tag, StringComparison.OrdinalIgnoreCase)).ToArray();

    private static Employee NewEmployee(string prefix)
    {
        var userName = TestData.UserName(prefix);
        return new Employee(userName, "Room", "Summary", TestData.Email(userName));
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
        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TestData.FixedClock(Now));
        return (await handler.Handle(query)).Length;
    }

    private static async Task<BuildingSummary[]> Handle()
    {
        var handler = new BuildingRoomSummaryQueryHandler(
            TestHost.GetRequiredService<DataContext>(), TestData.FixedClock(Now));
        return await handler.Handle(new BuildingRoomSummaryQuery());
    }

    private static WorkOrder Order(Employee creator, WorkOrderStatus status, string? building,
        string? room, DateTime? completed = null, DateOnly? due = null) =>
        new()
        {
            Creator = creator, Number = TestData.Number("V"), Title = $"Room summary {creator.UserName}", Status = status,
            Building = building,
            RoomNumber = room, CreatedDate = Now.DateTime.AddDays(-90), CompletedDate = completed, DueDate = due
        };

    private static async Task SaveAsync(Employee creator, params WorkOrder[] orders)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.AddRange(orders.Cast<object>());
        await context.SaveChangesAsync();
    }

    private sealed class StubDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool()
        {
        }
    }
}
