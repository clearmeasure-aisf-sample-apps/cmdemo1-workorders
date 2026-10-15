using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var assignee = new Employee("hsimpson", "Homer", "Simpson", "hs@example.com") { Id = Guid.NewGuid() };
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-001",
            Title = "Test",
            Description = "Test",
            Status = WorkOrderStatus.InProgress,
            Creator = creator,
            Assignee = assignee,
            CreatedDate = DateTime.UtcNow
        };

        var oldDate = new DateTime(2025, 2, 15, 10, 0, 0);
        var newDate = new DateTime(2025, 3, 1, 10, 0, 0);

        var change1 = new WorkOrderStatusChange
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.Draft,
            ToStatus = WorkOrderStatus.Assigned,
            ChangedById = assignee.Id,
            ChangedDate = oldDate
        };

        var change2 = new WorkOrderStatusChange
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.Assigned,
            ToStatus = WorkOrderStatus.InProgress,
            ChangedById = assignee.Id,
            ChangedDate = newDate
        };

        _context!.Add(creator);
        _context.Add(assignee);
        _context.Add(workOrder);
        _context.Add(change1);
        _context.Add(change2);
        await _context.SaveChangesAsync();

        var query = new WorkOrderStatusHistoryQuery(workOrder.Id);
        var result = await _handler!.Handle(query, CancellationToken.None);

        result.Length.ShouldBe(2);
        // Should be ordered by date descending (newest first)
        result[0].ChangedDate.ShouldBe(newDate);
        result[1].ChangedDate.ShouldBe(oldDate);
    }

    [Test]
    public async Task Handle_ShouldIncludeChangedByEmployee()
    {
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var assignee = new Employee("hsimpson", "Homer", "Simpson", "hs@example.com") { Id = Guid.NewGuid() };
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-001",
            Title = "Test",
            Description = "Test",
            Status = WorkOrderStatus.Assigned,
            Creator = creator,
            Assignee = assignee,
            CreatedDate = DateTime.UtcNow
        };

        var change = new WorkOrderStatusChange
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrder.Id,
            FromStatus = WorkOrderStatus.Draft,
            ToStatus = WorkOrderStatus.Assigned,
            ChangedById = assignee.Id,
            ChangedDate = DateTime.UtcNow
        };

        _context!.Add(creator);
        _context.Add(assignee);
        _context.Add(workOrder);
        _context.Add(change);
        await _context.SaveChangesAsync();

        var query = new WorkOrderStatusHistoryQuery(workOrder.Id);
        var result = await _handler!.Handle(query, CancellationToken.None);

        result.Length.ShouldBe(1);
        result[0].ChangedBy.ShouldNotBeNull();
        result[0].ChangedBy!.UserName.ShouldBe("hsimpson");
        result[0].ChangedBy!.GetFullName().ShouldBe("Homer Simpson");
    }

    [Test]
    public async Task Handle_ShouldOnlyReturnChangesForSpecificWorkOrder()
    {
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var assignee = new Employee("hsimpson", "Homer", "Simpson", "hs@example.com") { Id = Guid.NewGuid() };

        var workOrder1 = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-001",
            Title = "Test 1",
            Description = "Test",
            Status = WorkOrderStatus.Assigned,
            Creator = creator,
            Assignee = assignee,
            CreatedDate = DateTime.UtcNow
        };

        var workOrder2 = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-002",
            Title = "Test 2",
            Description = "Test",
            Status = WorkOrderStatus.Assigned,
            Creator = creator,
            Assignee = assignee,
            CreatedDate = DateTime.UtcNow
        };

        var change1 = new WorkOrderStatusChange
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrder1.Id,
            FromStatus = WorkOrderStatus.Draft,
            ToStatus = WorkOrderStatus.Assigned,
            ChangedById = assignee.Id,
            ChangedDate = DateTime.UtcNow
        };

        var change2 = new WorkOrderStatusChange
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrder2.Id,
            FromStatus = WorkOrderStatus.Draft,
            ToStatus = WorkOrderStatus.Assigned,
            ChangedById = assignee.Id,
            ChangedDate = DateTime.UtcNow
        };

        _context!.Add(creator);
        _context.Add(assignee);
        _context.Add(workOrder1);
        _context.Add(workOrder2);
        _context.Add(change1);
        _context.Add(change2);
        await _context.SaveChangesAsync();

        var query = new WorkOrderStatusHistoryQuery(workOrder1.Id);
        var result = await _handler!.Handle(query, CancellationToken.None);

        result.Length.ShouldBe(1);
        result[0].WorkOrderId.ShouldBe(workOrder1.Id);
    }
}
