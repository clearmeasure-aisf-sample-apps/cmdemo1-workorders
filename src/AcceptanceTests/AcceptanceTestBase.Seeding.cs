using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// API-backed seeding helpers so WorkOrders fixtures stay free of DataContext.
/// </summary>
public abstract partial class AcceptanceTestBase
{
    /// <summary>Object mother keyed by this test's <see cref="TestTag"/>.</summary>
    protected AcceptanceObjectMother Mother =>
        new(AcceptanceTestSupportSettings.CreateClient(), TestTag);

    /// <summary>Creates an admin employee for this test via test-support.</summary>
    protected async Task<Employee> SeedEmployeeAsync(string[]? additionalCreateRoleNames = null)
    {
        var created = await Mother.EmployeeAsync(additionalCreateRoleNames);
        var employee = new Employee(created.UserName, created.FirstName, created.LastName, created.EmailAddress)
        {
            Id = created.Id
        };
        employee.AddRole(new Role("admin", true, true));
        return employee;
    }

    /// <summary>Creates a work order for <paramref name="creator"/> via test-support.</summary>
    protected async Task<WorkOrder> SeedWorkOrderAsync(Employee creator, WorkOrderSeedSpec? spec = null)
    {
        spec ??= new WorkOrderSeedSpec();
        var status = spec.Status ?? WorkOrderStatus.Draft;
        var created = await Mother.WorkOrderAsync(creator.UserName, status, spec.ToFields());
        return MapSeededWorkOrder(created, creator, status, spec);
    }

    private static WorkOrder MapSeededWorkOrder(
        TestSupportWorkOrder created,
        Employee creator,
        WorkOrderStatus status,
        WorkOrderSeedSpec spec) =>
        new()
        {
            Id = created.Id,
            Number = created.Number,
            Title = created.Title,
            Description = created.Description,
            RoomNumber = created.RoomNumber,
            Creator = creator,
            Assignee = spec.Assignee,
            Status = status,
            Priority = spec.Priority,
            DueDate = spec.DueDate
        };
}
