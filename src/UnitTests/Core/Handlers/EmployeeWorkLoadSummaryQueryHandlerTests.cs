using System.Data.Common;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Handlers;

[TestFixture]
public class EmployeeWorkLoadSummaryQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 18, 0, 0, TimeSpan.Zero);

    private string _dbPath = null!;
    private CountingContext _context = null!;
    private FixedClock _clock = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ewls_test_{Guid.NewGuid():N}.db");
        _clock = new FixedClock(Now);
        _context = new CountingContext(new FileDbConfig($"Data Source={_dbPath}"));
        _context.Database.EnsureCreated();
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
    public async Task ShouldCountEveryAssignableEmployee_InOneSqlCommand()
    {
        var today = ChurchTimeZone.Today(_clock);
        var since = _clock.GetUtcNow().DateTime.AddDays(-30);
        await GivenAssignableWorkloadAsync(today, since);

        _context.Commands.Reset();
        var summaries = await new EmployeeWorkLoadSummaryQueryHandler(_context, _clock, new ChurchCalendar(_clock))
            .Handle(new EmployeeWorkLoadSummaryQuery());

        ThenOneGroupedSql(_context.Commands);
        ThenSummariesMatch(summaries);
    }

    [Test]
    public void ShouldTranslateToOneGroupedStatement_OnSqlServerAndSqlite()
    {
        AssertGroupedSql(
            "Server=localhost;Database=TranslationOnly;Integrated Security=false;TrustServerCertificate=true");
        AssertGroupedSql("Data Source=:memory:");
    }

    private async Task GivenAssignableWorkloadAsync(DateOnly today, DateTime since)
    {
        var creator = EmployeeBuilder.Create("creator").WithFirstName("Cre").WithLastName("Ator")
            .WithEmail("creator@example.test").WithRole("Creator", canCreate: false, canFulfill: false).Build();
        var worker = EmployeeBuilder.Create("worker").WithFirstName("Wor").WithLastName("Ker")
            .WithMiddleName("Mid").WithEmail("worker@example.test")
            .WithRole("Worker", canCreate: false, canFulfill: true)
            .WithRole("Lead", canCreate: false, canFulfill: true).Build();
        var idle = EmployeeBuilder.Create("idle").WithFirstName("Id").WithLastName("Le")
            .WithEmail("idle@example.test").WithRole("Worker", canCreate: false, canFulfill: true).Build();
        var other = EmployeeBuilder.Create("other").WithFirstName("Ot").WithLastName("Her")
            .WithEmail("other@example.test").WithRole("Worker", canCreate: false, canFulfill: true).Build();
        _context.AddRange(creator, worker, idle, other);
        _context.Set<WorkOrder>().AddRange(
            EmployeeWorkLoadSummarySteps.AssignableOrders(
                new EmployeeWorkLoadSummarySteps.People(creator, worker, other), today, since));
        await _context.SaveChangesAsync();
    }

    private static void ThenOneGroupedSql(CommandCounter commands)
    {
        commands.ReaderCommands.ShouldBe(1, string.Join(Environment.NewLine, commands.Texts));
        commands.OtherCommands.ShouldBe(0);
        var sql = commands.Texts.Single();
        sql.ShouldContain("GROUP BY");
        sql.ShouldContain("SUM(");
    }

    private void ThenSummariesMatch(EmployeeWorkLoadSummary[] summaries)
    {
        summaries.Select(summary => summary.Employee.UserName).ShouldBe(["other", "worker", "idle"]);
        var workerSummary = summaries.Single(summary => summary.Employee.UserName == "worker");
        workerSummary.Employee.GetFullName().ShouldBe("Wor Mid Ker");
        workerSummary.Employee.Roles.Select(role => role.Name).ShouldBe(["Lead", "Worker"], ignoreOrder: true);
        workerSummary.Employee.CanFulfillWorkOrder().ShouldBeTrue();
        ThenLoadIs(workerSummary.Load, Expected(5, 2, 2, 2));

        var idleSummary = summaries.Single(summary => summary.Employee.UserName == "idle");
        ThenLoadIs(idleSummary.Load, Expected(0, 0, 0, 0));

        var otherSummary = summaries.Single(summary => summary.Employee.UserName == "other");
        ThenLoadIs(otherSummary.Load, Expected(1, 1, 1, 0));
        ThenMatchesPerEmployee(summaries);
    }

    private static EmployeeWorkLoad Expected(
        int assignedAndOpen, int inProgress, int overdue, int completedLast30Days) =>
        new()
        {
            AssignedAndOpen = assignedAndOpen,
            InProgress = inProgress,
            Overdue = overdue,
            CompletedLast30Days = completedLast30Days
        };

    private static void ThenLoadIs(EmployeeWorkLoad load, EmployeeWorkLoad expected)
    {
        load.AssignedAndOpen.ShouldBe(expected.AssignedAndOpen);
        load.InProgress.ShouldBe(expected.InProgress);
        load.Overdue.ShouldBe(expected.Overdue);
        load.CompletedLast30Days.ShouldBe(expected.CompletedLast30Days);
    }

    private void ThenMatchesPerEmployee(EmployeeWorkLoadSummary[] summaries)
    {
        var perEmployee = new EmployeeWorkLoadQueryHandler(_context, _clock, new ChurchCalendar(_clock));
        foreach (var summary in summaries)
        {
            var load = perEmployee.Handle(new EmployeeWorkLoadQuery(summary.Employee.UserName))
                .GetAwaiter().GetResult();
            ThenLoadIs(summary.Load, load);
        }
    }

    private void AssertGroupedSql(string connectionString)
    {
        using var context = new DataContext(new FileDbConfig(connectionString));
        var sql = new EmployeeWorkLoadSummaryQueryHandler(context, _clock, new ChurchCalendar(_clock))
            .SummaryQuery()
            .ToQueryString();

        TestContext.Out.WriteLine($"-- {context.Database.ProviderName}{Environment.NewLine}{sql}");
        sql.ShouldContain("GROUP BY");
        sql.ShouldContain("SUM(");
    }

    private sealed class CountingContext(IDatabaseConfiguration configuration) : DataContext(configuration)
    {
        public CommandCounter Commands { get; } = new();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(Commands);
            base.OnConfiguring(optionsBuilder);
        }
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int ReaderCommands { get; private set; }
        public int OtherCommands { get; private set; }
        public List<string> Texts { get; } = [];

        public void Reset()
        {
            ReaderCommands = 0;
            OtherCommands = 0;
            Texts.Clear();
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            NoteReader(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            NoteReader(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result)
        {
            NoteOther(command);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            NoteOther(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData,
            InterceptionResult<object> result)
        {
            NoteOther(command);
            return base.ScalarExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            NoteOther(command);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void NoteReader(DbCommand command)
        {
            ReaderCommands++;
            Texts.Add(command.CommandText);
        }

        private void NoteOther(DbCommand command)
        {
            OtherCommands++;
            Texts.Add(command.CommandText);
        }
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
