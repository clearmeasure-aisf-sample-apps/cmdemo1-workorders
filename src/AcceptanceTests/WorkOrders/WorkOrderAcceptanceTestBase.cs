using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

/// <summary>
/// WorkOrders acceptance base: API-backed seeding keyed by <see cref="AcceptanceTestBase.TestTag"/>.
/// Falls back to SQL when test-support returns 404 (tdd/uat/prod).
/// </summary>
public abstract class WorkOrderAcceptanceTestBase : AcceptanceTestBase
{
    /// <summary>Object mother keyed by this test's tag.</summary>
    protected AcceptanceObjectMother Mother =>
        new(AcceptanceTestSupportSettings.CreateClient(), TestTag);

    /// <summary>Creates an admin employee for this test via test-support or SQL fallback.</summary>
    protected Task<Employee> SeedEmployeeAsync(string[]? additionalCreateRoleNames = null)
    {
        TestSupportEmployeeFields? fields = additionalCreateRoleNames is null
            ? null
            : new TestSupportEmployeeFields { AdditionalCreateRoleNames = additionalCreateRoleNames };
        return AcceptanceEmployeeSeed.CreateAsync(TestTag, fields);
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
