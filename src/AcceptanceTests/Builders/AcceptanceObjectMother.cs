using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Builders;

/// <summary>
/// Bogus-backed builders that create acceptance data keyed by TestTag.
/// Work orders fall back to SQL when test-support returns 404 (tdd/uat/prod).
/// </summary>
public sealed class AcceptanceObjectMother(TestSupportClient client, string testTag)
{
    /// <summary>Creates an admin employee for this test tag.</summary>
    public Task<TestSupportEmployee> EmployeeAsync(TestSupportEmployeeFields? fields = null) =>
        client.CreateEmployeeAsync(testTag, fields);

    /// <summary>Creates a draft work order for <paramref name="creatorUserName"/> titled with the test tag.</summary>
    public Task<TestSupportWorkOrder> DraftWorkOrderAsync(
        string creatorUserName,
        string? title = null,
        string? description = null,
        string? roomNumber = null) =>
        WorkOrderAsync(creatorUserName, WorkOrderStatus.Draft, new WorkOrderSeedFields
        {
            Title = title,
            Description = description,
            RoomNumber = roomNumber
        });

    /// <summary>
    /// Creates a work order with the given status and optional fields.
    /// Falls back to SQL when test-support returns 404 (tdd/uat/prod).
    /// </summary>
    public Task<TestSupportWorkOrder> WorkOrderAsync(
        string creatorUserName,
        WorkOrderStatus status,
        WorkOrderSeedFields? fields = null) =>
        AcceptanceWorkOrderSeed.CreateAsync(testTag, creatorUserName, status, fields);
}
