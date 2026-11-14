using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Builders;

/// <summary>
/// Shared API-backed seeding for acceptance fixtures proven under <see cref="ParallelScope.All"/>.
/// Falls back to SQL when test-support returns 404 (tdd/uat/prod).
/// </summary>
[Parallelizable(ParallelScope.All)]
public abstract class AcceptanceApiTestBase : AcceptanceTestBase
{
    /// <summary>Object mother keyed by this test's tag.</summary>
    protected AcceptanceObjectMother Mother =>
        new(AcceptanceTestSupportSettings.CreateClient(), TestTag);

    /// <summary>Creates an admin employee for this test via test-support or SQL fallback.</summary>
    protected Task<Employee> SeedEmployeeAsync(TestSupportEmployeeFields? fields = null) =>
        AcceptanceEmployeeSeed.CreateAsync(TestTag, fields);

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
