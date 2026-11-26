using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

/// <summary>
/// Pins that the handlers read "today" from the injected <see cref="ChurchCalendar"/>: at 03:00 UTC on
/// October 15 it is still October 14 in Chicago, so an order due October 14 is not yet overdue (it would be
/// under a UTC date) and an order due October 15 is not yet due (it would be under a UTC date).
/// </summary>
[TestFixture]
public class ChurchCalendarTodayHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly ChurchToday = new(2026, 10, 14);

    private string _dbPath = null!;
    private DataContext _context = null!;
    private TimeProvider _clock = null!;
    private ChurchCalendar _calendar = null!;

    [SetUp]
    public async Task SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cct_test_{Guid.NewGuid():N}.db");
        _clock = new FixedClock(Now);
        _calendar = new ChurchCalendar(_clock);
        _context = new DataContext(new FileDbConfig($"Data Source={_dbPath}"));
        await _context.Database.EnsureCreatedAsync();
        await GivenOrdersAroundTheDateLineAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Test]
    public async Task BuildingRoomSummary_WhenUtcDateIsAheadOfChicago_CountsOverdueByChurchToday()
    {
        var handler = new BuildingRoomSummaryQueryHandler(_context, _clock, _calendar);

        var buildings = await handler.Handle(new BuildingRoomSummaryQuery());

        _calendar.Today.ShouldBe(ChurchToday);
        var room = buildings.ShouldHaveSingleItem().Rooms.ShouldHaveSingleItem();
        room.OpenCount.ShouldBe(3);
        room.OverdueCount.ShouldBe(1);
    }

    [Test]
    public async Task EmployeeWorkLoad_WhenUtcDateIsAheadOfChicago_CountsOverdueByChurchToday()
    {
        var handler = new EmployeeWorkLoadQueryHandler(_context, _clock, _calendar);

        var load = await handler.Handle(new EmployeeWorkLoadQuery("worker"));

        _calendar.Today.ShouldBe(ChurchToday);
        load.AssignedAndOpen.ShouldBe(3);
        load.Overdue.ShouldBe(1);
    }

    [Test]
    public async Task EmployeeWorkLoadSummary_WhenUtcDateIsAheadOfChicago_CountsOverdueByChurchToday()
    {
        var handler = new EmployeeWorkLoadSummaryQueryHandler(_context, _clock, _calendar);

        var summaries = await handler.Handle(new EmployeeWorkLoadSummaryQuery());

        _calendar.Today.ShouldBe(ChurchToday);
        var worker = summaries.Single(summary => summary.Employee.UserName == "worker");
        worker.Load.AssignedAndOpen.ShouldBe(3);
        worker.Load.Overdue.ShouldBe(1);
    }

    [Test]
    public async Task NotificationsPanel_WhenUtcDateIsAheadOfChicago_ListsDueThroughChurchToday()
    {
        var handler = new NotificationsPanelQueryHandler(_context, _clock, _calendar);

        var summary = await handler.Handle(new NotificationsPanelQuery("worker"));

        _calendar.Today.ShouldBe(ChurchToday);
        summary.OverdueAndDueToday.Select(wo => wo.DueDate)
            .ShouldBe([ChurchToday, ChurchToday.AddDays(-1)]);
    }

    private async Task GivenOrdersAroundTheDateLineAsync()
    {
        var creator = EmployeeBuilder.Create("creator").WithFirstName("Cre").WithLastName("Ator")
            .WithEmail("creator@example.test").WithRole("Creator", canCreate: true, canFulfill: false).Build();
        var worker = EmployeeBuilder.Create("worker").WithFirstName("Wor").WithLastName("Ker")
            .WithEmail("worker@example.test").WithRole("Worker", canCreate: false, canFulfill: true).Build();
        _context.AddRange(creator, worker);
        foreach (var dueDate in new[] { ChurchToday.AddDays(-1), ChurchToday, ChurchToday.AddDays(1) })
        {
            _context.Add(WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).AssignedTo(worker)
                .InBuilding("Main").InRoom("101").DueOn(dueDate).Build());
        }

        await _context.SaveChangesAsync();
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool()
        {
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
