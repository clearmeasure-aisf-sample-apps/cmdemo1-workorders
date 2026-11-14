using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Builders;

/// <summary>
/// Shared API-backed seeding for acceptance fixtures proven under <see cref="ParallelScope.All"/>.
/// </summary>
[Parallelizable(ParallelScope.All)]
public abstract class AcceptanceApiTestBase : AcceptanceTestBase
{
    /// <summary>Object mother keyed by this test's tag.</summary>
    protected AcceptanceObjectMother Mother =>
        new(AcceptanceTestSupportSettings.CreateClient(), TestTag);

    /// <summary>Creates an admin employee for this test via test-support.</summary>
    protected async Task<Employee> SeedEmployeeAsync(TestSupportEmployeeFields? fields = null)
    {
        var created = await Mother.EmployeeAsync(fields);
        var employee = new Employee(created.UserName, created.FirstName, created.LastName, created.EmailAddress)
        {
            Id = created.Id
        };
        employee.AddRole(new Role("admin", true, true));
        return employee;
    }

    /// <summary>Creates a work order for <paramref name="creator"/> via test-support.</summary>
    protected async Task<WorkOrder> SeedWorkOrderAsync(
        Employee creator,
        WorkOrderStatus? status = null,
        WorkOrderSeedFields? fields = null)
    {
        fields ??= new WorkOrderSeedFields();
        var resolvedStatus = status ?? WorkOrderStatus.Draft;
        var created = await Mother.WorkOrderAsync(creator.UserName, resolvedStatus, fields);
        return new WorkOrder
        {
            Id = created.Id,
            Number = created.Number,
            Title = created.Title,
            Description = created.Description,
            RoomNumber = created.RoomNumber,
            Creator = creator,
            Status = resolvedStatus,
            Priority = fields.Priority,
            DueDate = fields.DueDate,
            Building = fields.Building,
            CreatedDate = fields.CreatedDate ?? DateTime.UtcNow,
            CompletedDate = fields.CompletedDate
        };
    }
}
