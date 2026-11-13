using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.TestSupport;

/// <summary>
/// Persists acceptance-test employees and work orders through EF Core for the gated test-support API.
/// </summary>
public sealed class TestSupportDataService(
    DataContext context,
    IWorkOrderNumberGenerator numberGenerator)
{
    private const string UserNamePrefix = "test_";

    /// <summary>Creates an employee whose user name carries the request test tag.</summary>
    public async Task<TestSupportEmployeeDto> CreateEmployeeAsync(
        TestSupportEmployeeCreate request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TestTag);
        var employee = BuildEmployee(request);
        context.Add(employee);
        await context.SaveChangesAsync(cancellationToken);
        return ToEmployeeDto(employee);
    }

    /// <summary>Creates a work order for the request creator, titled with the test tag.</summary>
    public async Task<TestSupportWorkOrderDto> CreateWorkOrderAsync(
        TestSupportWorkOrderCreate request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TestTag);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CreatorUserName);

        var creator = await context.Set<Employee>()
            .SingleAsync(employee => employee.UserName == request.CreatorUserName, cancellationToken);
        var assignee = await ResolveAssigneeAsync(request.AssigneeUserName, cancellationToken);
        var workOrder = BuildWorkOrder(request, creator, assignee);
        context.Add(workOrder);
        await context.SaveChangesAsync(cancellationToken);
        return ToWorkOrderDto(workOrder, creator.UserName);
    }

    private async Task<Employee?> ResolveAssigneeAsync(string? userName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        return await context.Set<Employee>()
            .SingleAsync(employee => employee.UserName == userName, cancellationToken);
    }

    private static Employee BuildEmployee(TestSupportEmployeeCreate request)
    {
        // Keep First/Last short and breakable by default: uniqueness stays in UserName.
        // Login fixtures may pass an early-sorting LastName so the row stays in the first page.
        var employee = new Employee(
            BuildUserName(request.TestTag),
            string.IsNullOrWhiteSpace(request.FirstName) ? "Pat" : request.FirstName.Trim(),
            string.IsNullOrWhiteSpace(request.LastName) ? "Tester" : request.LastName.Trim(),
            $"{request.TestTag}@acceptance.test");
        if (!string.IsNullOrWhiteSpace(request.MiddleName))
        {
            employee.MiddleName = request.MiddleName.Trim();
        }

        employee.AddRole(new Role("admin", request.CanCreateWorkOrder, request.CanFulfillWorkOrder));
        AddCreateOnlyRoles(employee, request.AdditionalCreateRoleNames);
        return employee;
    }

    private static void AddCreateOnlyRoles(Employee employee, string[]? roleNames)
    {
        if (roleNames is not { Length: > 0 })
        {
            return;
        }

        foreach (var roleName in roleNames)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                continue;
            }

            employee.AddRole(new Role(roleName.Trim(), canCreate: true, canFulfill: false));
        }
    }

    private static string BuildUserName(string testTag)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userName = $"{UserNamePrefix}{testTag}_{suffix}";
        return userName.Length > 100 ? userName[..100] : userName;
    }

    private WorkOrder BuildWorkOrder(
        TestSupportWorkOrderCreate request,
        Employee creator,
        Employee? assignee)
    {
        var status = ResolveStatus(request.StatusKey);
        var now = DateTime.UtcNow;
        var createdDate = ResolveDateTime(request.CreatedDate) ?? now;
        var workOrder = new WorkOrder
        {
            Number = ResolveNumber(request.Number),
            Creator = creator,
            Assignee = assignee,
            Status = status,
            Priority = ResolvePriority(request.Priority),
            DueDate = ResolveDueDate(request.DueDate),
            Title = ResolveTitle(request),
            Description = request.Description ?? $"[{request.TestTag}] description",
            Instructions = request.Instructions ?? string.Empty,
            RoomNumber = request.RoomNumber ?? "1",
            Building = request.Building,
            CreatedDate = createdDate
        };
        ApplyStatusDates(workOrder, status, now, ResolveDateTime(request.CompletedDate));
        return workOrder;
    }

    private string ResolveNumber(string? number) =>
        string.IsNullOrWhiteSpace(number) ? numberGenerator.GenerateNumber() : number.Trim();

    private static string ResolveTitle(TestSupportWorkOrderCreate request) =>
        string.IsNullOrWhiteSpace(request.Title)
            ? $"[{request.TestTag}] from test-support"
            : request.Title;

    private static TestSupportEmployeeDto ToEmployeeDto(Employee employee) => new()
    {
        Id = employee.Id,
        UserName = employee.UserName,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        EmailAddress = employee.EmailAddress
    };

    private static TestSupportWorkOrderDto ToWorkOrderDto(WorkOrder workOrder, string creatorUserName) => new()
    {
        Id = workOrder.Id,
        Number = workOrder.Number!,
        Title = workOrder.Title!,
        Description = workOrder.Description,
        RoomNumber = workOrder.RoomNumber,
        CreatorUserName = creatorUserName
    };

    private static void ApplyStatusDates(
        WorkOrder workOrder,
        WorkOrderStatus status,
        DateTime now,
        DateTime? completedDate)
    {
        if (status == WorkOrderStatus.Draft)
        {
            if (completedDate.HasValue)
            {
                workOrder.CompletedDate = completedDate;
            }

            return;
        }

        workOrder.AssignedDate ??= now.AddDays(-1);
        if (status == WorkOrderStatus.Complete)
        {
            workOrder.CompletedDate = completedDate ?? now;
        }
    }

    private static WorkOrderStatus ResolveStatus(string? statusKey)
    {
        if (string.IsNullOrWhiteSpace(statusKey))
        {
            return WorkOrderStatus.Draft;
        }

        return WorkOrderStatus.FromKey(statusKey.Trim());
    }

    private static WorkOrderPriority ResolvePriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
        {
            return WorkOrderPriority.Medium;
        }

        return Enum.Parse<WorkOrderPriority>(priority.Trim(), ignoreCase: true);
    }

    private static DateOnly? ResolveDueDate(string? dueDate)
    {
        if (string.IsNullOrWhiteSpace(dueDate))
        {
            return null;
        }

        return DateOnly.Parse(dueDate.Trim(), CultureInfo.InvariantCulture);
    }

    private static DateTime? ResolveDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.Parse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            .ToUniversalTime();
    }
}
