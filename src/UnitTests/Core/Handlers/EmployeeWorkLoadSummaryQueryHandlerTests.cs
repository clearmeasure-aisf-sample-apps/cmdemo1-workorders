using System.Data.Common;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
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
        var creator = Employee("creator", "Cre", "Ator", fulfill: false, "Creator");
        var worker = Employee("worker", "Wor", "Ker", fulfill: true, "Worker", "Lead");
        worker.MiddleName = "Mid";
        var idle = Employee("idle", "Id", "Le", fulfill: true, "Worker");
        var other = Employee("other", "Ot", "Her", fulfill: true, "Worker");
        _context.AddRange(creator, worker, idle, other);
        _context.Add(Order("WO-1", creator, worker, WorkOrderStatus.Draft, today.AddDays(5)));
        _context.Add(Order("WO-2", creator, worker, WorkOrderStatus.Assigned, today.AddDays(-3)));
        _context.Add(Order("WO-3", creator, worker, WorkOrderStatus.InProgress, today.AddDays(-1)));
        _context.Add(Order("WO-4", creator, worker, WorkOrderStatus.InProgress, null));
        _context.Add(Order("WO-5", creator, worker, WorkOrderStatus.Complete, today.AddDays(-10), since.AddDays(1)));
        _context.Add(Order("WO-6", creator, worker, WorkOrderStatus.Complete, today.AddDays(-90), since.AddSeconds(-1)));
        _context.Add(Order("WO-7", creator, worker, WorkOrderStatus.Cancelled, today.AddDays(-2)));
        _context.Add(Order("WO-8", creator, worker, WorkOrderStatus.Draft, today));
        _context.Add(Order("WO-9", creator, worker, WorkOrderStatus.Complete, today, since));
        _context.Add(Order("WO-10", creator, null, WorkOrderStatus.Assigned, today.AddDays(-2)));
        _context.Add(Order("WO-11", creator, creator, WorkOrderStatus.Assigned, today.AddDays(-4)));
        _context.Add(Order("WO-12", creator, other, WorkOrderStatus.InProgress, today.AddDays(-1)));
        await _context.SaveChangesAsync();

        _context.Commands.Reset();
        var handler = new EmployeeWorkLoadSummaryQueryHandler(_context, _clock);
        var summaries = await handler.Handle(new EmployeeWorkLoadSummaryQuery());

        _context.Commands.ReaderCommands.ShouldBe(1, string.Join(Environment.NewLine, _context.Commands.Texts));
        _context.Commands.OtherCommands.ShouldBe(0);
        var sql = _context.Commands.Texts.Single();
            sql.ShouldContain("GROUP BY", Case.Insensitive);
            sql.ShouldContain("SUM(", Case.Insensitive);

            summaries.Select(summary => summary.Employee.UserName).ShouldBe(["other", "worker", "idle"]);
        var workerSummary = summaries.Single(summary => summary.Employee.UserName == "worker");
        workerSummary.Employee.GetFullName().ShouldBe("Wor Mid Ker");
        workerSummary.Employee.Roles.Select(role => role.Name).ShouldBe(["Lead", "Worker"], ignoreOrder: true);
        workerSummary.Employee.CanFulfillWorkOrder().ShouldBeTrue();
        workerSummary.Load.AssignedAndOpen.ShouldBe(5);
        workerSummary.Load.InProgress.ShouldBe(2);
        workerSummary.Load.Overdue.ShouldBe(2);
        workerSummary.Load.CompletedLast30Days.ShouldBe(2);

        var idleSummary = summaries.Single(summary => summary.Employee.UserName == "idle");
        idleSummary.Load.AssignedAndOpen.ShouldBe(0);
        idleSummary.Load.InProgress.ShouldBe(0);
        idleSummary.Load.Overdue.ShouldBe(0);
        idleSummary.Load.CompletedLast30Days.ShouldBe(0);

        var otherSummary = summaries.Single(summary => summary.Employee.UserName == "other");
        otherSummary.Load.AssignedAndOpen.ShouldBe(1);
        otherSummary.Load.InProgress.ShouldBe(1);
        otherSummary.Load.Overdue.ShouldBe(1);
        otherSummary.Load.CompletedLast30Days.ShouldBe(0);

        var perEmployee = new EmployeeWorkLoadQueryHandler(_context, _clock);
        foreach (var summary in summaries)
        {
            var load = await perEmployee.Handle(new EmployeeWorkLoadQuery(summary.Employee.UserName));
            summary.Load.AssignedAndOpen.ShouldBe(load.AssignedAndOpen);
            summary.Load.InProgress.ShouldBe(load.InProgress);
            summary.Load.Overdue.ShouldBe(load.Overdue);
            summary.Load.CompletedLast30Days.ShouldBe(load.CompletedLast30Days);
        }
    }

    [Test]
    public void ShouldTranslateToOneGroupedStatement_OnSqlServerAndSqlite()
    {
        string[] connectionStrings =
        [
            "Server=localhost;Database=TranslationOnly;Integrated Security=false;TrustServerCertificate=true",
            "Data Source=:memory:"
        ];

        foreach (var connectionString in connectionStrings)
        {
            using var context = new DataContext(new FileDbConfig(connectionString));
            var sql = new EmployeeWorkLoadSummaryQueryHandler(context, _clock ?? new FixedClock(Now))
                .SummaryQuery()
                .ToQueryString();

            TestContext.Out.WriteLine($"-- {context.Database.ProviderName}{Environment.NewLine}{sql}");
            sql.ShouldContain("GROUP BY", Case.Insensitive);
            sql.ShouldContain("SUM(", Case.Insensitive);
        }
    }

    private static Employee Employee(string userName, string first, string last, bool fulfill, params string[] roleNames)
    {
        var employee = new Employee(userName, first, last, $"{userName}@example.test");
        foreach (var roleName in roleNames)
        {
            employee.AddRole(new Role(roleName, canCreate: false, canFulfill: fulfill));
        }

        return employee;
    }

    private static WorkOrder Order(string number, Employee creator, Employee? assignee, WorkOrderStatus status,
        DateOnly? dueDate, DateTime? completedDate = null)
    {
        return new WorkOrder
        {
            Number = number,
            Creator = creator,
            Assignee = assignee,
            Status = status,
            DueDate = dueDate,
            CompletedDate = completedDate
        };
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
