using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderStatusHistoryQueryHandlerTests
{
    private DbContext? _context;
    private WorkOrderStatusHistoryQueryHandler? _handler;
    private string? _dbPath;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:SqlConnectionString", $"Data Source={_dbPath}" }
            })
            .Build();

        var config = new TestDatabaseConfiguration(configuration);
        _context = new DataContext(config);
        _context.Database.EnsureCreated();

        _handler = new WorkOrderStatusHistoryQueryHandler(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (!string.IsNullOrEmpty(_dbPath) && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Test]
    public async Task Handle_ShouldReturnEmptyArray_WhenWorkOrderDoesNotExist()
    {
        var nonExistentWorkOrderId = Guid.NewGuid();
        var query = new WorkOrderStatusHistoryQuery(nonExistentWorkOrderId);

        var result = await _handler!.Handle(query, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldReturnEmptyArray_WhenWorkOrderHasNoStatusChanges()
    {
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-001",
            Title = "Test",
            Description = "Test",
            Status = WorkOrderStatus.Draft,
            Creator = creator,
            CreatedDate = DateTime.UtcNow
        };

        _context!.Add(creator);
        _context.Add(workOrder);
        await _context.SaveChangesAsync();

        var query = new WorkOrderStatusHistoryQuery(workOrder.Id);
        var result = await _handler!.Handle(query, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldReturnStatusChanges_OrderedByDateDescending()
    {
        var workOrderId = await GivenTwoChangesAsync();

        var result = await _handler!.Handle(new WorkOrderStatusHistoryQuery(workOrderId), CancellationToken.None);

        result.Length.ShouldBe(2);
        result[0].ChangedDate.ShouldBe(new DateTime(2025, 3, 1, 10, 0, 0));
        result[1].ChangedDate.ShouldBe(new DateTime(2025, 2, 15, 10, 0, 0));
    }

    [Test]
    public async Task Handle_ShouldIncludeChangedByEmployee()
    {
        var workOrderId = await GivenSingleChangeAsync();

        var result = await _handler!.Handle(new WorkOrderStatusHistoryQuery(workOrderId), CancellationToken.None);

        result.Length.ShouldBe(1);
        result[0].ChangedBy.ShouldNotBeNull();
        result[0].ChangedBy!.UserName.ShouldBe("hsimpson");
        result[0].ChangedBy!.GetFullName().ShouldBe("Homer Simpson");
    }

    [Test]
    public async Task Handle_ShouldOnlyReturnChangesForSpecificWorkOrder()
    {
        var workOrderId = await GivenTwoWorkOrdersWithChangesAsync();

        var result = await _handler!.Handle(new WorkOrderStatusHistoryQuery(workOrderId), CancellationToken.None);

        result.Length.ShouldBe(1);
        result[0].WorkOrderId.ShouldBe(workOrderId);
    }

    private async Task<Guid> GivenTwoChangesAsync()
    {
        var creator = EmployeeBuilder.Create("jpalermo")
            .WithFirstName("Jeffrey").WithLastName("Palermo").WithEmail("jp@example.com")
            .WithId(Guid.NewGuid()).Build();
        var assignee = EmployeeBuilder.Create("hsimpson")
            .WithFirstName("Homer").WithLastName("Simpson").WithEmail("hs@example.com")
            .WithId(Guid.NewGuid()).Build();
        var workOrder = WorkOrderBuilder.Create().WithId(Guid.NewGuid()).WithNumber("WO-001")
            .WithTitle("Test").WithDescription("Test").CreatedBy(creator).AssignedTo(assignee)
            .WithStatus(WorkOrderStatus.InProgress).CreatedAt(DateTime.UtcNow).Build();
        _context!.AddRange(creator, assignee, workOrder,
            Change(workOrder.Id, assignee.Id, (WorkOrderStatus.Draft, WorkOrderStatus.Assigned,
                new DateTime(2025, 2, 15, 10, 0, 0))),
            Change(workOrder.Id, assignee.Id, (WorkOrderStatus.Assigned, WorkOrderStatus.InProgress,
                new DateTime(2025, 3, 1, 10, 0, 0))));
        await _context.SaveChangesAsync();
        return workOrder.Id;
    }

    private async Task<Guid> GivenSingleChangeAsync()
    {
        var creator = EmployeeBuilder.Create("jpalermo")
            .WithFirstName("Jeffrey").WithLastName("Palermo").WithEmail("jp@example.com")
            .WithId(Guid.NewGuid()).Build();
        var assignee = EmployeeBuilder.Create("hsimpson")
            .WithFirstName("Homer").WithLastName("Simpson").WithEmail("hs@example.com")
            .WithId(Guid.NewGuid()).Build();
        var workOrder = WorkOrderBuilder.Create().WithId(Guid.NewGuid()).WithNumber("WO-001")
            .WithTitle("Test").WithDescription("Test").CreatedBy(creator).AssignedTo(assignee)
            .WithStatus(WorkOrderStatus.Assigned).CreatedAt(DateTime.UtcNow).Build();
        _context!.AddRange(creator, assignee, workOrder,
            Change(workOrder.Id, assignee.Id, (WorkOrderStatus.Draft, WorkOrderStatus.Assigned, DateTime.UtcNow)));
        await _context.SaveChangesAsync();
        return workOrder.Id;
    }

    private async Task<Guid> GivenTwoWorkOrdersWithChangesAsync()
    {
        var creator = EmployeeBuilder.Create("jpalermo")
            .WithFirstName("Jeffrey").WithLastName("Palermo").WithEmail("jp@example.com")
            .WithId(Guid.NewGuid()).Build();
        var assignee = EmployeeBuilder.Create("hsimpson")
            .WithFirstName("Homer").WithLastName("Simpson").WithEmail("hs@example.com")
            .WithId(Guid.NewGuid()).Build();
        var workOrder1 = WorkOrderBuilder.Create().WithId(Guid.NewGuid()).WithNumber("WO-001")
            .WithTitle("Test 1").WithDescription("Test").CreatedBy(creator).AssignedTo(assignee)
            .WithStatus(WorkOrderStatus.Assigned).CreatedAt(DateTime.UtcNow).Build();
        var workOrder2 = WorkOrderBuilder.Create().WithId(Guid.NewGuid()).WithNumber("WO-002")
            .WithTitle("Test 2").WithDescription("Test").CreatedBy(creator).AssignedTo(assignee)
            .WithStatus(WorkOrderStatus.Assigned).CreatedAt(DateTime.UtcNow).Build();
        _context!.AddRange(creator, assignee, workOrder1, workOrder2,
            Change(workOrder1.Id, assignee.Id, (WorkOrderStatus.Draft, WorkOrderStatus.Assigned, DateTime.UtcNow)),
            Change(workOrder2.Id, assignee.Id, (WorkOrderStatus.Draft, WorkOrderStatus.Assigned, DateTime.UtcNow)));
        await _context.SaveChangesAsync();
        return workOrder1.Id;
    }

    private static WorkOrderStatusChange Change(
        Guid workOrderId, Guid changedById, (WorkOrderStatus From, WorkOrderStatus To, DateTime When) transition) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrderId,
            FromStatus = transition.From,
            ToStatus = transition.To,
            ChangedById = changedById,
            ChangedDate = transition.When
        };
}
