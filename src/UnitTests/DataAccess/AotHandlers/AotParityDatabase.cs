using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.Data.Sqlite;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary>
/// A SQLite file database seeded with one church's work orders across statuses, buildings, rooms and due dates. The
/// Native AOT handlers (linked into this project from DataAccess) and the JIT handlers they replace both read it, and
/// <see cref="Json{T}"/> writes either answer through the remotable contract so the two can be compared as the browser
/// would receive them. Building names keep one spelling: SQLite compares text case-sensitively where the SQL Server
/// collation the JIT handlers run on in production does not, and the Aot handlers fold case as SQL Server does.
/// </summary>
internal sealed class AotParityDatabase : IAsyncDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 18, 0, 0, TimeSpan.Zero);

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"aot_parity_{Guid.NewGuid():N}.db");

    private AotParityDatabase()
    {
        Context = new DataContext(new SqliteFile($"Data Source={_path}"));
    }

    public DataContext Context { get; }

    public TimeProvider Clock { get; } = FixedUtcClock.At(Now);

    private readonly Employee _creator = EmployeeBuilder.Create("pcreator").WithFirstName("Pat").WithLastName("Creator")
        .WithEmail("pat@example.test").WithRole("Creator", canCreate: true, canFulfill: false).Build();

    private readonly Employee _fulfiller = EmployeeBuilder.Create("ffixer").WithFirstName("Fran").WithLastName("Fixer")
        .WithEmail("fran@example.test").WithRole("Fulfiller", canCreate: false, canFulfill: true).Build();

    private readonly Employee _idle = EmployeeBuilder.Create("iidle").WithFirstName("Ida").WithLastName("Idle")
        .WithEmail("ida@example.test").WithRole("Fulfiller", canCreate: false, canFulfill: true).Build();

    public static async Task<AotParityDatabase> SeedAsync()
    {
        var database = new AotParityDatabase();
        await database.Context.Database.EnsureCreatedAsync();
        database.Context.AddRange(database._creator, database._fulfiller, database._idle);
        database.Context.Set<WorkOrder>().AddRange(database.WorkOrders().Select(order => order.Build()));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        return database;
    }

    /// <summary>The answer as JSON through its remotable contract, the shape UI.Server sends the browser.</summary>
    public static string Json<T>(T answer) => JsonSerializer.Serialize(answer, RemotableMessageCatalog.ContractFor(typeof(T)));

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        SqliteConnection.ClearAllPools();
        File.Delete(_path);
    }

    private IEnumerable<WorkOrderBuilder> WorkOrders()
    {
        var today = ChurchTimeZone.Today(Clock);
        var utc = Now.UtcDateTime;
        yield return Order("AAA0001", WorkOrderStatus.Draft).InBuilding("Main").InRoom("101").CreatedAt(utc.AddDays(-9));
        yield return Order("AAA0002", WorkOrderStatus.Assigned).Assigned(_fulfiller).InBuilding("Main").InRoom("101")
            .DueOn(today.AddDays(-2)).WithPriority(WorkOrderPriority.High).CreatedAt(utc.AddDays(-8)).AssignedAt(utc.AddDays(-7));
        yield return Order("AAA0003", WorkOrderStatus.InProgress).Assigned(_fulfiller).InBuilding("Main").InRoom("102")
            .DueOn(today).CreatedAt(utc.AddDays(-6)).AssignedAt(utc.AddDays(-5));
        yield return Order("AAA0004", WorkOrderStatus.Complete).Assigned(_fulfiller).InBuilding("Annex").InRoom("1")
            .DueOn(today.AddDays(-1)).CreatedAt(utc.AddDays(-20)).AssignedAt(utc.AddDays(-19)).CompletedAt(utc.AddDays(-3));
        yield return Order("AAA0005", WorkOrderStatus.Assigned).Assigned(_fulfiller).InBuilding("Annex")
            .DueOn(today.AddDays(5)).WithPriority(WorkOrderPriority.Low).CreatedAt(utc.AddDays(-4)).AssignedAt(utc.AddDays(-4));
        yield return Order("AAA0006", WorkOrderStatus.Cancelled).InBuilding(null).CreatedAt(utc.AddDays(-2));
        yield return Order("AAA0007", WorkOrderStatus.Complete).Assigned(_fulfiller).InBuilding("Main").InRoom("101")
            .CreatedAt(utc.AddDays(-60)).AssignedAt(utc.AddDays(-59)).CompletedAt(utc.AddDays(-45));
    }

    private WorkOrderBuilder Order(string number, WorkOrderStatus status) =>
        WorkOrderBuilder.Create().WithNumber(number).WithTitle($"Work order {number}").CreatedBy(_creator).WithStatus(status);

    private sealed class SqliteFile(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool()
        {
        }
    }
}
