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

        var employee = new Employee(
            BuildUserName(request.TestTag),
            $"First{request.TestTag}",
            $"Last{request.TestTag}",
            $"{request.TestTag}@acceptance.test");
        employee.AddRole(new Role("admin", request.CanCreateWorkOrder, request.CanFulfillWorkOrder));
        context.Add(employee);
        await context.SaveChangesAsync(cancellationToken);

        return new TestSupportEmployeeDto
        {
            Id = employee.Id,
            UserName = employee.UserName,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            EmailAddress = employee.EmailAddress
        };
    }

    /// <summary>Creates a draft work order for the request creator, titled with the test tag.</summary>
    public async Task<TestSupportWorkOrderDto> CreateWorkOrderAsync(
        TestSupportWorkOrderCreate request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TestTag);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CreatorUserName);

        var creator = await context.Set<Employee>()
            .SingleAsync(employee => employee.UserName == request.CreatorUserName, cancellationToken);
        var workOrder = BuildDraft(request, creator);
        context.Add(workOrder);
        await context.SaveChangesAsync(cancellationToken);

        return new TestSupportWorkOrderDto
        {
            Id = workOrder.Id,
            Number = workOrder.Number!,
            Title = workOrder.Title!,
            Description = workOrder.Description,
            RoomNumber = workOrder.RoomNumber,
            CreatorUserName = creator.UserName
        };
    }

    private static string BuildUserName(string testTag)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userName = $"{UserNamePrefix}{testTag}_{suffix}";
        return userName.Length > 100 ? userName[..100] : userName;
    }

    private WorkOrder BuildDraft(TestSupportWorkOrderCreate request, Employee creator) =>
        new()
        {
            Number = numberGenerator.GenerateNumber(),
            Creator = creator,
            Status = WorkOrderStatus.Draft,
            Title = string.IsNullOrWhiteSpace(request.Title)
                ? $"[{request.TestTag}] from test-support"
                : request.Title,
            Description = request.Description ?? $"[{request.TestTag}] description",
            RoomNumber = request.RoomNumber ?? "1",
            CreatedDate = DateTime.UtcNow
        };
}
