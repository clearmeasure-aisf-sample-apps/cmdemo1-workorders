using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.TestSupport;

/// <summary>
/// Fluent test-data builder for <see cref="WorkOrder"/>. Named in the ubiquitous language so arrange
/// steps read as domain intent instead of positional parameter lists.
/// </summary>
public sealed class WorkOrderBuilder
{
    private Guid? _id;
    private string? _number;
    private string? _title;
    private string? _description;
    private string? _roomNumber;
    private string? _building;
    private Employee? _creator;
    private Employee? _assignee;
    private WorkOrderStatus _status = WorkOrderStatus.Draft;
    private DateTime? _createdDate;
    private DateTime? _assignedDate;
    private DateTime? _completedDate;
    private DateOnly? _dueDate;
    private WorkOrderPriority _priority = WorkOrderPriority.Medium;
    private DateTime? _waitingOnVendorSince;

    /// <summary>Starts a draft work order builder.</summary>
    public static WorkOrderBuilder Create() => new();

    /// <summary>Starts a builder for <paramref name="creator"/> in <paramref name="status"/> with a unique number.</summary>
    public static WorkOrderBuilder For(Employee creator, WorkOrderStatus status) =>
        Create().WithNumber(UniqueWorkOrderNumber.Next()).CreatedBy(creator).WithStatus(status);

    /// <summary>Sets a fixed identity for remoting or equality assertions.</summary>
    public WorkOrderBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>Sets the work-order number.</summary>
    public WorkOrderBuilder WithNumber(string? number)
    {
        _number = number;
        return this;
    }

    /// <summary>Sets the title; defaults to the number when unset at build time.</summary>
    public WorkOrderBuilder WithTitle(string? title)
    {
        _title = title;
        return this;
    }

    /// <summary>Sets the description.</summary>
    public WorkOrderBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Sets the room number.</summary>
    public WorkOrderBuilder InRoom(string? roomNumber)
    {
        _roomNumber = roomNumber;
        return this;
    }

    /// <summary>Sets the building name.</summary>
    public WorkOrderBuilder InBuilding(string? building)
    {
        _building = building;
        return this;
    }

    /// <summary>Sets the creator.</summary>
    public WorkOrderBuilder CreatedBy(Employee? creator)
    {
        _creator = creator;
        return this;
    }

    /// <summary>Assigns the work order to an employee without changing status.</summary>
    public WorkOrderBuilder AssignedTo(Employee? assignee)
    {
        _assignee = assignee;
        return this;
    }

    /// <summary>Sets status to Assigned and the assignee.</summary>
    public WorkOrderBuilder Assigned(Employee assignee)
    {
        _assignee = assignee;
        _status = WorkOrderStatus.Assigned;
        return this;
    }

    /// <summary>Sets the persisted status.</summary>
    public WorkOrderBuilder WithStatus(WorkOrderStatus status)
    {
        _status = status;
        return this;
    }

    /// <summary>Sets the created timestamp.</summary>
    public WorkOrderBuilder CreatedAt(DateTime? createdDate)
    {
        _createdDate = createdDate;
        return this;
    }

    /// <summary>Sets the assigned timestamp.</summary>
    public WorkOrderBuilder AssignedAt(DateTime? assignedDate)
    {
        _assignedDate = assignedDate;
        return this;
    }

    /// <summary>Sets the completed timestamp.</summary>
    public WorkOrderBuilder CompletedAt(DateTime? completedDate)
    {
        _completedDate = completedDate;
        return this;
    }

    /// <summary>Sets the calendar due date.</summary>
    public WorkOrderBuilder DueOn(DateOnly? dueDate)
    {
        _dueDate = dueDate;
        return this;
    }

    /// <summary>Sets priority.</summary>
    public WorkOrderBuilder WithPriority(WorkOrderPriority priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>Marks the work order as waiting on a vendor from <paramref name="since"/>.</summary>
    public WorkOrderBuilder WaitingOnVendorSince(DateTime? since)
    {
        _waitingOnVendorSince = since;
        return this;
    }

    /// <summary>Builds the work order.</summary>
    public WorkOrder Build()
    {
        var number = _number ?? UniqueWorkOrderNumber.Next();
        var order = new WorkOrder
        {
            Number = number,
            Title = _title ?? number,
            Description = _description ?? "",
            RoomNumber = _roomNumber,
            Building = _building,
            Creator = _creator,
            Assignee = _assignee,
            Status = _status,
            CreatedDate = _createdDate,
            AssignedDate = _assignedDate,
            CompletedDate = _completedDate,
            DueDate = _dueDate,
            Priority = _priority,
            WaitingOnVendorSince = _waitingOnVendorSince
        };
        if (_id.HasValue)
        {
            order.Id = _id.Value;
        }

        return order;
    }

    /// <summary>Catalog of <paramref name="count"/> draft work orders numbered WO-001…</summary>
    public static WorkOrder[] ManyDrafts(int count) =>
        Enumerable.Range(1, count)
            .Select(i => Create()
                .WithNumber($"WO-{i:000}")
                .WithTitle($"Order {i}")
                .WithStatus(WorkOrderStatus.Draft)
                .Build())
            .ToArray();

    /// <summary>
    /// Catalog of work orders cycling every <see cref="WorkOrderStatus"/>, <paramref name="perStatus"/> each,
    /// with even-numbered rows high priority, assigned to Willie, room 101, and overdue.
    /// </summary>
    public static WorkOrder[] OnePerStatus(int perStatus)
    {
        var statuses = WorkOrderStatus.GetAllItems();
        var total = statuses.Length * perStatus;
        var willie = EmployeeBuilder.Create("gwillie")
            .WithFirstName("Groundskeeper")
            .WithLastName("Willie")
            .WithEmail("willie@example.com")
            .Build();
        return Enumerable.Range(1, total)
            .Select(n => BuildOnePerStatusRow(n, total, statuses, willie))
            .ToArray();
    }

    private static WorkOrder BuildOnePerStatusRow(int n, int total, WorkOrderStatus[] statuses, Employee willie)
    {
        var even = n % 2 == 0;
        return Create()
            .WithNumber($"WO-{n:000}")
            .WithTitle($"Title {total - n + 1:000}")
            .WithStatus(statuses[(n - 1) % statuses.Length])
            .WithPriority(even ? WorkOrderPriority.High : WorkOrderPriority.Low)
            .AssignedTo(even ? willie : null)
            .InRoom(even ? "101" : null)
            .DueOn(even ? new DateOnly(2000, 1, 1) : null)
            .Build();
    }

    /// <summary>
    /// Catalog of <paramref name="count"/> complete work orders, one row per day counted back from
    /// <paramref name="windowStartInclusive"/>: row <c>n</c> completes at
    /// <c>windowStartInclusive.AddDays(-n)</c> and is due on its own completion day.
    /// </summary>
    public static WorkOrder[] CompletedJustOutsideWindow(Employee creator, DateTime windowStartInclusive, int count) =>
        Enumerable.Range(1, count)
            .Select(daysBack =>
            {
                var completed = windowStartInclusive.AddDays(-daysBack);
                return For(creator, WorkOrderStatus.Complete)
                    .CompletedAt(completed)
                    .DueOn(DateOnly.FromDateTime(completed))
                    .Build();
            })
            .ToArray();
}
